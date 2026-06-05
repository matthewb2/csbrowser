namespace CSBrowser.Render;

public sealed class GdiRenderer
{
    public void Render(
        Graphics g,
        List<DisplayItem> items)
    {
        foreach (var item in items)
        {
            using var font =
                new Font(
                    "Segoe UI",
                    item.FontSize,
                    GraphicsUnit.Pixel);

            g.DrawString(
                item.Text,
                font,
                Brushes.Black,
                item.Bounds);
        }
    }
}