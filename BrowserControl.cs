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
        Invalidate();
    }

    public void LoadDocument(BrowserElement root)
    {
        if (_document != null)
            _document.Unref();

        _document = root;
        root.Ref();

        ExecuteDocumentScripts();
        Relayout();
    }

    private void Relayout()
    {
        if (_document == null)
            return;

        Log.WriteLine("[BrowserControl] Relayout...");

        if (_layoutRoot != null)
        {
            _layoutRoot.Unref();
            _layoutRoot = null;
        }

        if (_displayList != null)
        {
            foreach (var item in _displayList)
                item.Unref();
            _displayList = null;
        }

        var layout = new LayoutEngine();
        _layoutRoot = layout.Layout(_document, Width);

        var builder = new DisplayListBuilder();
        _displayList = builder.Build(_layoutRoot);

        BuildHitTestTree();

        if (_layoutRoot != null)
        {
            float docHeight = _layoutRoot.Bounds.Y + _layoutRoot.Bounds.Height + 20;
            AutoScrollMinSize = new Size(Width, (int)docHeight);
        }

        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        // WinForms AutoScrollPosition.X/Y는 음수이므로, 문서 실제 좌표를 구하려면 빼주어야 합니다.
        var scrolled = new Point(e.X - AutoScrollPosition.X, e.Y - AutoScrollPosition.Y);
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

        if (oldAncestor != null)
        {
            ClearHoverRecursive(oldAncestor);
            RebuildDisplayList();
        }

        _hoveredElement = found;

        if (foundAncestor != null)
        {
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
        }
    }

    private void RebuildDisplayList()
    {
        if (_layoutRoot == null)
            return;

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

        if (_displayList != null)
        {
            foreach (var item in _displayList)
                _hitTestTree.Insert(item);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_displayList == null || _hitTestTree == null)
            return;

        // 1. 현재 화면에 노출되는 뷰포트(Viewport) 영역 계산
        // AutoScrollPosition은 음수이므로 양수로 변환하여 뷰포트 사각형 구성
        int viewX = -AutoScrollPosition.X;
        int viewY = -AutoScrollPosition.Y;
        int viewW = Math.Max(Width, 1);
        int viewH = Math.Max(Height, 1);

        RectangleF viewportRect = new RectangleF(viewX, viewY, viewW, viewH);

        // 2. 쿼드트리를 이용해 현재 뷰포트 영역에 겹치는 아이템들만 고속 추출 (컬링)
        var visibleItems = new List<DisplayItem>();
        _hitTestTree.Query(viewportRect, visibleItems);

        // 3. 스크롤 위치에 맞추어 그래픽스 좌표계 이동 (음수 스크롤 위치 그대로 적용)
        e.Graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

        // 4. 텍스트 및 그래픽 품질 설정 (글자 뭉개짐 방지)
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var renderer = new GdiRenderer();

        // 5. 가시 영역의 아이템들을 화면에 직접 다이렉트 렌더링
        foreach (var item in visibleItems)
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

        Log.WriteLine("[BrowserControl] Executing scripts...");

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