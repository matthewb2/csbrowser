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

public sealed class ComputedStyle
{
    public float FontSize = 16;

    public float MarginTop;
    public float MarginBottom;
    public float MarginLeft;
    public float MarginRight;

    public Color Color = Color.Black;

    public Color? BackgroundColor;

    public DisplayType Display = DisplayType.Block;
    public FlexDirection FlexDirection = FlexDirection.Row;
}
