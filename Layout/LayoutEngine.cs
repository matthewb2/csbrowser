using CSBrowser.Dom;

namespace CSBrowser.Layout;

public sealed class LayoutEngine
{
    private readonly FlexLayoutEngine _flex;

    public LayoutEngine()
    {
        _flex = new FlexLayoutEngine(this);
    }

    public LayoutNode Layout(
        BrowserElement root,
        float width)
    {
        var layoutRoot = Build(root);
        LayoutBlock(layoutRoot, 0, 0, width);
        return layoutRoot;
    }

    private LayoutNode Build(BrowserElement element)
    {
        var node = new LayoutNode();
        node.Element = element;
        element.Ref();
        node.Style = element.Style;

        foreach (var child in element.Children)
        {
            var childNode = Build(child);
            node.Children.Add(childNode);
        }

        return node;
    }

    public void LayoutBlock(
        LayoutNode node,
        float x,
        float y,
        float width)
    {
        Log.WriteLine(
            $"[Layout] <{node.Element?.TagName}> at ({x},{y}) w={width} disp={node.Style.Display}");

        if (node.Style.Display == DisplayType.None)
        {
            node.Bounds = RectangleF.Empty;
            return;
        }

        if (node.Style.Display == DisplayType.Flex)
        {
            _flex.LayoutFlex(node, x, y, width);
            return;
        }

        float currentY = y + node.Style.MarginTop;

        foreach (var child in node.Children)
        {
            LayoutBlock(
                child,
                x + node.Style.MarginLeft,
                currentY,
                width);
            currentY += child.Bounds.Height
                        + child.Style.MarginTop
                        + child.Style.MarginBottom;
        }

        float height = node.Style.FontSize + 10;

        node.Bounds = new RectangleF(
            x + node.Style.MarginLeft,
            y + node.Style.MarginTop,
            width,
            height);

        Log.WriteLine(
            $"[Layout] <{node.Element?.TagName}> bounds=({node.Bounds.X:F0},{node.Bounds.Y:F0} {node.Bounds.Width:F0}x{node.Bounds.Height:F0})");
    }
}
