using System.Drawing;
using CSBrowser.Dom;

namespace CSBrowser.Render;

public sealed class DisplayItem : RefCounted
{
    public string Text = "";
    public RectangleF Bounds;
    public float FontSize = 16;
    public Color Color = Color.Black;
    public Color? BackgroundColor;
    public BrowserElement? Element;

    protected override void Cleanup()
    {
    }
}
