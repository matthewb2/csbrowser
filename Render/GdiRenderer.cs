using CSBrowser.Layout;

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

            if (item.IsImage && item.Image != null)
            {
                g.DrawImage(
                    item.Image,
                    item.Bounds);
            }
            else if (!string.IsNullOrEmpty(item.Text))
            {
                using var brush =
                    new SolidBrush(item.Color);

                float fontSize = Math.Max(item.FontSize, 1);

                using var font =
                    new Font(
                        "Arial",
                        fontSize,
                        FontStyle.Regular,
                        GraphicsUnit.Pixel);

                g.DrawString(
                    item.Text,
                    font,
                    brush,
                    item.Bounds);

                if (item.TextDecoration == TextDecorationType.Underline)
                {
                    float textHeight = font.GetHeight(g);
                    float underlineY = item.Bounds.Y + textHeight + 2;
                    float textWidth = g.MeasureString(item.Text, font).Width;
                    float underlineWidth = Math.Min(textWidth, item.Bounds.Width);

                    using var pen = new Pen(item.Color, 1);
                    g.DrawLine(
                        pen,
                        item.Bounds.X,
                        underlineY,
                        item.Bounds.X + underlineWidth,
                        underlineY);
                }
                else if (item.TextDecoration == TextDecorationType.Overline)
                {
                    float overlineY = item.Bounds.Y + 2;
                    float textWidth = g.MeasureString(item.Text, font).Width;
                    float overlineWidth = Math.Min(textWidth, item.Bounds.Width);

                    using var pen = new Pen(item.Color, 1);
                    g.DrawLine(
                        pen,
                        item.Bounds.X,
                        overlineY,
                        item.Bounds.X + overlineWidth,
                        overlineY);
                }
                else if (item.TextDecoration == TextDecorationType.LineThrough)
                {
                    float textHeight = font.GetHeight(g);
                    float strikeY = item.Bounds.Y + textHeight / 2;
                    float textWidth = g.MeasureString(item.Text, font).Width;
                    float strikeWidth = Math.Min(textWidth, item.Bounds.Width);

                    using var pen = new Pen(item.Color, 1);
                    g.DrawLine(
                        pen,
                        item.Bounds.X,
                        strikeY,
                        item.Bounds.X + strikeWidth,
                        strikeY);
                }
            }
        }
    }
}
