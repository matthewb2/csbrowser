using CSBrowser.Dom;

namespace CSBrowser.Layout;

public sealed class LayoutEngine
{
    public LayoutNode Layout(
        BrowserElement root,
        float width)
    {
        var layoutRoot =
            Build(root);

        LayoutBlock(
            layoutRoot,
            0,
            0,
            width);

        return layoutRoot;
    }

    private LayoutNode Build(
        BrowserElement element)
    {
        var node =
            new LayoutNode();

        node.Element =
            element;

        node.Style =
            element.Style;

        foreach (var child
            in element.Children)
        {
            node.Children.Add(
                Build(child));
        }

        return node;
    }

    private float LayoutBlock(
        LayoutNode node,
        float x,
        float y,
        float width)
    {
        float currentY =
            y + node.Style.MarginTop;

        foreach (var child
            in node.Children)
        {
            currentY +=
                LayoutBlock(
                    child,
                    x +
                    node.Style.MarginLeft,
                    currentY,
                    width);
        }

        float height =
            node.Style.FontSize + 10;

        node.Bounds =
            new RectangleF(
                x +
                node.Style.MarginLeft,
                y +
                node.Style.MarginTop,
                width,
                height);

        return
            height +
            node.Style.MarginTop +
            node.Style.MarginBottom;
    }
}