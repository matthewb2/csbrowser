using CSBrowser.Layout;

namespace CSBrowser.Render;

public sealed class DisplayListBuilder
{
    private static readonly Dictionary<string, Image> _imageCache = new();

    public List<DisplayItem> Build(LayoutNode root)
    {
        var items = new List<DisplayItem>();
        Walk(root, items, TextAlignType.Left);
        return items;
    }

    private void Walk(LayoutNode node, List<DisplayItem> items, TextAlignType inheritedTextAlign)
    {
        if (node.Style.Display == DisplayType.None)
            return;

        var be = node.BrowserElement;
        var effectiveStyle = be?.EffectiveStyle ?? node.Style;

        var usedTextAlign = effectiveStyle.TextAlign;
        if (usedTextAlign == TextAlignType.Left)
            usedTextAlign = inheritedTextAlign;

        if (be != null && !string.IsNullOrEmpty(be.Text))
        {
            Log.WriteLine($"  [Display] <{be.TagName}> text='{be.Text}' styleTextAlign={effectiveStyle.TextAlign} inheritedTextAlign={inheritedTextAlign} usedTextAlign={usedTextAlign} font-size={effectiveStyle.FontSize}");
        }

        var childInherited = usedTextAlign;

        if (be != null && !string.IsNullOrEmpty(be.ImagePath))
        {
            var item = new DisplayItem
            {
                IsImage = true,
                Image = LoadImage(be.ImagePath),
                Bounds = node.Bounds,
                BackgroundColor = effectiveStyle.BackgroundColor,
                Element = be,
                BorderTopWidth = effectiveStyle.BorderTop.Width,
                BorderTopStyle = effectiveStyle.BorderTop.Style,
                BorderTopColor = effectiveStyle.BorderTop.Color,
                BorderBottomWidth = effectiveStyle.BorderBottom.Width,
                BorderBottomStyle = effectiveStyle.BorderBottom.Style,
                BorderBottomColor = effectiveStyle.BorderBottom.Color,
                BorderLeftWidth = effectiveStyle.BorderLeft.Width,
                BorderLeftStyle = effectiveStyle.BorderLeft.Style,
                BorderLeftColor = effectiveStyle.BorderLeft.Color,
                BorderRightWidth = effectiveStyle.BorderRight.Width,
                BorderRightStyle = effectiveStyle.BorderRight.Style,
                BorderRightColor = effectiveStyle.BorderRight.Color,
            };

            items.Add(item);
        }
        else if (be != null && !string.IsNullOrEmpty(be.Text))
        {
            var item = new DisplayItem
            {
                Text = be.Text,
                Bounds = node.Bounds,
                FontSize = effectiveStyle.FontSize,
                FontFamily = effectiveStyle.FontFamily,
                Color = effectiveStyle.Color,
                BackgroundColor = effectiveStyle.BackgroundColor,
                TextDecoration = effectiveStyle.TextDecoration,
                TextAlign = usedTextAlign,
                Element = be,
                BorderTopWidth = effectiveStyle.BorderTop.Width,
                BorderTopStyle = effectiveStyle.BorderTop.Style,
                BorderTopColor = effectiveStyle.BorderTop.Color,
                BorderBottomWidth = effectiveStyle.BorderBottom.Width,
                BorderBottomStyle = effectiveStyle.BorderBottom.Style,
                BorderBottomColor = effectiveStyle.BorderBottom.Color,
                BorderLeftWidth = effectiveStyle.BorderLeft.Width,
                BorderLeftStyle = effectiveStyle.BorderLeft.Style,
                BorderLeftColor = effectiveStyle.BorderLeft.Color,
                BorderRightWidth = effectiveStyle.BorderRight.Width,
                BorderRightStyle = effectiveStyle.BorderRight.Style,
                BorderRightColor = effectiveStyle.BorderRight.Color,
            };

            items.Add(item);
        }

        foreach (var child in node.Children)
            Walk(child, items, childInherited);
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
