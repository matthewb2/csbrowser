using CSBrowser.Layout;

namespace CSBrowser.Render;

public sealed class GdiRenderer
{
    public void Render(Graphics g, List<DisplayItem> items)
    {
        Log.WriteLine($"[GdiRenderer] Rendering {items.Count} items...");

        foreach (var item in items)
            RenderItem(g, item);
    }

    public void RenderItem(Graphics g, DisplayItem item)
    {
        Log.WriteLine($"[RenderItem] Rendering item...");
        if (item.BackgroundColor.HasValue)
        {
            using var bgBrush = new SolidBrush(item.BackgroundColor.Value);
            g.FillRectangle(bgBrush, item.Bounds);
        }

        if (item.IsImage && item.Image != null)
        {
            g.DrawImage(item.Image, item.Bounds);
        }
        else if (!string.IsNullOrEmpty(item.Text))
        {
            using var brush = new SolidBrush(item.Color);
            float fontSize = Math.Max(item.FontSize, 1);

            using var font = new Font("Arial", fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
            g.DrawString(item.Text, font, brush, item.Bounds);

            DrawTextDecoration(g, item, font);
        }
    }

    private static void DrawTextDecoration(Graphics g, DisplayItem item, Font font)
    {
        if (item.TextDecoration == TextDecorationType.None)
            return;

        float textWidth = g.MeasureString(item.Text, font).Width;
        float drawWidth = Math.Min(textWidth, item.Bounds.Width);

        using var pen = new Pen(item.Color, 1);

        if (item.TextDecoration == TextDecorationType.Underline)
        {
            float y = item.Bounds.Y + font.GetHeight(g) + 2;
            g.DrawLine(pen, item.Bounds.X, y, item.Bounds.X + drawWidth, y);
        }
        else if (item.TextDecoration == TextDecorationType.Overline)
        {
            float y = item.Bounds.Y + 2;
            g.DrawLine(pen, item.Bounds.X, y, item.Bounds.X + drawWidth, y);
        }
        else if (item.TextDecoration == TextDecorationType.LineThrough)
        {
            float y = item.Bounds.Y + font.GetHeight(g) / 2;
            g.DrawLine(pen, item.Bounds.X, y, item.Bounds.X + drawWidth, y);
        }
    }
}
