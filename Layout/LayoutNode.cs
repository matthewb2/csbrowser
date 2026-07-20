using AngleSharp.Dom;
using CSBrowser.Dom;

namespace CSBrowser.Layout;

public sealed class LayoutNode : RefCounted
{
    public IElement? Element;
    public BrowserElement? BrowserElement;
    public RectangleF Bounds;
    public ComputedStyle Style = null!;
    public TextAlignType ResolvedTextAlign;
    public List<LayoutNode> Children = new();

    protected override void Cleanup()
    {
        Element = null;

        if (BrowserElement != null)
        {
            BrowserElement.Unref();
            BrowserElement = null!;
        }

        foreach (var child in Children)
            child.Unref();
        Children.Clear();
    }
}
