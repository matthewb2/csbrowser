using CSBrowser.Dom;
using CSBrowser.JavaScript;
using CSBrowser.Layout;
using CSBrowser.Render;

namespace CSBrowser;

public sealed class BrowserControl
    : UserControl
{
    private List<DisplayItem>? _displayList;
    private BrowserElement? _document;
    private LayoutNode? _layoutRoot;
    private BrowserElement? _hoveredElement;

    public BrowserControl()
    {
        AutoScroll = true;
        DoubleBuffered = true;
    }

    public void LoadDocument(BrowserElement root)
    {
        Log.WriteLine("[BrowserControl] LoadDocument...");

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
        var scrolled = new Point(e.X + AutoScrollPosition.X, e.Y + AutoScrollPosition.Y);
        DispatchMouseEvent("mousedown", scrolled, e.Button);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var scrolled = new Point(e.X + AutoScrollPosition.X, e.Y + AutoScrollPosition.Y);

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
        if (_displayList == null)
            return;

        BrowserElement? found = null;

        for (int i = _displayList.Count - 1; i >= 0; i--)
        {
            var item = _displayList[i];
            if (item.Element != null && item.Bounds.Contains(pos.X, pos.Y))
            {
                found = item.Element;
                break;
            }
        }

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
        element.IsHovered = true;
        foreach (var child in element.Children)
            child.IsHovered = true;
    }

    private static void ClearHoverRecursive(BrowserElement element)
    {
        element.IsHovered = false;
        foreach (var child in element.Children)
            child.IsHovered = false;
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
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_displayList == null)
            return;

        e.Graphics.TranslateTransform(
            AutoScrollPosition.X,
            AutoScrollPosition.Y);

        var renderer = new GdiRenderer();
        renderer.Render(e.Graphics, _displayList);
    }

    private void DispatchMouseEvent(
        string eventType,
        Point pos,
        MouseButtons button = MouseButtons.None)
    {
        if (_displayList == null)
            return;

        for (int i = _displayList.Count - 1; i >= 0; i--)
        {
            var item = _displayList[i];

            if (item.Element == null)
                continue;

            if (!item.Bounds.Contains(pos.X, pos.Y))
                continue;

            var element = item.Element;

            if (!element.EventListeners
                    .TryGetValue(eventType,
                        out var listeners))
                break;

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
                altKey: ModifierKeys
                    .HasFlag(Keys.Alt),
                ctrlKey: ModifierKeys
                    .HasFlag(Keys.Control),
                shiftKey: ModifierKeys
                    .HasFlag(Keys.Shift),
                metaKey: false);

            var toRemove = new List<EventListenerInfo>();

            foreach (var info in listeners)
            {
                if (info.Callback is
                    Action<JsMouseEvent> cb)
                {
                    cb(jsEvent);

                    if (info.Once)
                        toRemove.Add(info);
                }
            }

            foreach (var info in toRemove)
                listeners.Remove(info);

            break;
        }
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
        engine.SetGlobal("alert",
            (string message) =>
                jsWindow.alert(message));
        engine.SetGlobal("setTimeout",
            (Delegate callback, int delay) =>
                jsWindow.setTimeout(callback, delay));
        engine.SetGlobal("clearTimeout",
            (int id) =>
                jsWindow.clearTimeout(id));

        foreach (var script in scripts)
        {
            Log.WriteLine($"  [Script] executing script...");
            engine.Execute(script);
        }

        RegisterOnHandlers(engine, _document);
    }

    private static void
        RegisterOnHandlers(
            JsEngine engine,
            BrowserElement root)
    {
        foreach (var (eventType, handlerCode)
            in root.OnEventHandlers)
        {
            if (string.IsNullOrEmpty(root.Id))
            {
                Log.WriteLine(
                    "  [WARN] on* handler requires element with id, " +
                    $"skipping on{eventType}");
                continue;
            }

            var js = $"document.getElementById('{root.Id}')" +
                     $".addEventListener('{eventType}', " +
                     $"function(event) {{ {handlerCode} }});";

            Log.WriteLine(
                $"  [RegHandler] {js}");

            try
            {
                engine.Execute(js);
            }
            catch (Exception ex)
            {
                Log.WriteLine(
                    $"  [ERROR] registering on{eventType}: {ex.Message}");
            }
        }

        foreach (var child in root.Children)
        {
            RegisterOnHandlers(engine, child);
        }
    }
}
