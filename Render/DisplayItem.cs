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
    public TextAlignType TextAlign = TextAlignType.Left;

    public string FontFamily = "Arial";

    public float BorderTopWidth;
    public Layout.BorderStyle BorderTopStyle;
    public Color BorderTopColor;

    public float BorderBottomWidth;
    public Layout.BorderStyle BorderBottomStyle;
    public Color BorderBottomColor;

    public float BorderLeftWidth;
    public Layout.BorderStyle BorderLeftStyle;
    public Color BorderLeftColor;

    public float BorderRightWidth;
    public Layout.BorderStyle BorderRightStyle;
    public Color BorderRightColor;

    protected override void Cleanup()
    {
        Image = null;
    }
}
