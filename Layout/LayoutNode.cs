using CSBrowser.Dom;

namespace CSBrowser.Layout;

public sealed class LayoutNode : RefCounted
{
    public BrowserElement Element = null!;
    public RectangleF Bounds;
    public ComputedStyle Style = null!;
    public List<LayoutNode> Children = new();

    protected override void Cleanup()
    {
        if (Element != null)
        {
            Element.Unref();
            Element = null!;
        }
        foreach (var child in Children)
            child.Unref();
        Children.Clear();
    }
}
