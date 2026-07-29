namespace CSBrowser.Layout;

/// <summary>
/// CSS display 속성 유형을 정의합니다.
/// </summary>
public enum DisplayType
{
    None,
    Inline,
    Block,
    Flex
}

/// <summary>
/// Flexbox 레이아웃의 주축(main axis) 방향을 정의합니다.
/// </summary>
public enum FlexDirection
{
    Row,
    Column
}

/// <summary>
/// 텍스트 장식(밑줄, 취소선 등) 유형을 정의합니다.
/// </summary>
public enum TextDecorationType
{
    None,
    Underline,
    Overline,
    LineThrough
}

/// <summary>
/// 박스 크기 계산 기준(box-sizing)을 정의합니다.
/// </summary>
public enum BoxSizingType
{
    ContentBox,
    BorderBox
}

/// <summary>
/// Flexbox 항목의 줄바꿈 허용 여부를 정의합니다.
/// </summary>
public enum FlexWrapType
{
    NoWrap,
    Wrap
}

/// <summary>
/// 텍스트 정렬 방식을 정의합니다.
/// </summary>
public enum TextAlignType
{
    Left,
    Center,
    Right
}

/// <summary>
/// 테두리 선의 스타일을 정의합니다.
/// </summary>
public enum BorderStyle
{
    None,
    Solid,
    Dashed,
    Dotted
}

/// <summary>
/// 개별 테두리 변(Top, Bottom, Left, Right)의 속성을 정의하는 구조체입니다.
/// </summary>
public struct BorderSide
{
    public float Width;
    public BorderStyle Style;
    public Color Color;

    // 두께가 존재하고 스타일이 None이 아닐 때만 테두리가 표시됨
    public bool IsVisible => Width > 0 && Style != BorderStyle.None;

    public static readonly BorderSide Empty = new();
}

/// <summary>
/// CSS 계산된 스타일(Computed Style) 정보를 담고 있는 클래스입니다.
/// 레이아웃 및 렌더링에 필요한 폰트, 여백, 테두리, Flexbox 등의 속성을 관리합니다.
/// </summary>
public sealed class ComputedStyle
{
    // 명시적으로 설정된 CSS 속성 이름 집합
    public HashSet<string> SetProperties = new();

    // 폰트 및 텍스트 관련 속성
    public float FontSize = 16;
    public string FontFamily = "Arial";
    public bool IsBold;
    public float LineHeight;
    public bool LineHeightIsMultiplier;

    // 외부 여백(Margin) 속성
    public float MarginTop;
    public float MarginBottom;
    public float MarginLeft;
    public float MarginRight;

    // 내부 여백(Padding) 속성
    public float PaddingTop;
    public float PaddingBottom;
    public float PaddingLeft;
    public float PaddingRight;

    // 사방 테두리 속성
    public BorderSide BorderTop;
    public BorderSide BorderBottom;
    public BorderSide BorderLeft;
    public BorderSide BorderRight;

    // 색상 관련 속성
    public Color Color = Color.Black;
    public Color? BackgroundColor;

    // 레이아웃 및 Flexbox 관련 속성
    public DisplayType Display = DisplayType.Block;
    public FlexDirection FlexDirection = FlexDirection.Row;
    public FlexWrapType FlexWrap = FlexWrapType.NoWrap;
    public float Gap;

    public float FlexGrow;
    public float FlexShrink = 1;
    public float FlexBasis;

    // 기타 렌더링 및 크기 속성
    public TextDecorationType TextDecoration = TextDecorationType.None;
    public BoxSizingType BoxSizing = BoxSizingType.ContentBox;

    public float? Width;
    public float? Height;

    public TextAlignType TextAlign = TextAlignType.Left;

    public float BorderRadius;
}