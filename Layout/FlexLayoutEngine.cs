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
            $"[Flex] container <{node.Element?.TagName ?? node.BrowserElement?.TagName ?? "?"}> at ({x},{y}) w={width} dir={node.Style.FlexDirection} wrap={node.Style.FlexWrap} gap={node.Style.Gap}");

        if (node.Style.FlexDirection == FlexDirection.Row)
            LayoutRow(node, x, y, width);
        else
            LayoutColumn(node, x, y, width);
    }

    private float EstimateChildWidth(LayoutNode child)
    {
        var text = child.BrowserElement?.Text;
        if (!string.IsNullOrEmpty(text))
            return text.Length * child.Style.FontSize * 0.85f + 6;
        return child.Style.FontSize * 4;
    }

    private void LayoutRow(
        LayoutNode node,
        float x,
        float y,
        float width)
    {
        float gap = node.Style.Gap;
        bool doWrap = node.Style.FlexWrap == FlexWrapType.Wrap;

        float containerTop = y + node.Style.MarginTop + node.Style.BorderTop.Width + node.Style.PaddingTop;
        float cursorX = x + node.Style.MarginLeft + node.Style.BorderLeft.Width + node.Style.PaddingLeft;
        float startY = containerTop;

        var lines = new List<(List<LayoutNode> items, float width)>();
        var currentLineItems = new List<LayoutNode>();
        float currentLineWidth = 0f;

        foreach (var child in node.Children)
        {
            float childContentW = EstimateChildWidth(child);

            _blockEngine.LayoutBlock(child, cursorX, containerTop, childContentW);

            child.Bounds = new RectangleF(
                cursorX + child.Style.MarginLeft,
                containerTop + child.Style.MarginTop,
                childContentW,
                child.Bounds.Height);

            float childW = childContentW + child.Style.MarginLeft + child.Style.MarginRight;

            float neededExtra = currentLineItems.Count > 0 ? gap : 0;

            if (doWrap && currentLineItems.Count > 0 &&
                currentLineWidth + neededExtra + childW > width)
            {
                lines.Add((currentLineItems, currentLineWidth));
                currentLineItems = new List<LayoutNode>();
                currentLineWidth = 0f;
                neededExtra = 0f;
            }

            currentLineItems.Add(child);
            currentLineWidth += neededExtra + childW;

            cursorX += neededExtra + childW;

            Log.WriteLine(
                $"[Flex] Row item <{child.Element?.TagName ?? child.BrowserElement?.TagName ?? "?"}> -> ({child.Bounds.X:F0},{child.Bounds.Y:F0}) size=({child.Bounds.Width:F0}x{child.Bounds.Height:F0})");
        }

        if (currentLineItems.Count > 0)
            lines.Add((currentLineItems, currentLineWidth));

        float allMaxHeight = 0f;
        float lineY = startY;
        float totalContentWidth = 0f;

        foreach (var (items, lineW) in lines)
        {
            float lineHeight = 0f;

            foreach (var child in items)
            {
                float childH = child.Bounds.Height + child.Style.MarginTop + child.Style.MarginBottom;
                if (childH > lineHeight) lineHeight = childH;
            }

            foreach (var child in items)
            {
                child.Bounds = new RectangleF(
                    child.Bounds.X,
                    lineY + child.Style.MarginTop,
                    child.Bounds.Width,
                    child.Bounds.Height);
            }

            lineY += lineHeight;
            if (lineW > totalContentWidth) totalContentWidth = lineW;
        }

        allMaxHeight = lineY - startY;

        float padLeft = node.Style.PaddingLeft + node.Style.BorderLeft.Width;
        float padRight = node.Style.PaddingRight + node.Style.BorderRight.Width;
        float totalH = allMaxHeight + node.Style.MarginTop + node.Style.BorderTop.Width + node.Style.PaddingTop
                      + node.Style.BorderBottom.Width + node.Style.PaddingBottom + node.Style.MarginBottom;

        node.Bounds = new RectangleF(x, y, width, totalH);

        Log.WriteLine($"[Flex] Container result: {lines.Count} line(s), totalW={totalContentWidth:F0} totalH={totalH:F0}");
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
        float gap = node.Style.Gap;

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
                $"[Flex] Col item <{child.Element?.TagName ?? child.BrowserElement?.TagName ?? "?"}> -> ({child.Bounds.X:F0},{child.Bounds.Y:F0}) size=({child.Bounds.Width:F0}x{child.Bounds.Height:F0})");

            cursorY += childH + gap;
            if (childW > maxWidth) maxWidth = childW;
        }

        float totalW = (maxWidth > width ? maxWidth : width)
                        + node.Style.MarginLeft + node.Style.MarginRight;
        float totalH = cursorY - (y + node.Style.MarginTop)
                        + node.Style.MarginTop + node.Style.MarginBottom;
        node.Bounds = new RectangleF(x, y, totalW, totalH);
    }
}
