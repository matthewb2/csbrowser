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
    private DisplayItem? _hoveredDisplayItem;

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
        Relayout();
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

        if (_layoutRoot != null)
        {
            float docHeight = _layoutRoot.Bounds.Y + _layoutRoot.Bounds.Height + 20;
            AutoScrollMinSize = new Size(Width, (int)docHeight);
            Log.WriteLine($"[BrowserControl] Updated AutoScrollMinSize: {AutoScrollMinSize}");
        }

        BuildHitTestTree();
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
            //Log.WriteLine($"[Hover] CLEAR hover on tag=<{oldAncestor.TagName}> id={oldAncestor.Id}");
            ClearHoverRecursive(oldAncestor);
            RebuildDisplayList(GetPseudoStyledBounds(oldAncestor));
            //Invalidate();
        }

        _hoveredElement = found;

        if (foundAncestor != null)
        {
            //Log.WriteLine($"[Hover] SET hover on tag=<{foundAncestor.TagName}> id={foundAncestor.Id}");
            SetHoverRecursive(foundAncestor);
            RebuildDisplayList(GetPseudoStyledBounds(foundAncestor));
            //Invalidate();
        }

        if (foundItem != _hoveredDisplayItem)
        {
            if (_hoveredDisplayItem != null)
                Invalidate(TransformToClient(_hoveredDisplayItem.Bounds));

            _hoveredDisplayItem = foundItem;

            if (_hoveredDisplayItem != null)
                Invalidate(TransformToClient(_hoveredDisplayItem.Bounds));
        }
    }

    private RectangleF GetPseudoStyledBounds(BrowserElement element)
    {
        if (_displayList == null)
            return RectangleF.Empty;

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        bool found = false;

        foreach (var item in _displayList)
        {
            if (item.Element == element)
            {
                found = true;
                if (item.Bounds.X < minX) minX = item.Bounds.X;
                if (item.Bounds.Y < minY) minY = item.Bounds.Y;
                if (item.Bounds.Right > maxX) maxX = item.Bounds.Right;
                if (item.Bounds.Bottom > maxY) maxY = item.Bounds.Bottom;
            }
        }

        return found
            ? RectangleF.FromLTRB(minX, minY, maxX, maxY)
            : RectangleF.Empty;
    }

    private Rectangle TransformToClient(RectangleF docBounds)
    {
        int x = (int)(docBounds.X - AutoScrollPosition.X);
        int y = (int)(docBounds.Y - AutoScrollPosition.Y);
        int w = (int)Math.Ceiling(docBounds.Width);
        int h = (int)Math.Ceiling(docBounds.Height);
        return new Rectangle(x, y, w, h);
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
                RebuildDisplayList(GetPseudoStyledBounds(ancestor));
            }
            _hoveredElement = null;
        }

        if (_hoveredDisplayItem != null)
        {
            Invalidate(TransformToClient(_hoveredDisplayItem.Bounds));
            _hoveredDisplayItem = null;
        }

        Log.WriteLine("[Hover] Cleared all hover states due to leave.");
    }

    private void RebuildDisplayList(RectangleF dirtyRegion)
    {
        if (_layoutRoot == null)
            return;

        //Log.WriteLine("[BrowserControl] Rebuilding display list due to style/hover change...");

        if (_displayList != null)
        {
            foreach (var item in _displayList)
                item.Unref();
        }
        
        var builder = new DisplayListBuilder();
        _displayList = builder.Build(_layoutRoot);

        //BuildHitTestTree();
        

        if (dirtyRegion != RectangleF.Empty)
            Invalidate(TransformToClient(dirtyRegion));
        else
            Invalidate();
    }

    private void BuildHitTestTree()
    {
        float treeW = 1;
        float treeH = 1;
        if (_layoutRoot != null)
        {
            treeW = Math.Max(_layoutRoot.Bounds.Right + 10, 1);
            treeH = Math.Max(_layoutRoot.Bounds.Bottom + 10, 1);
        }
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

        int viewX = -AutoScrollPosition.X;
        int viewY = -AutoScrollPosition.Y;
        int viewW = Math.Max(Width, 1);
        int viewH = Math.Max(Height, 1);

        RectangleF viewportRect = new RectangleF(viewX, viewY, viewW, viewH);

        var renderer = new GdiRenderer();

        Log.WriteLine($"[Paint #{_paintCount}] Initial Paint: Rendering all {_displayList.Count} items without Quadtree culling.");

        e.Graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

        foreach (var item in _displayList)
        {
            renderer.RenderItem(e.Graphics, item);
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
