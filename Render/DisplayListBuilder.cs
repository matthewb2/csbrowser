using CSBrowser.Layout;

namespace CSBrowser.Render;

public sealed class DisplayListBuilder
{
    private static readonly Dictionary<string, Image> _imageCache = new();

    public List<DisplayItem> Build(LayoutNode root)
    {
        Log.WriteLine("[DisplayListBuilder] Building display list...");

        var items = new List<DisplayItem>();
        Walk(root, items);
        return items;
    }

    private void Walk(LayoutNode node, List<DisplayItem> items)
    {
        if (node.Style.Display == DisplayType.None)
            return;

        var be = node.BrowserElement;
        var effectiveStyle = be?.GetEffectiveStyle() ?? node.Style;

        if (be != null && !string.IsNullOrEmpty(be.ImagePath))
        {
            var item = new DisplayItem
            {
                IsImage = true,
                Image = LoadImage(be.ImagePath),
                Bounds = node.Bounds,
                BackgroundColor = effectiveStyle.BackgroundColor,
                Element = be
            };

            items.Add(item);

            Log.WriteLine(
                $"  [Display] <img> at ({item.Bounds.X:F0},{item.Bounds.Y:F0}) " +
                $"size=({item.Bounds.Width:F0}x{item.Bounds.Height:F0})");
        }
        else if (be != null && !string.IsNullOrEmpty(be.Text))
        {
            var item = new DisplayItem
            {
                Text = be.Text,
                Bounds = node.Bounds,
                FontSize = effectiveStyle.FontSize,
                Color = effectiveStyle.Color,
                BackgroundColor = effectiveStyle.BackgroundColor,
                TextDecoration = effectiveStyle.TextDecoration,
                Element = be
            };

            items.Add(item);

            Log.WriteLine(
                $"  [Display] \"{item.Text}\" at ({item.Bounds.X:F0},{item.Bounds.Y:F0}) " +
                $"size=({item.Bounds.Width:F0}x{item.Bounds.Height:F0}) " +
                $"font={item.FontSize} color={item.Color} decorate={item.TextDecoration}");
        }

        foreach (var child in node.Children)
            Walk(child, items);
    }

    private static Image? LoadImage(string path)
    {
        if (_imageCache.TryGetValue(path, out var cached))
            return cached;

        if (!File.Exists(path))
        {
            Log.WriteLine($"  [Display]  image not found: {path}");
            return null;
        }

        try
        {
            var img = Image.FromFile(path);
            _imageCache[path] = img;
            Log.WriteLine($"  [Display]  image loaded: {path} ({img.Width}x{img.Height})");
            return img;
        }
        catch (Exception ex)
        {
            Log.WriteLine($"  [Display]  failed to load image: {path} - {ex.Message}");
            return null;
        }
    }
}
