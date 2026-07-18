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
        var layoutRoot = Build(root);
        LayoutBlock(layoutRoot, 0, 0, width);
        return layoutRoot;
    }

    private LayoutNode Build(BrowserElement element)
    {
        var node = new LayoutNode();
        node.BrowserElement = element;
        element.Ref();

        node.Element = element.Source;
        node.Style = element.Style;

        ApplyDefaultStyles(node);

        foreach (var child in element.Children)
        {
            var childNode = Build(child);
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

        float currentY = y + node.Style.MarginTop;

        foreach (var child in node.Children)
        {
            LayoutBlock(child, x + node.Style.MarginLeft, currentY, width);
            currentY += child.Bounds.Height
                        + child.Style.MarginTop
                        + child.Style.MarginBottom;
        }

        float contentHeight = node.Style.FontSize + 10;
        if (node.Children.Count > 0)
        {
            var lastChild = node.Children[^1];
            contentHeight = lastChild.Bounds.Y + lastChild.Bounds.Height - y;
        }

        node.Bounds = new RectangleF(
            x + node.Style.MarginLeft,
            y + node.Style.MarginTop,
            width,
            contentHeight);

        Log.WriteLine(
            $"[Layout] <{tagName}> bounds=({node.Bounds.X:F0},{node.Bounds.Y:F0} {node.Bounds.Width:F0}x{node.Bounds.Height:F0})");
    }

    private void LayoutInline(LayoutNode node, float x, float y, float width)
    {
        var tagName = node.Element?.TagName ?? "?";

        float contentWidth = EstimateInlineWidth(node);
        float contentHeight = node.Style.FontSize + 4;

        node.Bounds = new RectangleF(x, y, contentWidth, contentHeight);

        Log.WriteLine(
            $"[Layout:inline] <{tagName}> bounds=({node.Bounds.X:F0},{node.Bounds.Y:F0} {node.Bounds.Width:F0}x{node.Bounds.Height:F0})");
    }

    private float EstimateInlineWidth(LayoutNode node)
    {
        var text = node.BrowserElement?.Text;
        if (!string.IsNullOrEmpty(text))
            return text.Length * node.Style.FontSize * 0.6f;

        return node.Style.FontSize * 2;
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
}
