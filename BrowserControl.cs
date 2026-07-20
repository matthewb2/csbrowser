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
    private Bitmap? _renderCache;
    private bool _cacheDirty = true;
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
        if (disposing)
        {
            _renderCache?.Dispose();
            _renderCache = null;
        }
        base.Dispose(disposing);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        InvalidateCache();
        Invalidate();
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

        BuildHitTestTree();

        if (_layoutRoot != null)
        {
            float docHeight = _layoutRoot.Bounds.Y + _layoutRoot.Bounds.Height + 20;
            AutoScrollMinSize = new Size(Width, (int)docHeight);
        }

        InvalidateCache();
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
        if (_hitTestTree == null)
            return;

        var foundItem = _hitTestTree.HitTest(pos.X, pos.Y);
        BrowserElement? found = foundItem?.Element;

        Log.WriteLine($"[Hover] hit element: {(found != null ? $"<{found.TagName}> id={found.Id} class={found.ClassName}" : "null")}");

        BrowserElement? foundAncestor = found?.FindAncestorWithPseudoStyle();
        BrowserElement? oldAncestor = _hoveredElement?.FindAncestorWithPseudoStyle();

        Log.WriteLine($"[Hover] foundAncestor: {(foundAncestor != null ? $"<{foundAncestor.TagName}> pseudoKeys=[{string.Join(",", foundAncestor.PseudoStyles.Keys)}]" : "null")}");
        Log.WriteLine($"[Hover] oldAncestor: {(oldAncestor != null ? $"<{oldAncestor.TagName}>" : "null")}");

        if (foundAncestor == oldAncestor)
        {
            Log.WriteLine($"[Hover] no change, skip");
            return;
        }

        if (oldAncestor != null)
        {
            Log.WriteLine($"[Hover] CLEAR hover on <{oldAncestor.TagName}>");
            ClearHoverRecursive(oldAncestor);
            RebuildDisplayList();
        }

        _hoveredElement = found;

        if (foundAncestor != null)
        {
            Log.WriteLine($"[Hover] SET hover on <{foundAncestor.TagName}>, state={foundAncestor.State}");
            SetHoverRecursive(foundAncestor);
            Log.WriteLine($"[Hover] after set: state={foundAncestor.State}, effective.TextDecoration={foundAncestor.EffectiveStyle.TextDecoration}");
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
        InvalidateCache();
        Invalidate();
    }

    private void InvalidateCache()
    {
        _cacheDirty = true;
        if (_renderCache != null)
        {
            Log.WriteLine("[Cache] invalidated, disposing old bitmap");
            _renderCache.Dispose();
            _renderCache = null;
        }
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

        if (_cacheDirty)
        {
            Log.WriteLine("[Cache] rebuilding render cache...");

            int cacheW = Math.Max(Width, 1);
            int cacheH = Math.Max(AutoScrollMinSize.Height, Height);

            _renderCache?.Dispose();
            _renderCache = new Bitmap(cacheW, cacheH);

            using var cacheGraphics = Graphics.FromImage(_renderCache);
            cacheGraphics.Clear(BackColor);

            var renderer = new GdiRenderer();
            foreach (var item in _displayList)
                renderer.RenderItem(cacheGraphics, item);

            _cacheDirty = false;

            Log.WriteLine($"[Cache] rebuilt: {cacheW}x{cacheH}, items={_displayList.Count}");
        }

        e.Graphics.TranslateTransform(
            AutoScrollPosition.X,
            AutoScrollPosition.Y);

        if (_renderCache != null)
        {
            e.Graphics.DrawImageUnscaled(_renderCache, 0, 0);
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
