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

public sealed class ComputedStyle
{
    public HashSet<string> SetProperties = new();

    public float FontSize = 16;

    public float MarginTop;
    public float MarginBottom;
    public float MarginLeft;
    public float MarginRight;

    public float PaddingTop;
    public float PaddingBottom;
    public float PaddingLeft;
    public float PaddingRight;

    public Color Color = Color.Black;

    public Color? BackgroundColor;

    public DisplayType Display = DisplayType.Block;
    public FlexDirection FlexDirection = FlexDirection.Row;

    public TextDecorationType TextDecoration = TextDecorationType.None;
    public BoxSizingType BoxSizing = BoxSizingType.ContentBox;

    public float? Width;
    public float? Height;
}
