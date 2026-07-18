using CSBrowser.Layout;

namespace CSBrowser.Render;

public sealed class DisplayListBuilder
{
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

        if (!string.IsNullOrEmpty(node.BrowserElement?.Text))
        {
            var item = new DisplayItem
            {
                Text = node.BrowserElement.Text,
                Bounds = node.Bounds,
                FontSize = node.Style.FontSize,
                Color = node.Style.Color,
                BackgroundColor = node.Style.BackgroundColor,
                Element = node.BrowserElement
            };

            items.Add(item);

            Log.WriteLine(
                $"  [Display] \"{item.Text}\" at ({item.Bounds.X:F0},{item.Bounds.Y:F0}) " +
                $"size=({item.Bounds.Width:F0}x{item.Bounds.Height:F0}) " +
                $"font={item.FontSize} color={item.Color}");
        }

        foreach (var child in node.Children)
            Walk(child, items);
    }
}
