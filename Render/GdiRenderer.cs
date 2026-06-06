namespace CSBrowser.Render;

public sealed class GdiRenderer
{
    public void Render(
        Graphics g,
        List<DisplayItem> items)
    {
        Log.WriteLine($"[GdiRenderer] Rendering {items.Count} items...");

        foreach (var item in items)
        {
            if (item.BackgroundColor.HasValue)
            {
                using var bgBrush =
                    new SolidBrush(
                        item.BackgroundColor.Value);

                g.FillRectangle(
                    bgBrush,
                    item.Bounds);
            }

            using var brush =
                new SolidBrush(item.Color);

            using var font =
                new Font(
                    "Arial",
                    item.FontSize,
                    GraphicsUnit.Pixel);

            g.DrawString(
                item.Text,
                font,
                brush,
                item.Bounds);
        }
    }
}
