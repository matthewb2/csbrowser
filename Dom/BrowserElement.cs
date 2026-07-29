using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using CSBrowser.JavaScript;
using CSBrowser.Layout;

namespace CSBrowser.Dom;

/// <summary>
/// 브라우저 DOM 트리의 각 요소를 표현하는 클래스입니다.
/// HTML 요소의 속성, 스타일, 계층 구조 및 이벤트 처리를 관리합니다.
/// </summary>
public sealed class BrowserElement : RefCounted
{
    public string TagName { get; set; } = "";
    public string Text { get; set; } = "";
    public string Id { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string ScriptContent { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public string InputType { get; set; } = "";
    public string Placeholder { get; set; } = "";
    public bool IsChecked { get; set; }

    // 외부 라이브러리(AngleSharp) 연동 객체 및 인라인 스타일
    public IElement? Source;
    public ICssStyleDeclaration? InlineStyle;

    // 스타일 관련 속성 (기본 스타일, 호버 스타일 및 상태)
    public ComputedStyle NormalStyle { get; set; } = new();
    public ComputedStyle HoverStyle { get; set; } = new();
    public bool IsHovered { get; set; }

    // 내부 호버 오버라이드 스타일
    internal ComputedStyle? HoverOverrides { get; set; }

    // DOM 트리 구조 및 이벤트 핸들러 관리
    public BrowserElement? Parent;
    public List<BrowserElement> Children = new();
    public Dictionary<string, List<EventListenerInfo>> EventListeners = new();
    public Dictionary<string, string> OnEventHandlers = new();

    /// <summary>
    /// 현재 요소 또는 상위 조상 중 호버(Hover) 오버라이드 스타일이 적용될 수 있는 요소를 찾습니다.
    /// </summary>
    public BrowserElement? FindHoverableAncestor()
    {
        if (HoverOverrides != null)
            return this;

        var current = Parent;
        while (current != null)
        {
            if (current.HoverOverrides != null)
                return current;
            current = current.Parent;
        }
        return null;
    }

    /// <summary>
    /// 객체 소멸 시 자식 요소들의 참조를 해제하고 메모리를 정리합니다.
    /// </summary>
    protected override void Cleanup()
    {
        foreach (var child in Children)
            child.Unref();
        Children.Clear();
    }
}
