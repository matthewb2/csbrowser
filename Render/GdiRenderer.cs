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
        if (!string.IsNullOrEmpty(item.Text))
        {
            Log.WriteLine($"[RenderItem] text='{item.Text}' TextAlign={item.TextAlign} Bounds=({item.Bounds.X:F0},{item.Bounds.Y:F0} {item.Bounds.Width:F0}x{item.Bounds.Height:F0})");
        }
        else
        {
            Log.WriteLine($"[RenderItem] <{(item.Element?.TagName ?? "?")}> (no text) Bounds=({item.Bounds.X:F0},{item.Bounds.Y:F0} {item.Bounds.Width:F0}x{item.Bounds.Height:F0})");
        }

        DrawBorders(g, item);

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

            var fontFamily = item.FontFamily;
            if (!IsFontInstalled(fontFamily))
                fontFamily = "Arial";

            using var font = new Font(fontFamily, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);

            using var format = new StringFormat();
            format.Alignment = item.TextAlign switch
            {
                TextAlignType.Center => StringAlignment.Center,
                TextAlignType.Right => StringAlignment.Far,
                _ => StringAlignment.Near
            };
            format.LineAlignment = StringAlignment.Near;

            g.DrawString(item.Text, font, brush, item.Bounds, format);

            DrawTextDecoration(g, item, font);
        }
    }

    private static bool IsFontInstalled(string fontName)
    {
        try
        {
            using var font = new Font(fontName, 8);
            return font.Name.Equals(fontName,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private void DrawBorders(Graphics g, DisplayItem item)
    {
        var b = item.Bounds;

        if (item.BorderTopWidth > 0 && item.BorderTopStyle != Layout.BorderStyle.None)
        {
            using var pen = new Pen(item.BorderTopColor, item.BorderTopWidth);
            g.DrawLine(pen, b.Left, b.Top, b.Right, b.Top);
        }

        if (item.BorderBottomWidth > 0 && item.BorderBottomStyle != Layout.BorderStyle.None)
        {
            using var pen = new Pen(item.BorderBottomColor, item.BorderBottomWidth);
            g.DrawLine(pen, b.Left, b.Bottom, b.Right, b.Bottom);
        }

        if (item.BorderLeftWidth > 0 && item.BorderLeftStyle != Layout.BorderStyle.None)
        {
            using var pen = new Pen(item.BorderLeftColor, item.BorderLeftWidth);
            g.DrawLine(pen, b.Left, b.Top, b.Left, b.Bottom);
        }

        if (item.BorderRightWidth > 0 && item.BorderRightStyle != Layout.BorderStyle.None)
        {
            using var pen = new Pen(item.BorderRightColor, item.BorderRightWidth);
            g.DrawLine(pen, b.Right, b.Top, b.Right, b.Bottom);
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
            float y = item.Bounds.Y + font.Size + 1.5f;
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
