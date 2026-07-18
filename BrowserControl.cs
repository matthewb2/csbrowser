using CSBrowser.Dom;
using CSBrowser.JavaScript;
using CSBrowser.Layout;
using CSBrowser.Render;

namespace CSBrowser;

public sealed class BrowserControl
    : Control
{
    private List<DisplayItem>? _displayList;
    private BrowserElement? _document;
    private LayoutNode? _layoutRoot;

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

        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        DispatchMouseEvent("mousedown", e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        DispatchMouseEvent("mousemove", e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_displayList == null)
            return;

        var renderer = new GdiRenderer();
        renderer.Render(e.Graphics, _displayList);
    }

    private void DispatchMouseEvent(
        string eventType,
        MouseEventArgs e)
    {
        if (_displayList == null)
            return;

        for (int i = _displayList.Count - 1; i >= 0; i--)
        {
            var item = _displayList[i];

            if (item.Element == null)
                continue;

            if (!item.Bounds.Contains(e.X, e.Y))
                continue;

            var element = item.Element;

            if (!element.EventListeners
                    .TryGetValue(eventType,
                        out var listeners))
                break;

            var screen = PointToScreen(
                new Point(e.X, e.Y));

            var jsEvent = new JsMouseEvent(
                type: eventType,
                clientX: e.X,
                clientY: e.Y,
                screenX: screen.X,
                screenY: screen.Y,
                button: e.Button switch
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

            foreach (var listener in listeners)
            {
                if (listener is
                    Action<JsMouseEvent> cb)
                {
                    cb(jsEvent);
                }
            }

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

        foreach (var script in scripts)
        {
            Log.WriteLine($"  [Script] executing script...");
            engine.Execute(script);
        }

        // Register on* HTML attribute handlers
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
