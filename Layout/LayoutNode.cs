using CSBrowser.Dom;

namespace CSBrowser.Layout;

public sealed class LayoutNode
{
    public BrowserElement Element
        = null!;

    public RectangleF Bounds;

    public ComputedStyle Style
        = null!;

    public List<LayoutNode>
        Children = new();
}