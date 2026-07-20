using CSBrowser.Layout;

namespace CSBrowser.Render;

public sealed class DisplayListBuilder
{
    private static readonly Dictionary<string, Image> _imageCache = new();

    public List<DisplayItem> Build(LayoutNode root)
    {
        var items = new List<DisplayItem>();
        Walk(root, items, null);
        return items;
    }

    private void Walk(LayoutNode node, List<DisplayItem> items, ComputedStyle? inherited)
    {
        if (node.Style.Display == DisplayType.None)
            return;

        var be = node.BrowserElement;
        var ownStyle = be?.EffectiveStyle ?? node.Style;
        var elementStyle = be?.Style;

        // Resolve inherited properties
        var resolved = BuildResolvedStyle(ownStyle, elementStyle, inherited);

        // Log all CSS properties for this node
        if (be != null)
        {
            Log.WriteLine($"[Display] <{be.TagName}>"
                + $" font-family='{resolved.FontFamily}'"
                + $" font-size={resolved.FontSize}"
                + $" color=#{resolved.Color.R:X2}{resolved.Color.G:X2}{resolved.Color.B:X2}"
                + $" line-height={resolved.LineHeight}"
                + $" text-align={resolved.TextAlign}"
                + $" display={ownStyle.Display}"
                + $" bg=#{(ownStyle.BackgroundColor.HasValue ? $"{ownStyle.BackgroundColor.Value.R:X2}{ownStyle.BackgroundColor.Value.G:X2}{ownStyle.BackgroundColor.Value.B:X2}" : "none")}"
                + $" text='{(be.Text ?? "")}'");
        }

        if (be != null && !string.IsNullOrEmpty(be.ImagePath))
        {
            var item = new DisplayItem
            {
                IsImage = true,
                Image = LoadImage(be.ImagePath),
                Bounds = node.Bounds,
                BackgroundColor = resolved.BackgroundColor,
                Element = be,
                BorderTopWidth = resolved.BorderTop.Width,
                BorderTopStyle = resolved.BorderTop.Style,
                BorderTopColor = resolved.BorderTop.Color,
                BorderBottomWidth = resolved.BorderBottom.Width,
                BorderBottomStyle = resolved.BorderBottom.Style,
                BorderBottomColor = resolved.BorderBottom.Color,
                BorderLeftWidth = resolved.BorderLeft.Width,
                BorderLeftStyle = resolved.BorderLeft.Style,
                BorderLeftColor = resolved.BorderLeft.Color,
                BorderRightWidth = resolved.BorderRight.Width,
                BorderRightStyle = resolved.BorderRight.Style,
                BorderRightColor = resolved.BorderRight.Color,
            };

            items.Add(item);
        }
        else if (be != null && !string.IsNullOrEmpty(be.Text))
        {
            var item = new DisplayItem
            {
                Text = be.Text,
                Bounds = node.Bounds,
                FontSize = resolved.FontSize,
                FontFamily = resolved.FontFamily,
                Color = resolved.Color,
                BackgroundColor = resolved.BackgroundColor,
                TextDecoration = resolved.TextDecoration,
                TextAlign = resolved.TextAlign,
                Element = be,
                BorderTopWidth = resolved.BorderTop.Width,
                BorderTopStyle = resolved.BorderTop.Style,
                BorderTopColor = resolved.BorderTop.Color,
                BorderBottomWidth = resolved.BorderBottom.Width,
                BorderBottomStyle = resolved.BorderBottom.Style,
                BorderBottomColor = resolved.BorderBottom.Color,
                BorderLeftWidth = resolved.BorderLeft.Width,
                BorderLeftStyle = resolved.BorderLeft.Style,
                BorderLeftColor = resolved.BorderLeft.Color,
                BorderRightWidth = resolved.BorderRight.Width,
                BorderRightStyle = resolved.BorderRight.Style,
                BorderRightColor = resolved.BorderRight.Color,
            };

            items.Add(item);
        }

        foreach (var child in node.Children)
            Walk(child, items, resolved);
    }

    private static bool IsSet(ComputedStyle style, string prop)
    {
        return style.SetProperties.Contains(prop);
    }

    private static ComputedStyle BuildResolvedStyle(
        ComputedStyle own, ComputedStyle? elementStyle, ComputedStyle? inherited)
    {
        var c = new ComputedStyle();

        if (inherited != null)
        {
            // Inherited properties: start from parent, override if element explicitly set it
            // or if the element's value differs from the class-field default (e.g. heading font-size from ApplyDefaultStyles)
            if (own.FontFamily != "Arial" || (elementStyle != null && IsSet(elementStyle, "font-family")))
                c.FontFamily = own.FontFamily;
            else
                c.FontFamily = inherited.FontFamily;

            if (own.FontSize != 16 || (elementStyle != null && IsSet(elementStyle, "font-size")))
                c.FontSize = own.FontSize;
            else
                c.FontSize = inherited.FontSize;

            if (own.Color.ToArgb() != Color.Black.ToArgb() || (elementStyle != null && IsSet(elementStyle, "color")))
                c.Color = own.Color;
            else
                c.Color = inherited.Color;

            if (own.LineHeight != 0 || (elementStyle != null && IsSet(elementStyle, "line-height")))
            {
                c.LineHeight = own.LineHeight;
                c.LineHeightIsMultiplier = own.LineHeightIsMultiplier;
            }
            else
            {
                c.LineHeight = inherited.LineHeight;
                c.LineHeightIsMultiplier = inherited.LineHeightIsMultiplier;
            }

            if (own.TextAlign != TextAlignType.Left || (elementStyle != null && IsSet(elementStyle, "text-align")))
                c.TextAlign = own.TextAlign;
            else
                c.TextAlign = inherited.TextAlign;
        }
        else
        {
            // Root: use own values directly
            c.FontFamily = own.FontFamily;
            c.FontSize = own.FontSize;
            c.Color = own.Color;
            c.LineHeight = own.LineHeight;
            c.LineHeightIsMultiplier = own.LineHeightIsMultiplier;
            c.TextAlign = own.TextAlign;
        }

        // Non-inherited properties: always use the element's own value
        c.BackgroundColor = own.BackgroundColor;
        c.TextDecoration = own.TextDecoration;
        c.Display = own.Display;
        c.BoxSizing = own.BoxSizing;
        c.FlexDirection = own.FlexDirection;
        c.Width = own.Width;
        c.Height = own.Height;
        c.MarginTop = own.MarginTop;
        c.MarginBottom = own.MarginBottom;
        c.MarginLeft = own.MarginLeft;
        c.MarginRight = own.MarginRight;
        c.PaddingTop = own.PaddingTop;
        c.PaddingBottom = own.PaddingBottom;
        c.PaddingLeft = own.PaddingLeft;
        c.PaddingRight = own.PaddingRight;
        c.BorderTop = own.BorderTop;
        c.BorderBottom = own.BorderBottom;
        c.BorderLeft = own.BorderLeft;
        c.BorderRight = own.BorderRight;

        return c;
    }

    private static Image? LoadImage(string path)
    {
        if (_imageCache.TryGetValue(path, out var cached))
            return cached;

        if (!File.Exists(path))
        {
            //Log.WriteLine($"  [Display]  image not found: {path}");
            return null;
        }

        try
        {
            var img = Image.FromFile(path);
            _imageCache[path] = img;
            //Log.WriteLine($"  [Display]  image loaded: {path} ({img.Width}x{img.Height})");
            return img;
        }
        catch (Exception ex)
        {
            Log.WriteLine($"  [Display]  failed to load image: {path} - {ex.Message}");
            return null;
        }
    }
}
