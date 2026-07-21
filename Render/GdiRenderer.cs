using System.Drawing.Drawing2D;
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
            if (item.BorderRadius > 0)
            {
                using var path = CreateRoundedRectPath(item.Bounds, item.BorderRadius);
                g.FillPath(bgBrush, path);
            }
            else
            {
                g.FillRectangle(bgBrush, item.Bounds);
            }
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

            var fontStyle = item.IsBold ? FontStyle.Bold : FontStyle.Regular;
            using var font = new Font(fontFamily, fontSize, fontStyle, GraphicsUnit.Pixel);

            using var format = new StringFormat(StringFormatFlags.LineLimit);
            format.Alignment = item.TextAlign switch
            {
                TextAlignType.Center => StringAlignment.Center,
                TextAlignType.Right => StringAlignment.Far,
                _ => StringAlignment.Near
            };
            format.LineAlignment = StringAlignment.Near;
            format.Trimming = StringTrimming.Word;

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
        float br = item.BorderRadius;

        bool hasTop = item.BorderTopWidth > 0 && item.BorderTopStyle != Layout.BorderStyle.None;
        bool hasBottom = item.BorderBottomWidth > 0 && item.BorderBottomStyle != Layout.BorderStyle.None;
        bool hasLeft = item.BorderLeftWidth > 0 && item.BorderLeftStyle != Layout.BorderStyle.None;
        bool hasRight = item.BorderRightWidth > 0 && item.BorderRightStyle != Layout.BorderStyle.None;

        if (br > 0 && hasTop && hasBottom && hasLeft && hasRight
            && item.BorderTopWidth == item.BorderBottomWidth
            && item.BorderTopWidth == item.BorderLeftWidth
            && item.BorderTopWidth == item.BorderRightWidth
            && item.BorderTopColor == item.BorderBottomColor
            && item.BorderTopColor == item.BorderLeftColor
            && item.BorderTopColor == item.BorderRightColor)
        {
            using var pen = new Pen(item.BorderTopColor, item.BorderTopWidth);
            using var path = CreateRoundedRectPath(b, br);
            g.DrawPath(pen, path);
            return;
        }

        float r = Math.Min(br, Math.Min(b.Width, b.Height) / 2f);

        if (hasTop)
        {
            using var pen = new Pen(item.BorderTopColor, item.BorderTopWidth);
            g.DrawLine(pen, b.Left + r, b.Top, b.Right - r, b.Top);
        }

        if (hasBottom)
        {
            using var pen = new Pen(item.BorderBottomColor, item.BorderBottomWidth);
            g.DrawLine(pen, b.Left + r, b.Bottom, b.Right - r, b.Bottom);
        }

        if (hasLeft)
        {
            using var pen = new Pen(item.BorderLeftColor, item.BorderLeftWidth);
            g.DrawLine(pen, b.Left, b.Top + r, b.Left, b.Bottom - r);
        }

        if (hasRight)
        {
            using var pen = new Pen(item.BorderRightColor, item.BorderRightWidth);
            g.DrawLine(pen, b.Right, b.Top + r, b.Right, b.Bottom - r);
        }

        if (r > 0)
        {
            DrawCornerArc(g, item.BorderTopColor, item.BorderTopWidth, b.Left + r, b.Top + r, r, 180, 90);
            DrawCornerArc(g, item.BorderTopColor, item.BorderTopWidth, b.Right - r, b.Top + r, r, 270, 90);
            DrawCornerArc(g, item.BorderTopColor, item.BorderTopWidth, b.Left + r, b.Bottom - r, r, 90, 90);
            DrawCornerArc(g, item.BorderTopColor, item.BorderTopWidth, b.Right - r, b.Bottom - r, r, 0, 90);
        }
    }

    private static void DrawCornerArc(Graphics g, Color color, float width, float cx, float cy, float r, float startAngle, float sweepAngle)
    {
        if (width <= 0) return;
        using var pen = new Pen(color, width);
        g.DrawArc(pen, cx - r, cy - r, r * 2, r * 2, startAngle, sweepAngle);
    }

    private static GraphicsPath CreateRoundedRectPath(RectangleF rect, float radius)
    {
        float d = radius * 2;
        var path = new GraphicsPath();

        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();

        return path;
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
