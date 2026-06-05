using CSBrowser.Dom;
using CSBrowser.Layout;
using CSBrowser.Render;

namespace CSBrowser;

public sealed class BrowserControl
    : Control
{
    private List<DisplayItem>
        _displayList = new();

    public void LoadDocument(
        BrowserElement root)
    {
        var layout =
            new LayoutEngine();

        var layoutRoot =
            layout.Layout(
                root,
                Width);

        var builder =
            new DisplayListBuilder();

        _displayList =
            builder.Build(layoutRoot);

        Invalidate();
    }

    protected override void OnPaint(
        PaintEventArgs e)
    {
        base.OnPaint(e);

        var renderer =
            new GdiRenderer();

        renderer.Render(
            e.Graphics,
            _displayList);
    }
}