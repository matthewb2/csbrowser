namespace CSBrowser.Layout;

public enum DisplayType
{
    None,
    Inline,
    Block,
    Flex
}

public enum FlexDirection
{
    Row,
    Column
}

public enum TextDecorationType
{
    None,
    Underline,
    Overline,
    LineThrough
}

public enum BoxSizingType
{
    ContentBox,
    BorderBox
}

public enum ElementState
{
    Normal,
    Hover
}

public enum TextAlignType
{
    Left,
    Center,
    Right
}

public enum BorderStyle
{
    None,
    Solid,
    Dashed,
    Dotted
}

public struct BorderSide
{
    public float Width;
    public BorderStyle Style;
    public Color Color;
    public bool IsVisible => Width > 0 && Style != BorderStyle.None;

    public static readonly BorderSide Empty = new();
}

public sealed class ComputedStyle
{
    public HashSet<string> SetProperties = new();

    public float FontSize = 16;
    public string FontFamily = "Arial";
    public float LineHeight;

    public float MarginTop;
    public float MarginBottom;
    public float MarginLeft;
    public float MarginRight;

    public float PaddingTop;
    public float PaddingBottom;
    public float PaddingLeft;
    public float PaddingRight;

    public BorderSide BorderTop;
    public BorderSide BorderBottom;
    public BorderSide BorderLeft;
    public BorderSide BorderRight;

    public Color Color = Color.Black;
    public Color? BackgroundColor;

    public DisplayType Display = DisplayType.Block;
    public FlexDirection FlexDirection = FlexDirection.Row;

    public TextDecorationType TextDecoration = TextDecorationType.None;
    public BoxSizingType BoxSizing = BoxSizingType.ContentBox;

    public float? Width;
    public float? Height;

    public TextAlignType TextAlign = TextAlignType.Left;
}
