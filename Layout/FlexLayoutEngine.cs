namespace CSBrowser.Layout;

public sealed class FlexLayoutEngine
{
    private readonly LayoutEngine _blockEngine;

    public FlexLayoutEngine(LayoutEngine blockEngine)
    {
        _blockEngine = blockEngine;
    }

    public void LayoutFlex(
        LayoutNode node,
        float x,
        float y,
        float width)
    {
        Log.WriteLine(
            $"[Flex] container <{node.Element?.TagName}> at ({x},{y}) w={width} dir={node.Style.FlexDirection}");

        if (node.Style.FlexDirection == FlexDirection.Row)
            LayoutRow(node, x, y, width);
        else
            LayoutColumn(node, x, y, width);
    }

    private float EstimateChildWidth(LayoutNode child)
    {
        if (child.Element != null && !string.IsNullOrEmpty(child.Element.Text))
            return child.Element.Text.Length * child.Style.FontSize * 0.85f + 6;
        return child.Style.FontSize * 4;
    }

    private void LayoutRow(
        LayoutNode node,
        float x,
        float y,
        float width)
    {
        float cursorX = x + node.Style.MarginLeft;
        float maxHeight = 0f;

        foreach (var child in node.Children)
        {
            float childContentW = EstimateChildWidth(child);
            _blockEngine.LayoutBlock(child, cursorX, y + node.Style.MarginTop, childContentW);

            child.Bounds = new RectangleF(
                cursorX + child.Style.MarginLeft,
                y + node.Style.MarginTop + child.Style.MarginTop,
                childContentW,
                child.Bounds.Height);

            Log.WriteLine(
                $"[Flex] Row item <{child.Element?.TagName}> -> ({child.Bounds.X:F0},{child.Bounds.Y:F0}) size=({child.Bounds.Width:F0}x{child.Bounds.Height:F0})");

            float childW = childContentW + child.Style.MarginLeft + child.Style.MarginRight;
            float childH = child.Bounds.Height + child.Style.MarginTop + child.Style.MarginBottom;

            cursorX += childW;
            if (childH > maxHeight) maxHeight = childH;
        }

        float totalH = maxHeight + node.Style.MarginTop + node.Style.MarginBottom;
        node.Bounds = new RectangleF(x, y, width, totalH);
    }

    private void LayoutColumn(
        LayoutNode node,
        float x,
        float y,
        float width)
    {
        float contentX = x + node.Style.MarginLeft;
        float cursorY = y + node.Style.MarginTop;
        float maxWidth = 0f;

        foreach (var child in node.Children)
        {
            _blockEngine.LayoutBlock(child, contentX, cursorY, width);

            float childW = child.Bounds.Width + child.Style.MarginLeft + child.Style.MarginRight;
            float childH = child.Bounds.Height + child.Style.MarginTop + child.Style.MarginBottom;

            child.Bounds = new RectangleF(
                contentX + child.Style.MarginLeft,
                cursorY + child.Style.MarginTop,
                child.Bounds.Width,
                child.Bounds.Height);

            Log.WriteLine(
                $"[Flex] Col item <{child.Element?.TagName}> -> ({child.Bounds.X:F0},{child.Bounds.Y:F0}) size=({child.Bounds.Width:F0}x{child.Bounds.Height:F0})");

            cursorY += childH;
            if (childW > maxWidth) maxWidth = childW;
        }

        float totalW = (maxWidth > width ? maxWidth : width)
                        + node.Style.MarginLeft + node.Style.MarginRight;
        float totalH = cursorY - (y + node.Style.MarginTop)
                        + node.Style.MarginTop + node.Style.MarginBottom;
        node.Bounds = new RectangleF(x, y, totalW, totalH);
    }
}
