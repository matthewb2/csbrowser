using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using CSBrowser.Dom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

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

    private static ComputedStyle ComputeStyleWithInheritance(
        ComputedStyle elementStyle, ComputedStyle? parentStyle)
    {
        var resolved = new ComputedStyle();

        // Non-inherited properties: always use the element's own value
        resolved.Display = elementStyle.Display;
        resolved.BoxSizing = elementStyle.BoxSizing;
        resolved.Width = elementStyle.Width;
        resolved.Height = elementStyle.Height;
        resolved.MarginTop = elementStyle.MarginTop;
        resolved.MarginBottom = elementStyle.MarginBottom;
        resolved.MarginLeft = elementStyle.MarginLeft;
        resolved.MarginRight = elementStyle.MarginRight;
        resolved.PaddingTop = elementStyle.PaddingTop;
        resolved.PaddingBottom = elementStyle.PaddingBottom;
        resolved.PaddingLeft = elementStyle.PaddingLeft;
        resolved.PaddingRight = elementStyle.PaddingRight;
        resolved.BorderTop = elementStyle.BorderTop;
        resolved.BorderBottom = elementStyle.BorderBottom;
        resolved.BorderLeft = elementStyle.BorderLeft;
        resolved.BorderRight = elementStyle.BorderRight;
        resolved.BackgroundColor = elementStyle.BackgroundColor;
        resolved.TextDecoration = elementStyle.TextDecoration;
        resolved.FlexDirection = elementStyle.FlexDirection;
        resolved.FlexWrap = elementStyle.FlexWrap;
        resolved.Gap = elementStyle.Gap;
        resolved.FlexGrow = elementStyle.FlexGrow;
        resolved.FlexShrink = elementStyle.FlexShrink;
        resolved.FlexBasis = elementStyle.FlexBasis;
        resolved.BorderRadius = elementStyle.BorderRadius;

        // Inherited properties: use element's value if explicitly set, otherwise inherit from parent
        resolved.Color = elementStyle.SetProperties.Contains("color")
            ? elementStyle.Color
            : parentStyle?.Color ?? Color.Black;

        resolved.FontFamily = elementStyle.SetProperties.Contains("font-family")
            ? elementStyle.FontFamily
            : parentStyle?.FontFamily ?? "Arial";

        resolved.FontSize = elementStyle.SetProperties.Contains("font-size")
            ? elementStyle.FontSize
            : parentStyle?.FontSize ?? 16f;

        resolved.IsBold = elementStyle.SetProperties.Contains("font-weight")
            ? elementStyle.IsBold
            : parentStyle?.IsBold ?? false;

        if (elementStyle.SetProperties.Contains("line-height"))
        {
            resolved.LineHeight = elementStyle.LineHeight;
            resolved.LineHeightIsMultiplier = elementStyle.LineHeightIsMultiplier;
        }
        else
        {
            resolved.LineHeight = parentStyle?.LineHeight ?? 0;
            resolved.LineHeightIsMultiplier = parentStyle?.LineHeightIsMultiplier ?? false;
        }

        resolved.TextAlign = elementStyle.SetProperties.Contains("text-align")
            ? elementStyle.TextAlign
            : parentStyle?.TextAlign ?? TextAlignType.Left;

        resolved.SetProperties = new HashSet<string>(elementStyle.SetProperties);

        return resolved;
    }

    public LayoutNode Build(BrowserElement element, ComputedStyle? parentStyle = null)
    {
        var node = new LayoutNode();
        node.BrowserElement = element;
        element.Ref();

        node.Element = element.Source;

        // Create a fresh clone with inheritance — never mutate the original element.Style
        var inheritedStyle = ComputeStyleWithInheritance(element.Style, parentStyle);
        ApplyDefaultStyles(inheritedStyle, node.Element);
        node.Style = inheritedStyle;

        foreach (var child in element.Children)
        {
            var childNode = Build(child, node.Style);
            node.Children.Add(childNode);
        }

        return node;
    }

    private void ApplyDefaultStyles(ComputedStyle style, IElement? element)
    {
        if (element == null)
            return;

        switch (element)
        {
            case IHtmlHeadingElement:
                var level = element.LocalName;
                if (!style.SetProperties.Contains("font-size"))
                {
                    style.FontSize = level switch
                    {
                        "h1" => 32,
                        "h2" => 24,
                        "h3" => 18.5f,
                        "h4" => 16,
                        "h5" => 13.3f,
                        "h6" => 10.7f,
                        _ => 16
                    };
                }
                if (!style.SetProperties.Contains("margin-top") && style.MarginTop == 0)
                    style.MarginTop = style.FontSize * 0.67f;
                if (!style.SetProperties.Contains("margin-bottom") && style.MarginBottom == 0)
                    style.MarginBottom = style.FontSize * 0.67f;
                break;

            case IHtmlParagraphElement:
                if (style.MarginTop == 0)
                    style.MarginTop = 16;
                if (style.MarginBottom == 0)
                    style.MarginBottom = 16;
                break;

            case IHtmlListItemElement:
                if (style.MarginTop == 0)
                    style.MarginTop = 4;
                if (style.MarginBottom == 0)
                    style.MarginBottom = 4;
                break;

            case IHtmlUnorderedListElement:
            case IHtmlOrderedListElement:
                if (style.MarginTop == 0)
                    style.MarginTop = 16;
                if (style.MarginBottom == 0)
                    style.MarginBottom = 16;
                if (style.MarginLeft == 0)
                    style.MarginLeft = 40;
                break;

            case IHtmlTableElement:
                style.Display = DisplayType.Block;
                if (style.MarginTop == 0)
                    style.MarginTop = 16;
                if (style.MarginBottom == 0)
                    style.MarginBottom = 16;
                break;

            case IHtmlTableRowElement:
                style.Display = DisplayType.Flex;
                style.FlexDirection = FlexDirection.Row;
                break;

            case IHtmlTableCellElement:
                style.Display = DisplayType.Block;
                break;

            case IHtmlSpanElement:
            case IHtmlImageElement:
                style.Display = DisplayType.Inline;
                break;

            default:
                ApplyDefaultStylesByLocalName(style, element);
                break;
        }

        if (element.LocalName is "body")
        {
            if (style.MarginTop == 0)
                style.MarginTop = 8;
            if (style.MarginLeft == 0)
                style.MarginLeft = 8;
        }
    }

    private void ApplyDefaultStylesByLocalName(ComputedStyle style, IElement element)
    {
        var local = element.LocalName;

        switch (local)
        {
            case "br":
                style.Display = DisplayType.Inline;
                break;

            case "hr":
                if (style.MarginTop == 0)
                    style.MarginTop = 8;
                if (style.MarginBottom == 0)
                    style.MarginBottom = 8;
                break;

            case "blockquote":
                if (style.MarginTop == 0)
                    style.MarginTop = 16;
                if (style.MarginBottom == 0)
                    style.MarginBottom = 16;
                if (style.MarginLeft == 0)
                    style.MarginLeft = 40;
                if (style.MarginRight == 0)
                    style.MarginRight = 40;
                break;

            case "pre":
                if (style.MarginTop == 0)
                    style.MarginTop = 16;
                if (style.MarginBottom == 0)
                    style.MarginBottom = 16;
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
                style.Display = DisplayType.Inline;
                break;
        }
    }

    public void LayoutBlock(LayoutNode node, float x, float y, float width)
    {
        var tagName = node.Element?.TagName ?? "?";

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
            if (child.Style.Display == DisplayType.Inline || child.Style.Display == DisplayType.None)
                hasInlineChildren = true;
            else
                hasBlockChildren = true;
        }

        float outerWidth = node.Style.Width.HasValue ? node.Style.Width.Value : width;

        float marginLeft = node.Style.MarginLeft;
        float marginRight = node.Style.MarginRight;
        float paddingLeft = node.Style.PaddingLeft;
        float paddingRight = node.Style.PaddingRight;
        float borderLeft = node.Style.BorderLeft.Width;
        float borderRight = node.Style.BorderRight.Width;

        float boundsWidth;
        float contentWidth;

        // 3. [����] BoxSizing ǥ�� ���� �б� ����
        if (node.Style.BoxSizing == BoxSizingType.BorderBox)
        {
            boundsWidth = outerWidth;
            contentWidth = Math.Max(0, outerWidth - paddingLeft - paddingRight - borderLeft - borderRight);
        }
        else
        {
            // ContentBox ���� �� ��
            if (node.Style.Width.HasValue)
            {
                contentWidth = outerWidth;
                boundsWidth = outerWidth + paddingLeft + paddingRight + borderLeft + borderRight;
            }
            else
            {
                boundsWidth = outerWidth;
                contentWidth = Math.Max(0, outerWidth - paddingLeft - paddingRight - borderLeft - borderRight);
            }
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

                // ���̾ƿ��� ���� 1ȸ�� �����Ͽ� Bounds�� ũ�⸦ �޾ƿɴϴ�.
                LayoutBlock(child, currentX, lineY, contentWidth);

                // 4. [����] ���̾ƿ� �ߺ� ȣ�� ���� �� ShiftBoundsX/Y ��ȯ ����
                if (currentX + child.Bounds.Width > maxX && currentX > contentX)
                {
                    ApplyTextAlignLine(inlineItems, contentX, contentWidth, parent: node);
                    inlineItems.Clear();

                    float oldX = child.Bounds.X;
                    float oldY = child.Bounds.Y;

                    currentX = contentX;
                    lineY += lineHeight + child.Style.MarginTop;
                    lineHeight = 0;

                    // ����(LayoutBlock) ��� �����¸�ŭ Bounds ��ǥ �̵� ó�� (���� ����ȭ)
                    ShiftBounds(child, currentX - oldX, lineY - oldY);
                }

                inlineItems.Add(child);

                currentX += child.Bounds.Width + child.Style.MarginLeft + child.Style.MarginRight;

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

                currentY += child.Bounds.Height + child.Style.MarginTop + child.Style.MarginBottom;
            }

            if (node.Children.Count > 0)
            {
                var lastChild = node.Children[^1];
                contentHeight = lastChild.Bounds.Y + lastChild.Bounds.Height - y;
            }
        }

        contentHeight += node.Style.PaddingTop + node.Style.PaddingBottom + node.Style.BorderTop.Width + node.Style.BorderBottom.Width;

        if (node.Style.Height.HasValue && node.Style.Height.Value > contentHeight)
            contentHeight = node.Style.Height.Value;

        node.Bounds = new RectangleF(x + node.Style.MarginLeft, y + node.Style.MarginTop, boundsWidth, contentHeight);
    }

    private void LayoutInline(LayoutNode node, float x, float y, float width)
    {
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

                childW += child.Bounds.Width + child.Style.MarginLeft + child.Style.MarginRight;

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

            if (contentWidth > width)
            {
                contentWidth = width;
                int lineCount = Math.Max(1, (int)Math.Ceiling(EstimateInlineWidth(node) / width));
                contentHeight = GetLineHeight(node.Style) * lineCount;
            }
        }

        node.Bounds = new RectangleF(x, y, contentWidth, contentHeight);
    }

    private float EstimateInlineWidth(LayoutNode node)
    {
        var text = node.BrowserElement?.Text;
        if (!string.IsNullOrEmpty(text))
        {
            using var bmp = new Bitmap(1, 1);
            using var g = Graphics.FromImage(bmp);
            var fontStyle = node.Style.IsBold ? FontStyle.Bold : FontStyle.Regular;
            using var font = new Font(node.Style.FontFamily, node.Style.FontSize, fontStyle, GraphicsUnit.Pixel);
            return g.MeasureString(text, font).Width;
        }

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

        if (maxCols == 0) maxCols = 1;

        float colWidth = width / maxCols;
        float tableX = x + node.Style.MarginLeft;
        float tableY = y + node.Style.MarginTop;
        float currentY = tableY;

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
                cell.Bounds = new RectangleF(cell.Bounds.X, currentY, cell.Bounds.Width, rowHeight);

            currentY += rowHeight;
        }

        float totalH = currentY - tableY + node.Style.MarginTop + node.Style.MarginBottom;
        node.Bounds = new RectangleF(x + node.Style.MarginLeft, y + node.Style.MarginTop, width, totalH);
    }

    // 5. [����] X��� Y�� �������� ��� �����ϵ��� ������ ShiftBounds
    private static void ShiftBounds(LayoutNode node, float offsetX, float offsetY)
    {
        node.Bounds = new RectangleF(
            node.Bounds.X + offsetX,
            node.Bounds.Y + offsetY,
            node.Bounds.Width,
            node.Bounds.Height);

        foreach (var child in node.Children)
            ShiftBounds(child, offsetX, offsetY);
    }

    private static void ApplyTextAlignLine(List<LayoutNode> items, float contentX, float contentWidth, LayoutNode? parent = null)
    {
        if (parent == null || parent.Style.TextAlign == TextAlignType.Left)
            return;

        float totalWidth = 0;
        foreach (var item in items)
            totalWidth += item.Bounds.Width + item.Style.MarginLeft + item.Style.MarginRight;

        float excess = contentWidth - totalWidth;
        if (excess <= 0)
            return;

        float offset = parent.Style.TextAlign == TextAlignType.Center ? excess / 2 : excess;

        foreach (var item in items)
            ShiftBounds(item, offset, 0); // Y�� �̵��� �����Ƿ� 0 ����
    }

    private static void ApplyTextAlignChild(LayoutNode parent, LayoutNode child, float contentX, float contentWidth)
    {
        if (parent.Style.TextAlign == TextAlignType.Left)
            return;

        if (child.Style.Display != DisplayType.Inline && child.Style.Display != DisplayType.None)
            return;

        float excess = contentWidth - child.Bounds.Width;
        if (excess <= 0)
            return;

        float offset = parent.Style.TextAlign == TextAlignType.Center ? excess / 2 : excess;

        ShiftBounds(child, offset, 0); // Y�� �̵��� �����Ƿ� 0 ����
    }

    private static float GetLineHeight(ComputedStyle style)
    {
        if (style.LineHeight > 0)
        {
            if (style.LineHeightIsMultiplier)
                return style.LineHeight * style.FontSize;
            return style.LineHeight;
        }
        return style.FontSize + 4;
    }
}