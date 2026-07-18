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

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_displayList == null)
            return;

        var renderer = new GdiRenderer();
        renderer.Render(e.Graphics, _displayList);
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

        var engine = new JsEngine();
        engine.SetGlobal("document", jsDoc);
        engine.SetGlobal("window", jsWindow);

        foreach (var script in scripts)
        {
            Log.WriteLine($"  [Script] executing script...");
            engine.Execute(script);
        }
    }
}
