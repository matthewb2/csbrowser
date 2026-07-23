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
         //   Log.WriteLine($"[RenderItem] text='{item.Text}' TextAlign={item.TextAlign} Bounds=({item.Bounds.X:F0},{item.Bounds.Y:F0} {item.Bounds.Width:F0}x{item.Bounds.Height:F0})");
        }
        else
        {
         //   Log.WriteLine($"[RenderItem] <{(item.Element?.TagName ?? "?")}> (no text) Bounds=({item.Bounds.X:F0},{item.Bounds.Y:F0} {item.Bounds.Width:F0}x{item.Bounds.Height:F0})");
        }

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

        DrawBorders(g, item);

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

            // GdiRenderer.RenderItem ������ �ؽ�Ʈ ��� ����
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            g.DrawString(item.Text, font, brush, item.Bounds, format);

            DrawTextDecoration(g, item, font);
        }
    }

    private static readonly HashSet<string> _installedFonts;

    static GdiRenderer()
    {
        _installedFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var collection = new System.Drawing.Text.InstalledFontCollection();
        foreach (var family in collection.Families)
            _installedFonts.Add(family.Name);
    }

    private static bool IsFontInstalled(string fontName)
    {
        return _installedFonts.Contains(fontName);
    }

    private void DrawBorders(Graphics g, DisplayItem item)
    {
        var b = item.Bounds;
        float br = item.BorderRadius;

        bool hasTop = item.BorderTopWidth > 0 && item.BorderTopStyle != Layout.BorderStyle.None;
        bool hasBottom = item.BorderBottomWidth > 0 && item.BorderBottomStyle != Layout.BorderStyle.None;
        bool hasLeft = item.BorderLeftWidth > 0 && item.BorderLeftStyle != Layout.BorderStyle.None;
        bool hasRight = item.BorderRightWidth > 0 && item.BorderRightStyle != Layout.BorderStyle.None;

        if (!hasTop && !hasBottom && !hasLeft && !hasRight)
            return;

        if (br > 0 && hasTop && hasBottom && hasLeft && hasRight
            && item.BorderTopWidth == item.BorderBottomWidth
            && item.BorderTopWidth == item.BorderLeftWidth
            && item.BorderTopWidth == item.BorderRightWidth
            && item.BorderTopColor == item.BorderBottomColor
            && item.BorderTopColor == item.BorderLeftColor
            && item.BorderTopColor == item.BorderRightColor)
        {
            float borderWidth = item.BorderTopWidth;
            var adjustedRect = new RectangleF(
                b.X + borderWidth / 2f,
                b.Y + borderWidth / 2f,
                Math.Max(0, b.Width - borderWidth),
                Math.Max(0, b.Height - borderWidth)
            );
            float adjustedRadius = Math.Max(0, br - borderWidth / 2f);

            using var pen = new Pen(item.BorderTopColor, borderWidth);
            using var path = CreateRoundedRectPath(adjustedRect, adjustedRadius);
            g.DrawPath(pen, path);
            return;
        }

        if (hasTop)
        {
            using var pen = new Pen(item.BorderTopColor, item.BorderTopWidth);
            float halfW = item.BorderTopWidth / 2f;
            g.DrawLine(pen, b.Left, b.Top + halfW, b.Right, b.Top + halfW);
        }

        if (hasBottom)
        {
            using var pen = new Pen(item.BorderBottomColor, item.BorderBottomWidth);
            float halfW = item.BorderBottomWidth / 2f;
            g.DrawLine(pen, b.Left, b.Bottom - halfW, b.Right, b.Bottom - halfW);
        }

        if (hasLeft)
        {
            using var pen = new Pen(item.BorderLeftColor, item.BorderLeftWidth);
            float halfW = item.BorderLeftWidth / 2f;
            g.DrawLine(pen, b.Left + halfW, b.Top, b.Left + halfW, b.Bottom);
        }

        if (hasRight)
        {
            using var pen = new Pen(item.BorderRightColor, item.BorderRightWidth);
            float halfW = item.BorderRightWidth / 2f;
            g.DrawLine(pen, b.Right - halfW, b.Top, b.Right - halfW, b.Bottom);
        }
    }

    private static GraphicsPath CreateRoundedRectPath(RectangleF rect, float radius)
    {
        float d = Math.Max(0, radius * 2);
        var path = new GraphicsPath();

        if (d <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        if (d > rect.Width) d = rect.Width;
        if (d > rect.Height) d = rect.Height;

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

        var family = font.FontFamily;
        int emHeight = family.GetEmHeight(font.Style);
        float lineH = font.GetHeight(g);
        float ascent = family.GetCellAscent(font.Style) * lineH / emHeight;

        float baselineY = item.Bounds.Y + ascent;

        using var pen = new Pen(item.Color, 1);

        if (item.TextDecoration == TextDecorationType.Underline)
        {
            float y = baselineY + 2;
            g.DrawLine(pen, item.Bounds.X, y, item.Bounds.X + drawWidth, y);
        }
        else if (item.TextDecoration == TextDecorationType.Overline)
        {
            float y = item.Bounds.Y;
            g.DrawLine(pen, item.Bounds.X, y, item.Bounds.X + drawWidth, y);
        }
        else if (item.TextDecoration == TextDecorationType.LineThrough)
        {
            int descent = family.GetCellDescent(font.Style);
            float middle = (ascent - descent * lineH / emHeight) / 2;
            float y = item.Bounds.Y + middle;
            g.DrawLine(pen, item.Bounds.X, y, item.Bounds.X + drawWidth, y);
        }
    }
}
