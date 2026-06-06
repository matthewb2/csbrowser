using System.Drawing;

namespace CSBrowser.Render;

public sealed class DisplayItem
{
    public string Text = "";

    public RectangleF Bounds;

    public float FontSize = 16;

    public Color Color = Color.Black;

    public Color? BackgroundColor;
}