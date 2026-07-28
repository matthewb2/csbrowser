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

    public LayoutNode Build(BrowserElement element, ComputedStyle? parentStyle = null, ComputedStyle? inheritedHoverOverrides = null)
    {
        var node = new LayoutNode();
        node.BrowserElement = element;
        element.Ref();

        node.Element = element.Source;

        ApplyDefaultStyles(element.NormalStyle, node.Element);

        if (!element.NormalStyle.SetProperties.Contains("width") && !element.NormalStyle.Width.HasValue)
        {
            if (element.Source is IHtmlInputElement inputEl)
            {
                var inputType = (inputEl.GetAttribute("type") ?? "").ToLowerInvariant();
                if (inputType is "submit" or "button" or "reset")
                {
                    var text = inputEl.Value;
                    if (!string.IsNullOrEmpty(text))
                    {
                        using var bmp = new Bitmap(1, 1);
                        using var g = Graphics.FromImage(bmp);
                        using var font = new Font(element.NormalStyle.FontFamily, element.NormalStyle.FontSize, FontStyle.Regular, GraphicsUnit.Pixel);
                        float textWidth = g.MeasureString(text, font).Width;
                        element.NormalStyle.Width = textWidth
                            + element.NormalStyle.PaddingLeft + element.NormalStyle.PaddingRight
                            + element.NormalStyle.BorderLeft.Width + element.NormalStyle.BorderRight.Width;
                        Log.WriteLine($"  [Layout] <input type=\"{inputType}\"> auto-width={element.NormalStyle.Width} from text='{text}'");
                    }
                }
            }
        }

        ComputedStyle? activeOverrides = element.HoverOverrides ?? inheritedHoverOverrides;
        if (activeOverrides != null)
        {
            element.HoverStyle = CopyStyle(element.NormalStyle);
            ApplyHoverOverrides(element.HoverStyle, activeOverrides);
            Log.WriteLine($"  [Layout] <{element.Source?.TagName ?? "?"}> HoverStyle rebuilt: color=#{element.HoverStyle.Color.R:X2}{element.HoverStyle.Color.G:X2}{element.HoverStyle.Color.B:X2} textDec={element.HoverStyle.TextDecoration} activeOverrides=color?{activeOverrides.SetProperties.Contains("color")} textDec?{activeOverrides.SetProperties.Contains("text-decoration")}");
        }
        else
        {
            element.HoverStyle = element.NormalStyle;
        }

        var inheritedStyle = ComputeStyleWithInheritance(element.NormalStyle, parentStyle);
        node.Style = inheritedStyle;

        foreach (var child in element.Children)
        {
            var childNode = Build(child, node.Style, activeOverrides);
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

            case IHtmlInputElement inputEl:
                var inputType = (inputEl.GetAttribute("type") ?? "").ToLowerInvariant();
                if (inputType == "submit" || inputType == "button" || inputType == "reset")
                {
                    style.Display = DisplayType.Block;
                    if (style.MarginTop == 0) style.MarginTop = 2;
                    if (style.MarginBottom == 0) style.MarginBottom = 2;
                    if (style.PaddingTop == 0) style.PaddingTop = 2;
                    if (style.PaddingBottom == 0) style.PaddingBottom = 2;
                    if (style.PaddingLeft == 0) style.PaddingLeft = 2;
                    if (style.PaddingRight == 0) style.PaddingRight = 2;
                    if (!style.SetProperties.Contains("background-color"))
                        style.BackgroundColor = Color.FromArgb(238, 238, 238);
                    if (!style.SetProperties.Contains("border"))
                    {
                        style.BorderTop = new BorderSide { Width = 1, Style = BorderStyle.Solid, Color = Color.FromArgb(120, 120, 120) };
                        style.BorderBottom = new BorderSide { Width = 1, Style = BorderStyle.Solid, Color = Color.FromArgb(120, 120, 120) };
                        style.BorderLeft = new BorderSide { Width = 1, Style = BorderStyle.Solid, Color = Color.FromArgb(120, 120, 120) };
                        style.BorderRight = new BorderSide { Width = 1, Style = BorderStyle.Solid, Color = Color.FromArgb(120, 120, 120) };
                    }
                    if (!style.SetProperties.Contains("border-radius"))
                        style.BorderRadius = 2;
                    if (!style.SetProperties.Contains("text-align"))
                        style.TextAlign = TextAlignType.Center;
                    Log.WriteLine($"  [Layout] <input type=\"{inputType}\"> default button styles applied");
                }
                else if (inputType is "text" or "password" or "email" or "number" or "search" or "tel" or "url" or "")
                {
                    style.Display = DisplayType.Block;
                    if (style.MarginTop == 0) style.MarginTop = 2;
                    if (style.MarginBottom == 0) style.MarginBottom = 2;
                    if (style.PaddingTop == 0) style.PaddingTop = 2;
                    if (style.PaddingBottom == 0) style.PaddingBottom = 2;
                    if (style.PaddingLeft == 0) style.PaddingLeft = 2;
                    if (style.PaddingRight == 0) style.PaddingRight = 2;
                    if (!style.SetProperties.Contains("background-color"))
                        style.BackgroundColor = Color.White;
                    if (!style.SetProperties.Contains("border"))
                    {
                        style.BorderTop = new BorderSide { Width = 1, Style = BorderStyle.Solid, Color = Color.FromArgb(120, 120, 120) };
                        style.BorderBottom = new BorderSide { Width = 1, Style = BorderStyle.Solid, Color = Color.FromArgb(120, 120, 120) };
                        style.BorderLeft = new BorderSide { Width = 1, Style = BorderStyle.Solid, Color = Color.FromArgb(120, 120, 120) };
                        style.BorderRight = new BorderSide { Width = 1, Style = BorderStyle.Solid, Color = Color.FromArgb(120, 120, 120) };
                    }
                    if (!style.SetProperties.Contains("border-radius"))
                        style.BorderRadius = 2;
                    if (!style.SetProperties.Contains("width"))
                        style.Width = 200;
                    Log.WriteLine($"  [Layout] <input type=\"{inputType}\"> default text input styles applied");
                }
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

                // 화면에 보이지 않는 요소는 레이아웃 계산 및 높이 합산에서 제외
                if (child.Style.Display == DisplayType.None)
                {
                    child.Bounds = RectangleF.Empty;
                    continue;
                }

                LayoutBlock(child, contentX, currentY, contentWidth);
                ApplyTextAlignChild(node, child, contentX, contentWidth);

                currentY += child.Bounds.Height + child.Style.MarginTop + child.Style.MarginBottom;
            }

            if (node.Children.Count > 0)
            {
                // Find last visible child (skip display=None like <script>)
                LayoutNode? lastVisible = null;
                for (int i = node.Children.Count - 1; i >= 0; i--)
                {
                    if (node.Children[i].Style.Display != DisplayType.None && node.Children[i].Bounds.Height > 0)
                    {
                        lastVisible = node.Children[i];
                        break;
                    }
                }
                if (lastVisible != null)
                    contentHeight = lastVisible.Bounds.Y + lastVisible.Bounds.Height - y;
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

    private static ComputedStyle CopyStyle(ComputedStyle src)
    {
        var c = new ComputedStyle();
        c.SetProperties = new HashSet<string>(src.SetProperties);
        c.FontSize = src.FontSize;
        c.FontFamily = src.FontFamily;
        c.IsBold = src.IsBold;
        c.LineHeight = src.LineHeight;
        c.LineHeightIsMultiplier = src.LineHeightIsMultiplier;
        c.MarginTop = src.MarginTop;
        c.MarginBottom = src.MarginBottom;
        c.MarginLeft = src.MarginLeft;
        c.MarginRight = src.MarginRight;
        c.PaddingTop = src.PaddingTop;
        c.PaddingBottom = src.PaddingBottom;
        c.PaddingLeft = src.PaddingLeft;
        c.PaddingRight = src.PaddingRight;
        c.BorderTop = src.BorderTop;
        c.BorderBottom = src.BorderBottom;
        c.BorderLeft = src.BorderLeft;
        c.BorderRight = src.BorderRight;
        c.Color = src.Color;
        c.BackgroundColor = src.BackgroundColor;
        c.Display = src.Display;
        c.FlexDirection = src.FlexDirection;
        c.FlexWrap = src.FlexWrap;
        c.Gap = src.Gap;
        c.FlexGrow = src.FlexGrow;
        c.FlexShrink = src.FlexShrink;
        c.FlexBasis = src.FlexBasis;
        c.TextDecoration = src.TextDecoration;
        c.BoxSizing = src.BoxSizing;
        c.Width = src.Width;
        c.Height = src.Height;
        c.TextAlign = src.TextAlign;
        c.BorderRadius = src.BorderRadius;
        return c;
    }

    private static void ApplyHoverOverrides(ComputedStyle target, ComputedStyle hover)
    {
        if (hover.SetProperties.Contains("background-color"))
            target.BackgroundColor = hover.BackgroundColor;
        if (hover.SetProperties.Contains("color"))
            target.Color = hover.Color;
        if (hover.SetProperties.Contains("font-size"))
            target.FontSize = hover.FontSize;
        if (hover.SetProperties.Contains("font-family"))
            target.FontFamily = hover.FontFamily;
        if (hover.SetProperties.Contains("font-weight"))
            target.IsBold = hover.IsBold;
        if (hover.SetProperties.Contains("text-decoration"))
            target.TextDecoration = hover.TextDecoration;
        if (hover.SetProperties.Contains("border-radius"))
            target.BorderRadius = hover.BorderRadius;
        if (hover.SetProperties.Contains("border"))
        {
            target.BorderTop = hover.BorderTop;
            target.BorderBottom = hover.BorderBottom;
            target.BorderLeft = hover.BorderLeft;
            target.BorderRight = hover.BorderRight;
        }
    }
}