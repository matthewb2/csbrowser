using System.Drawing;
using CSBrowser.Dom;
using CSBrowser.Layout;

namespace CSBrowser.Render;

/// <summary>
/// 화면에 렌더링될 개별 요소나 텍스트 조각을 표현하는 디스플레이 아이템 클래스입니다.
/// 레이아웃 결과물로 생성되며, 실제 화면에 그려질 위치, 텍스트, 이미지, 스타일 정보를 담고 있습니다.
/// </summary>
public sealed class DisplayItem : RefCounted
{
    // 기본 텍스트 및 영역 정보
    public string Text = "";
    public RectangleF Bounds;
    public float FontSize = 16;
    public Color Color = Color.Black;
    public Color? BackgroundColor;
    public BrowserElement? Element;

    // 이미지 렌더링 관련 속성
    public bool IsImage;
    public Image? Image;

    // 텍스트 정렬 및 장식 속성
    public TextDecorationType TextDecoration = TextDecorationType.None;
    public TextAlignType TextAlign = TextAlignType.Left;

    // 폰트 스타일 속성
    public string FontFamily = "Arial";
    public bool IsBold;

    // 상단 테두리 스타일
    public float BorderTopWidth;
    public Layout.BorderStyle BorderTopStyle;
    public Color BorderTopColor;

    // 하단 테두리 스타일
    public float BorderBottomWidth;
    public Layout.BorderStyle BorderBottomStyle;
    public Color BorderBottomColor;

    // 좌측 테두리 스타일
    public float BorderLeftWidth;
    public Layout.BorderStyle BorderLeftStyle;
    public Color BorderLeftColor;

    // 우측 테두리 스타일
    public float BorderRightWidth;
    public Layout.BorderStyle BorderRightStyle;
    public Color BorderRightColor;

    // 모서리 둥글기 속성
    public float BorderRadius;

    /// <summary>
    /// 객체 소멸 시 참조하는 이미지 리소스를 해제하고 정리합니다.
    /// </summary>
    protected override void Cleanup()
    {
        Image = null;
    }
}