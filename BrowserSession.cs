using CSBrowser.Dom;
using CSBrowser.JavaScript;
using CSBrowser.Layout;
using CSBrowser.Render;

namespace CSBrowser;

public sealed class BrowserSession : IDisposable
{
    private BrowserElement? _document;
    private LayoutNode? _layoutRoot;
    private List<DisplayItem>? _displayList;
    private QuadtreeNode? _hitTestTree;

    public BrowserElement? Document => _document;
    public LayoutNode? LayoutRoot => _layoutRoot;
    public List<DisplayItem>? DisplayList => _displayList;
    public QuadtreeNode? HitTestTree => _hitTestTree;
    public readonly Dictionary<BrowserElement, RectangleF> ElementBoundsCache = new();
    public JsEngine? JsEngine { get; private set; }
    private int _width;

    public void LoadDocument(BrowserElement root, int width)
    {
        Log.WriteLine("[BrowserSession] LoadDocument started.");

        if (_document != null)
            _document.Unref();

        _document = root;
        root.Ref();

        _width = width;
        ExecuteScripts();
        Relayout(width);
        //Log.WriteLine("[BrowserSession] LoadDocument finished.");
    }

    public void Relayout(int width)
    {
        _width = width;

        if (_document == null)
            return;

        //Log.WriteLine("[BrowserSession] Relayout started. Width = " + width);

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
        _layoutRoot = layout.Layout(_document, width);

        var builder = new DisplayListBuilder();
        _displayList = builder.Build(_layoutRoot);

        //Log.WriteLine($"[BrowserSession] Built new display list. Total DisplayItems: {_displayList?.Count ?? 0}");

        if (_layoutRoot != null)
        {
            float docHeight = _layoutRoot.Bounds.Y + _layoutRoot.Bounds.Height + 20;
            var scrollSize = new Size(width, (int)docHeight);
            Log.WriteLine($"[BrowserSession] Updated AutoScrollMinSize: {scrollSize}");
        }

        BuildHitTestTree();
    }

    public void BuildHitTestTree()
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

    public RectangleF RebuildDisplayList()
    {
        if (_layoutRoot == null)
            return RectangleF.Empty;

        if (_displayList != null)
        {
            foreach (var item in _displayList)
                item.Unref();
        }

        var builder = new DisplayListBuilder();
        _displayList = builder.Build(_layoutRoot);

        ElementBoundsCache.Clear();
        foreach (var item in _displayList)
        {
            if (item.Element != null)
            {
                if (ElementBoundsCache.TryGetValue(item.Element, out var existing))
                {
                    ElementBoundsCache[item.Element] = RectangleF.Union(existing, item.Bounds);
                }
                else
                {
                    ElementBoundsCache[item.Element] = item.Bounds;
                }
            }
        }

        float treeW = Math.Max(_layoutRoot.Bounds.Right + 10, 1);
        float treeH = Math.Max(_layoutRoot.Bounds.Bottom + 10, 1);
        _hitTestTree = new QuadtreeNode(new RectangleF(0, 0, treeW, treeH));
        if (_displayList != null)
        {
            foreach (var item in _displayList)
                _hitTestTree.Insert(item);
        }

        return _layoutRoot.Bounds;
    }

    public void ExecuteScripts()
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

        JsEngine = new JsEngine();
        JsEngine.SetGlobal("document", jsDoc);
        JsEngine.SetGlobal("window", jsWindow);
        JsEngine.SetGlobal("console", jsConsole);
        JsEngine.SetGlobal("alert", (string message) => jsWindow.alert(message));
        JsEngine.SetGlobal("setTimeout", (Delegate callback, int delay) => jsWindow.setTimeout(callback, delay));
        JsEngine.SetGlobal("clearTimeout", (int id) => jsWindow.clearTimeout(id));

        foreach (var script in scripts)
        {
            JsEngine.Execute(script);
        }

        RegisterOnHandlers(JsEngine, _document);
    }

    private static void RegisterOnHandlers(JsEngine engine, BrowserElement root)
    {
        foreach (var (eventType, handlerCode) in root.OnEventHandlers)
        {
            if (string.IsNullOrEmpty(root.Id))
                continue;

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

    public void Dispose()
    {
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

        if (_document != null)
        {
            _document.Unref();
            _document = null;
        }

        ElementBoundsCache.Clear();
    }
}
