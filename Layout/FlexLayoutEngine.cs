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

    private float GetFlexBasis(LayoutNode child)
    {
        if (child.Style.FlexBasis > 0)
            return child.Style.FlexBasis;
        if (child.Style.Width.HasValue)
            return child.Style.Width.Value;
        return EstimateChildWidth(child);
    }

    private void LayoutRow(
        LayoutNode node,
        float x,
        float y,
        float width)
    {
        float gap = node.Style.Gap;
        bool doWrap = node.Style.FlexWrap == FlexWrapType.Wrap;

        float padLeft = node.Style.BorderLeft.Width + node.Style.PaddingLeft;
        float padRight = node.Style.BorderRight.Width + node.Style.PaddingRight;
        float contentX = x + node.Style.MarginLeft + padLeft;
        float contentWidth = Math.Max(0, width - padLeft - padRight);
        float containerTop = y + node.Style.MarginTop + node.Style.BorderTop.Width + node.Style.PaddingTop;

        var children = node.Children;

        if (doWrap)
            LayoutRowWrap(node, contentX, containerTop, contentWidth, width, gap);
        else
            LayoutRowNoWrap(node, contentX, containerTop, contentWidth, width, gap);
    }

    private void LayoutRowNoWrap(
        LayoutNode node,
        float contentX,
        float containerTop,
        float contentWidth,
        float totalWidth,
        float gap)
    {
        var children = node.Children;
        int count = children.Count;

        float totalBasis = 0;
        float totalGrow = 0;
        float totalGap = gap > 0 && count > 1 ? gap * (count - 1) : 0;
        int growableCount = 0;

        for (int i = 0; i < count; i++)
        {
            float basis = GetFlexBasis(children[i]);
            totalBasis += basis;
            if (children[i].Style.FlexGrow > 0)
            {
                totalGrow += children[i].Style.FlexGrow;
                growableCount++;
            }
        }

        float remaining = Math.Max(0, contentWidth - totalBasis - totalGap);

        float cursorX = contentX;
        float maxHeight = 0f;

        for (int i = 0; i < count; i++)
        {
            var child = children[i];
            float basis = GetFlexBasis(child);
            float extra = (growableCount > 0 && child.Style.FlexGrow > 0)
                ? remaining * child.Style.FlexGrow / totalGrow
                : 0;
            float finalW = basis + extra;

            _blockEngine.LayoutBlock(child, cursorX, containerTop, finalW);

            child.Bounds = new RectangleF(
                cursorX + child.Style.MarginLeft,
                containerTop + child.Style.MarginTop,
                child.Bounds.Width,
                child.Bounds.Height);

            float childW = child.Bounds.Width + child.Style.MarginLeft + child.Style.MarginRight;
            float childH = child.Bounds.Height + child.Style.MarginTop + child.Style.MarginBottom;

            Log.WriteLine(
                $"[Flex] Row item <{child.Element?.TagName ?? child.BrowserElement?.TagName ?? "?"}> -> ({child.Bounds.X:F0},{child.Bounds.Y:F0}) size=({child.Bounds.Width:F0}x{child.Bounds.Height:F0}) basis={basis:F0} final={finalW:F0}");

            cursorX += childW + (i < count - 1 ? gap : 0);
            if (childH > maxHeight) maxHeight = childH;
        }

        float totalH = maxHeight + node.Style.MarginTop + node.Style.BorderTop.Width + node.Style.PaddingTop
                      + node.Style.BorderBottom.Width + node.Style.PaddingBottom + node.Style.MarginBottom;
        float containerX = contentX - node.Style.BorderLeft.Width - node.Style.PaddingLeft - node.Style.MarginLeft;
        node.Bounds = new RectangleF(containerX, containerTop - node.Style.BorderTop.Width - node.Style.PaddingTop - node.Style.MarginTop, totalWidth, totalH);
    }

    private void LayoutRowWrap(
        LayoutNode node,
        float contentX,
        float containerTop,
        float contentWidth,
        float totalWidth,
        float gap)
    {
        var children = node.Children;
        int count = children.Count;

        var bases = new float[count];
        var growValues = new float[count];
        float totalGrow = 0;

        for (int i = 0; i < count; i++)
        {
            bases[i] = GetFlexBasis(children[i]);
            growValues[i] = children[i].Style.FlexGrow;
            totalGrow += growValues[i];
        }

        var lines = new List<List<int>>();
        var currentLine = new List<int>();
        float lineW = 0;

        for (int i = 0; i < count; i++)
        {
            float extra = currentLine.Count > 0 ? gap : 0;

            if (currentLine.Count > 0 && lineW + extra + bases[i] > contentWidth)
            {
                lines.Add(currentLine);
                currentLine = new List<int>();
                lineW = 0;
            }

            currentLine.Add(i);
            lineW += (currentLine.Count > 1 ? gap : 0) + bases[i];
        }

        if (currentLine.Count > 0)
            lines.Add(currentLine);

        float lineY = containerTop;

        foreach (var line in lines)
        {
            float lineBasisSum = 0;
            float lineGrowSum = 0;

            foreach (var idx in line)
            {
                lineBasisSum += bases[idx];
                lineGrowSum += growValues[idx];
            }

            int lineGapCount = line.Count > 1 ? line.Count - 1 : 0;
            float lineGaps = gap * lineGapCount;
            float lineAvailable = contentWidth - lineBasisSum - lineGaps;
            float lineHeight = 0;
            float cursorX = contentX;

            for (int li = 0; li < line.Count; li++)
            {
                int i = line[li];
                var child = children[i];
                float extra = (lineGrowSum > 0 && growValues[i] > 0)
                    ? lineAvailable * growValues[i] / lineGrowSum
                    : 0;
                float finalW = bases[i] + extra;

                _blockEngine.LayoutBlock(child, cursorX, lineY, finalW);

                child.Bounds = new RectangleF(
                    cursorX + child.Style.MarginLeft,
                    lineY + child.Style.MarginTop,
                    child.Bounds.Width,
                    child.Bounds.Height);

                float childW = child.Bounds.Width + child.Style.MarginLeft + child.Style.MarginRight;
                float childH = child.Bounds.Height + child.Style.MarginTop + child.Style.MarginBottom;

                Log.WriteLine(
                    $"[Flex] Wrap item <{child.Element?.TagName ?? child.BrowserElement?.TagName ?? "?"}> -> ({child.Bounds.X:F0},{child.Bounds.Y:F0}) size=({child.Bounds.Width:F0}x{child.Bounds.Height:F0}) basis={bases[i]:F0} final={finalW:F0}");

                cursorX += childW + gap;

                if (childH > lineHeight)
                    lineHeight = childH;
            }

            lineY += lineHeight;
        }

        float totalH = lineY - containerTop + node.Style.MarginTop + node.Style.BorderTop.Width + node.Style.PaddingTop
                      + node.Style.BorderBottom.Width + node.Style.PaddingBottom + node.Style.MarginBottom;
        float containerX = contentX - node.Style.BorderLeft.Width - node.Style.PaddingLeft - node.Style.MarginLeft;
        node.Bounds = new RectangleF(containerX, containerTop - node.Style.BorderTop.Width - node.Style.PaddingTop - node.Style.MarginTop, totalWidth, totalH);
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
