using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using CSBrowser.Dom;

namespace CSBrowser.Layout;

public sealed class LayoutEngine
{
    private readonly FlexLayoutEngine _flex;

    public LayoutEngine()
    {
        _flex = new FlexLayoutEngine(this);
    }

    public LayoutNode Layout(BrowserElement root, float width)
    {
        var layoutRoot = Build(root, null);
        LayoutBlock(layoutRoot, 0, 0, width);
        return layoutRoot;
    }

    public LayoutNode Build(BrowserElement element, TextAlignType? inheritedTextAlign = null)
    {
        var node = new LayoutNode();
        node.BrowserElement = element;
        element.Ref();

        node.Element = element.Source;
        node.Style = element.Style;

        ApplyDefaultStyles(node);

        node.ResolvedTextAlign =
            element.Style.SetProperties.Contains("text-align")
                ? element.Style.TextAlign
                : inheritedTextAlign ?? element.Style.TextAlign;

        foreach (var child in element.Children)
        {
            var childNode = Build(child, node.ResolvedTextAlign);
            node.Children.Add(childNode);
        }

        return node;
    }

    private void ApplyDefaultStyles(LayoutNode node)
    {
        if (node.Element == null)
            return;

        switch (node.Element)
        {
            case IHtmlHeadingElement:
                var level = node.Element.LocalName;
                node.Style.FontSize = level switch
                {
                    "h1" => 32,
                    "h2" => 24,
                    "h3" => 18.5f,
                    "h4" => 16,
                    "h5" => 13.3f,
                    "h6" => 10.7f,
                    _ => 16
                };
                if (node.Style.MarginTop == 0)
                    node.Style.MarginTop = node.Style.FontSize * 0.67f;
                if (node.Style.MarginBottom == 0)
                    node.Style.MarginBottom = node.Style.FontSize * 0.67f;
                break;

            case IHtmlParagraphElement:
                if (node.Style.MarginTop == 0)
                    node.Style.MarginTop = 16;
                if (node.Style.MarginBottom == 0)
                    node.Style.MarginBottom = 16;
                break;

            case IHtmlListItemElement:
                if (node.Style.MarginTop == 0)
                    node.Style.MarginTop = 4;
                if (node.Style.MarginBottom == 0)
                    node.Style.MarginBottom = 4;
                break;

            case IHtmlUnorderedListElement:
            case IHtmlOrderedListElement:
                if (node.Style.MarginTop == 0)
                    node.Style.MarginTop = 16;
                if (node.Style.MarginBottom == 0)
                    node.Style.MarginBottom = 16;
                if (node.Style.MarginLeft == 0)
                    node.Style.MarginLeft = 40;
                break;

            case IHtmlTableElement:
                node.Style.Display = DisplayType.Block;
                if (node.Style.MarginTop == 0)
                    node.Style.MarginTop = 16;
                if (node.Style.MarginBottom == 0)
                    node.Style.MarginBottom = 16;
                break;

            case IHtmlTableRowElement:
                node.Style.Display = DisplayType.Flex;
                node.Style.FlexDirection = FlexDirection.Row;
                break;

            case IHtmlTableCellElement:
                node.Style.Display = DisplayType.Block;
                break;

            case IHtmlSpanElement:
                node.Style.Display = DisplayType.Inline;
                break;

            case IHtmlImageElement:
                node.Style.Display = DisplayType.Inline;
                break;

            default:
                ApplyDefaultStylesByLocalName(node);
                break;
        }

        if (node.Element.LocalName is "body")
        {
            if (node.Style.MarginTop == 0)
                node.Style.MarginTop = 8;
            if (node.Style.MarginLeft == 0)
                node.Style.MarginLeft = 8;
        }
    }

    private void ApplyDefaultStylesByLocalName(LayoutNode node)
    {
        if (node.Element == null)
            return;

        var local = node.Element.LocalName;

        switch (local)
        {
            case "br":
                node.Style.Display = DisplayType.Inline;
                break;

            case "hr":
                if (node.Style.MarginTop == 0)
                    node.Style.MarginTop = 8;
                if (node.Style.MarginBottom == 0)
                    node.Style.MarginBottom = 8;
                break;

            case "blockquote":
                if (node.Style.MarginTop == 0)
                    node.Style.MarginTop = 16;
                if (node.Style.MarginBottom == 0)
                    node.Style.MarginBottom = 16;
                if (node.Style.MarginLeft == 0)
                    node.Style.MarginLeft = 40;
                if (node.Style.MarginRight == 0)
                    node.Style.MarginRight = 40;
                break;

            case "pre":
                if (node.Style.MarginTop == 0)
                    node.Style.MarginTop = 16;
                if (node.Style.MarginBottom == 0)
                    node.Style.MarginBottom = 16;
                break;

            case "span":
            case "a":
            case "strong":
            case "em":
            case "b":
            case "i":
            case "u":
            case "code":
            case "small":
            case "sub":
            case "sup":
                node.Style.Display = DisplayType.Inline;
                break;
        }
    }

    public void LayoutBlock(LayoutNode node, float x, float y, float width)
    {
        var tagName = node.Element?.TagName ?? "?";

        Log.WriteLine(
            $"[Layout] <{tagName}> at ({x},{y}) w={width} disp={node.Style.Display}");

        if (node.Style.Display == DisplayType.None)
        {
            node.Bounds = RectangleF.Empty;
            return;
        }

        if (node.Style.Display == DisplayType.Inline)
        {
            LayoutInline(node, x, y, width);
            return;
        }

        if (node.Style.Display == DisplayType.Flex)
        {
            _flex.LayoutFlex(node, x, y, width);
            return;
        }

        if (node.Element is IHtmlTableElement)
        {
            LayoutTable(node, x, y, width);
            return;
        }

        float currentY = y + node.Style.MarginTop + node.Style.BorderTop.Width + node.Style.PaddingTop;
        float contentHeight = GetLineHeight(node.Style);

        bool hasInlineChildren = false;
        bool hasBlockChildren = false;
        foreach (var child in node.Children)
        {
            if (child.Style.Display == DisplayType.Inline ||
                child.Style.Display == DisplayType.None)
                hasInlineChildren = true;
            else
                hasBlockChildren = true;
        }

        float outerWidth;
        if (node.Style.Width.HasValue)
        {
            outerWidth = node.Style.Width.Value;
        }
        else
        {
            outerWidth = width;
        }

        float marginLeft = node.Style.MarginLeft;
        float marginRight = node.Style.MarginRight;
        float paddingLeft = node.Style.PaddingLeft;
        float paddingRight = node.Style.PaddingRight;
        float borderLeft = node.Style.BorderLeft.Width;
        float borderRight = node.Style.BorderRight.Width;

        float boundsWidth;
        float contentWidth;

        if (node.Style.BoxSizing == BoxSizingType.BorderBox)
        {
            boundsWidth = outerWidth;
            contentWidth = outerWidth - paddingLeft - paddingRight - borderLeft - borderRight;
        }
        else
        {
            boundsWidth = outerWidth;
            contentWidth = outerWidth - paddingLeft - paddingRight - borderLeft - borderRight;
        }

        float contentX = x + node.Style.MarginLeft + borderLeft + node.Style.PaddingLeft;

        if (hasInlineChildren && !hasBlockChildren)
        {
            float currentX = contentX;
            float lineY = currentY;
            float lineHeight = 0;
            float maxX = contentX + contentWidth;

            var inlineItems = new List<LayoutNode>();
            foreach (var child in node.Children)
            {
                if (child.Style.Display == DisplayType.None)
                    continue;

                LayoutBlock(child, currentX, lineY, contentWidth);

                if (currentX + child.Bounds.Width > maxX && currentX > contentX)
                {
                    ApplyTextAlignLine(inlineItems, contentX, contentWidth, parent: node);
                    inlineItems.Clear();

                    currentX = contentX;
                    lineY += lineHeight + child.Style.MarginTop;
                    lineHeight = 0;

                    LayoutBlock(child, currentX, lineY, contentWidth);
                }

                inlineItems.Add(child);

                currentX += child.Bounds.Width
                            + child.Style.MarginLeft
                            + child.Style.MarginRight;

                if (child.Bounds.Height > lineHeight)
                    lineHeight = child.Bounds.Height;
            }

            ApplyTextAlignLine(inlineItems, contentX, contentWidth, parent: node);

            contentHeight = lineY + lineHeight - y;
        }
        else
        {
            foreach (var child in node.Children)
            {
                LayoutBlock(child, contentX, currentY, contentWidth);

                ApplyTextAlignChild(node, child, contentX, contentWidth);

                currentY += child.Bounds.Height
                            + child.Style.MarginTop
                            + child.Style.MarginBottom;
            }

            if (node.Children.Count > 0)
            {
                var lastChild = node.Children[^1];
                contentHeight = lastChild.Bounds.Y + lastChild.Bounds.Height - y;
            }
        }

        contentHeight += node.Style.PaddingTop + node.Style.PaddingBottom
                         + node.Style.BorderTop.Width + node.Style.BorderBottom.Width;

        if (node.Style.Height.HasValue && node.Style.Height.Value > contentHeight)
            contentHeight = node.Style.Height.Value;

        node.Bounds = new RectangleF(
            x + node.Style.MarginLeft,
            y + node.Style.MarginTop,
            boundsWidth,
            contentHeight);

        Log.WriteLine(
            $"[Layout] <{tagName}> bounds=({node.Bounds.X:F0},{node.Bounds.Y:F0} {node.Bounds.Width:F0}x{node.Bounds.Height:F0})");
    }

    private void LayoutInline(LayoutNode node, float x, float y, float width)
    {
        var tagName = node.Element?.TagName ?? "?";

        float contentWidth;
        float contentHeight;

        if (node.Element is IHtmlImageElement img)
        {
            var wAttr = img.GetAttribute("width");
            var hAttr = img.GetAttribute("height");

            if (float.TryParse(wAttr, out float w) && float.TryParse(hAttr, out float h))
            {
                contentWidth = w;
                contentHeight = h;
            }
            else
            {
                var natural = GetImageNaturalSize(node.BrowserElement?.ImagePath);
                contentWidth = natural.Width;
                contentHeight = natural.Height;
            }
        }
        else if (node.Children.Count > 0)
        {
            float childW = 0;
            float childH = 0;

            foreach (var child in node.Children)
            {
                LayoutBlock(child, x + childW, y, width);

                childW += child.Bounds.Width
                          + child.Style.MarginLeft
                          + child.Style.MarginRight;

                if (child.Bounds.Height > childH)
                    childH = child.Bounds.Height;
            }

            contentWidth = childW;
            contentHeight = childH;
        }
        else
        {
            contentWidth = EstimateInlineWidth(node);
            contentHeight = GetLineHeight(node.Style);
        }

        node.Bounds = new RectangleF(x, y, contentWidth, contentHeight);

        Log.WriteLine(
            $"[Layout:inline] <{tagName}> bounds=({node.Bounds.X:F0},{node.Bounds.Y:F0} {node.Bounds.Width:F0}x{node.Bounds.Height:F0})");
    }

    private float EstimateInlineWidth(LayoutNode node)
    {
        var text = node.BrowserElement?.Text;
        if (!string.IsNullOrEmpty(text))
            return text.Length * node.Style.FontSize * 0.75f + 2;

        return node.Style.FontSize * 2;
    }

    private static SizeF GetImageNaturalSize(string? path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try
            {
                using var img = Image.FromFile(path);
                return new SizeF(img.Width, img.Height);
            }
            catch { }
        }

        return new SizeF(300, 200);
    }

    private void LayoutTable(LayoutNode node, float x, float y, float width)
    {
        var rows = node.Children;
        int rowCount = rows.Count;
        if (rowCount == 0)
        {
            node.Bounds = new RectangleF(x, y, width, 0);
            return;
        }

        int maxCols = 0;
        foreach (var row in rows)
        {
            if (row.Children.Count > maxCols)
                maxCols = row.Children.Count;
        }

        if (maxCols == 0)
            maxCols = 1;

        float colWidth = width / maxCols;
        float tableX = x + node.Style.MarginLeft;
        float tableY = y + node.Style.MarginTop;
        float currentY = tableY;

        float[] colWidths = new float[maxCols];
        for (int c = 0; c < maxCols; c++)
            colWidths[c] = colWidth;

        foreach (var row in rows)
        {
            float rowHeight = 0;

            for (int c = 0; c < row.Children.Count && c < maxCols; c++)
            {
                var cell = row.Children[c];
                LayoutBlock(cell, tableX + c * colWidth, currentY, colWidth);

                if (cell.Bounds.Height > rowHeight)
                    rowHeight = cell.Bounds.Height;
            }

            foreach (var cell in row.Children)
                cell.Bounds = new RectangleF(
                    cell.Bounds.X, currentY,
                    cell.Bounds.Width, rowHeight);

            currentY += rowHeight;
        }

        float totalH = currentY - tableY
                        + node.Style.MarginTop + node.Style.MarginBottom;

        node.Bounds = new RectangleF(
            x + node.Style.MarginLeft,
            y + node.Style.MarginTop,
            width,
            totalH);

        Log.WriteLine(
            $"[Layout:table] <table> bounds=({node.Bounds.X:F0},{node.Bounds.Y:F0} {node.Bounds.Width:F0}x{node.Bounds.Height:F0}) rows={rowCount} cols={maxCols}");
    }

    private static void ShiftBounds(LayoutNode node, float offsetX)
    {
        node.Bounds = new RectangleF(
            node.Bounds.X + offsetX,
            node.Bounds.Y,
            node.Bounds.Width,
            node.Bounds.Height);

        foreach (var child in node.Children)
            ShiftBounds(child, offsetX);
    }

    private static void ApplyTextAlignLine(
        List<LayoutNode> items,
        float contentX, float contentWidth,
        LayoutNode? parent = null)
    {
        if (parent == null || parent.ResolvedTextAlign == TextAlignType.Left)
            return;

        float totalWidth = 0;
        foreach (var item in items)
            totalWidth += item.Bounds.Width
                          + item.Style.MarginLeft
                          + item.Style.MarginRight;

        float excess = contentWidth - totalWidth;
        if (excess <= 0)
            return;

        float offset = parent.ResolvedTextAlign == TextAlignType.Center
            ? excess / 2
            : excess;

        foreach (var item in items)
            ShiftBounds(item, offset);
    }

    private static void ApplyTextAlignChild(
        LayoutNode parent, LayoutNode child,
        float contentX, float contentWidth)
    {
        if (parent.ResolvedTextAlign == TextAlignType.Left)
            return;

        if (child.Style.Display != DisplayType.Inline &&
            child.Style.Display != DisplayType.None)
            return;

        float excess = contentWidth - child.Bounds.Width;
        if (excess <= 0)
            return;

        float offset = parent.ResolvedTextAlign == TextAlignType.Center
            ? excess / 2
            : excess;

        ShiftBounds(child, offset);
    }

    private static float GetLineHeight(ComputedStyle style)
    {
        if (style.LineHeight > 0)
            return style.LineHeight;
        return style.FontSize + 4;
    }
}
