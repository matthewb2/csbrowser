using CSBrowser.Dom;
using CSBrowser.JavaScript;
using CSBrowser.Layout;
using CSBrowser.Render;

namespace CSBrowser;

public sealed class BrowserControl
    : UserControl
{
    private List<DisplayItem>? _displayList;
    private QuadtreeNode? _hitTestTree;

    private BrowserElement? _document;
    private LayoutNode? _layoutRoot;
    private BrowserElement? _hoveredElement;

    // ����׿� ������ ������ ī���� (�α� ȫ�� ������ �ֱ��� ��� � Ȱ�� ����)
    private int _paintCount = 0;
    private readonly List<DisplayItem> _capturedItems = new List<DisplayItem>(512);

    public BrowserControl()
    {
        AutoScroll = true;
        DoubleBuffered = true;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Log.WriteLine($"[BrowserControl] OnResize: Control Size = {Width}x{Height}");
        Invalidate();
    }

    public void LoadDocument(BrowserElement root)
    {
        Log.WriteLine("[BrowserControl] LoadDocument started.");
        if (_document != null)
            _document.Unref();

        _document = root;
        root.Ref();

        ExecuteDocumentScripts();
        Relayout();
        Log.WriteLine("[BrowserControl] LoadDocument finished.");
    }

    private void Relayout()
    {
        if (_document == null)
            return;

        Log.WriteLine("[BrowserControl] Relayout started. Width = " + Width);

        if (_layoutRoot != null)
        {
            _layoutRoot.Unref();
            _layoutRoot = null;
        }

        if (_displayList != null)
        {
            Log.WriteLine($"[BrowserControl] Disposing previous display list (Count: {_displayList.Count})");
            foreach (var item in _displayList)
                item.Unref();
            _displayList = null;
        }

        var layout = new LayoutEngine();
        _layoutRoot = layout.Layout(_document, Width);

        var builder = new DisplayListBuilder();
        _displayList = builder.Build(_layoutRoot);

        Log.WriteLine($"[BrowserControl] Built new display list. Total DisplayItems: {_displayList?.Count ?? 0}");

        BuildHitTestTree();

        if (_layoutRoot != null)
        {
            float docHeight = _layoutRoot.Bounds.Y + _layoutRoot.Bounds.Height + 20;
            AutoScrollMinSize = new Size(Width, (int)docHeight);
            Log.WriteLine($"[BrowserControl] Updated AutoScrollMinSize: {AutoScrollMinSize}");
        }

        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        var scrolled = new Point(e.X - AutoScrollPosition.X, e.Y - AutoScrollPosition.Y);
        Log.WriteLine($"[Mouse] Down at Screen({e.X}, {e.Y}), Scroll({AutoScrollPosition.X}, {AutoScrollPosition.Y}) -> DocPos({scrolled.X}, {scrolled.Y}), Button: {e.Button}");
        DispatchMouseEvent("mousedown", scrolled, e.Button);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var scrolled = new Point(e.X - AutoScrollPosition.X, e.Y - AutoScrollPosition.Y);

        UpdateHoverState(scrolled);
        DispatchMouseEvent("mousemove", scrolled, e.Button);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        Log.WriteLine("[Mouse] Leave control area.");
        ClearHoverState();
    }

    private void UpdateHoverState(Point pos)
    {
        if (_hitTestTree == null)
            return;

        var foundItem = _hitTestTree.HitTest(pos.X, pos.Y);
        BrowserElement? found = foundItem?.Element;

        BrowserElement? foundAncestor = found?.FindAncestorWithPseudoStyle();
        BrowserElement? oldAncestor = _hoveredElement?.FindAncestorWithPseudoStyle();

        if (foundAncestor == oldAncestor)
            return;

        Log.WriteLine($"[Hover] State change detected at DocPos({pos.X}, {pos.Y})");

        if (oldAncestor != null)
        {
            Log.WriteLine($"[Hover] CLEAR hover on tag=<{oldAncestor.TagName}> id={oldAncestor.Id}");
            ClearHoverRecursive(oldAncestor);
            RebuildDisplayList();
        }

        _hoveredElement = found;

        if (foundAncestor != null)
        {
            Log.WriteLine($"[Hover] SET hover on tag=<{foundAncestor.TagName}> id={foundAncestor.Id}");
            SetHoverRecursive(foundAncestor);
            RebuildDisplayList();
        }
    }

    private static void SetHoverRecursive(BrowserElement element)
    {
        element.State = ElementState.Hover;
        foreach (var child in element.Children)
            child.State = ElementState.Hover;
    }

    private static void ClearHoverRecursive(BrowserElement element)
    {
        element.State = ElementState.Normal;
        foreach (var child in element.Children)
            child.State = ElementState.Normal;
    }

    private void ClearHoverState()
    {
        if (_hoveredElement != null)
        {
            var ancestor = _hoveredElement.FindAncestorWithPseudoStyle();
            if (ancestor != null)
            {
                ClearHoverRecursive(ancestor);
                RebuildDisplayList();
            }
            _hoveredElement = null;
            Log.WriteLine("[Hover] Cleared all hover states due to leave.");
        }
    }

    private void RebuildDisplayList()
    {
        if (_layoutRoot == null)
            return;

        Log.WriteLine("[BrowserControl] Rebuilding display list due to style/hover change...");

        if (_displayList != null)
        {
            foreach (var item in _displayList)
                item.Unref();
        }

        var builder = new DisplayListBuilder();
        _displayList = builder.Build(_layoutRoot);

        BuildHitTestTree();
        Invalidate();
    }

    private void BuildHitTestTree()
    {
        float treeW = Math.Max(Width, 1);
        float treeH = Math.Max(Math.Max(AutoScrollMinSize.Height, Height), 1);
        _hitTestTree = new QuadtreeNode(new RectangleF(0, 0, treeW, treeH));

        int insertCount = 0;
        if (_displayList != null)
        {
            foreach (var item in _displayList)
            {
                _hitTestTree.Insert(item);
                insertCount++;
            }
        }
        Log.WriteLine($"[Quadtree] HitTest tree built. Bounds: {treeW}x{treeH}, Inserted items: {insertCount}");
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_displayList == null || _hitTestTree == null)
        {
            Log.WriteLine($"[Paint] Skipped. DisplayList={_displayList != null}, HitTestTree={_hitTestTree != null}");
            return;
        }

        _paintCount++;

        // 1. ���� ȭ�鿡 ����Ǵ� ����Ʈ(Viewport) ���� ���
        int viewX = -AutoScrollPosition.X;
        int viewY = -AutoScrollPosition.Y;
        int viewW = Math.Max(Width, 1);
        int viewH = Math.Max(Height, 1);

        RectangleF viewportRect = new RectangleF(viewX, viewY, viewW, viewH);

        // 2. ����Ʈ���� �̿��� ���� ����Ʈ ������ ��ġ�� �����۵鸸 ���� ���� (�ø�)
        
        _capturedItems.Clear();
        _hitTestTree.Query(viewportRect, _capturedItems);

        // [����� �α�] �˰������� �ùٸ��� �����ϴ��� Ȯ���� �� �ִ� �� ��ǥ ���
        // (��ũ���ϰų� â ũ�⸦ �ٲ� �� ��ü ������ �� �� ���� �߷����� �׸����� Ȯ���� �� �ֽ��ϴ�)
        Log.WriteLine($"[Paint #{_paintCount}] Viewport: [X={viewX}, Y={viewY}, W={viewW}, H={viewH}] | " +
                      $"Total Items: {_displayList.Count} | " +
                      $"Visible (Culling Result): {_capturedItems.Count} items rendered " +
                      $"({(double)_capturedItems.Count / Math.Max(_displayList.Count, 1) * 100:F1}% of total)");

        // 3. ��ũ�� ��ġ�� ���߾� �׷��Ƚ� ��ǥ�� �̵�
        e.Graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

        // 4. �ؽ�Ʈ �� �׷��� ǰ�� ����
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var renderer = new GdiRenderer();

        foreach (var item in _displayList)
        {
            if (item.Bounds.IntersectsWith(viewportRect))
            {
                renderer.RenderItem(e.Graphics, item);
            }
        }
    }

    private void DispatchMouseEvent(
        string eventType,
        Point pos,
        MouseButtons button = MouseButtons.None)
    {
        if (_hitTestTree == null)
            return;

        var hitItem = _hitTestTree.HitTest(pos.X, pos.Y);
        if (hitItem?.Element == null)
            return;

        var element = hitItem.Element;

        if (!element.EventListeners
                .TryGetValue(eventType,
                    out var listeners))
            return;

        var screen = PointToScreen(pos);
        Log.WriteLine($"[Event] Dispatched '{eventType}' to Element <{element.TagName}> id={element.Id}");

        var jsEvent = new JsMouseEvent(
            type: eventType,
            clientX: pos.X,
            clientY: pos.Y,
            screenX: screen.X,
            screenY: screen.Y,
            button: button switch
            {
                MouseButtons.Left => 0,
                MouseButtons.Middle => 1,
                MouseButtons.Right => 2,
                _ => 0
            },
            altKey: ModifierKeys.HasFlag(Keys.Alt),
            ctrlKey: ModifierKeys.HasFlag(Keys.Control),
            shiftKey: ModifierKeys.HasFlag(Keys.Shift),
            metaKey: false);

        var toRemove = new List<EventListenerInfo>();

        foreach (var info in listeners)
        {
            if (info.Callback is Action<JsMouseEvent> cb)
            {
                cb(jsEvent);

                if (info.Once)
                    toRemove.Add(info);
            }
        }

        foreach (var info in toRemove)
            listeners.Remove(info);
    }

    private void ExecuteDocumentScripts()
    {
        if (_document == null)
            return;

        var scripts = ScriptCollector.Collect(_document);
        if (scripts.Count == 0)
            return;

        Log.WriteLine("[Script] Executing document scripts...");

        var browserDoc = new BrowserDocument(_document);
        var jsDoc = new JsDocument(browserDoc);
        var jsWindow = new JsWindow();
        var jsConsole = new JsConsole();

        var engine = new JsEngine();
        engine.SetGlobal("document", jsDoc);
        engine.SetGlobal("window", jsWindow);
        engine.SetGlobal("console", jsConsole);
        engine.SetGlobal("alert", (string message) => jsWindow.alert(message));
        engine.SetGlobal("setTimeout", (Delegate callback, int delay) => jsWindow.setTimeout(callback, delay));
        engine.SetGlobal("clearTimeout", (int id) => jsWindow.clearTimeout(id));

        foreach (var script in scripts)
        {
            engine.Execute(script);
        }

        RegisterOnHandlers(engine, _document);
    }

    private static void RegisterOnHandlers(JsEngine engine, BrowserElement root)
    {
        foreach (var (eventType, handlerCode) in root.OnEventHandlers)
        {
            if (string.IsNullOrEmpty(root.Id))
            {
                continue;
            }

            var js = $"document.getElementById('{root.Id}')" +
                     $".addEventListener('{eventType}', " +
                     $"function(event) {{ {handlerCode} }});";

            try
            {
                engine.Execute(js);
            }
            catch (Exception ex)
            {
                Log.WriteLine($"  [ERROR] registering on{eventType}: {ex.Message}");
            }
        }

        foreach (var child in root.Children)
        {
            RegisterOnHandlers(engine, child);
        }
    }
}