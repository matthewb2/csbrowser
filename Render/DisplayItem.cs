using System.Drawing;
using CSBrowser.Dom;
using CSBrowser.Layout;

namespace CSBrowser.Render;

public sealed class DisplayItem : RefCounted
{
    public string Text = "";
    public RectangleF Bounds;
    public float FontSize = 16;
    public Color Color = Color.Black;
    public Color? BackgroundColor;
    public BrowserElement? Element;

    public bool IsImage;
    public Image? Image;

    public TextDecorationType TextDecoration = TextDecorationType.None;

    protected override void Cleanup()
    {
        Image = null;
    }
}
