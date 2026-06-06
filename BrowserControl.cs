using CSBrowser.Dom;
using CSBrowser.JavaScript;
using CSBrowser.Layout;
using CSBrowser.Render;

namespace CSBrowser;

public sealed class BrowserControl
    : Control
{
    private List<DisplayItem> _displayList = new();
    private BrowserElement? _document;

    public void LoadDocument(BrowserElement root)
    {
        Log.WriteLine("[BrowserControl] LoadDocument...");

        _document = root;
        ExecuteDocumentScripts();
        Relayout();
    }

    private void Relayout()
    {
        if (_document == null)
            return;

        Log.WriteLine("[BrowserControl] Relayout...");

        var layout = new LayoutEngine();
        var layoutRoot = layout.Layout(_document, Width);

        var builder = new DisplayListBuilder();
        _displayList = builder.Build(layoutRoot);

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

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
