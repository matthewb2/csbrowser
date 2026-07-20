using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using CSBrowser.JavaScript;
using CSBrowser.Layout;

namespace CSBrowser.Dom;

public sealed class BrowserElement : RefCounted
{
    public string TagName { get; set; } = "";
    public string Text { get; set; } = "";
    public string Id { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string ScriptContent { get; set; } = "";
    public string ImagePath { get; set; } = "";

    public IElement? Source;
    public ICssStyleDeclaration? InlineStyle;

    public ComputedStyle Style = new();

    public ElementState State = ElementState.Normal;
    public BrowserElement? Parent;
    public Dictionary<string, ComputedStyle> PseudoStyles = new();

    public List<BrowserElement> Children = new();
    public Dictionary<string, List<EventListenerInfo>> EventListeners = new();
    public Dictionary<string, string> OnEventHandlers = new();

    public ComputedStyle EffectiveStyle
    {
        get
        {
            if (State != ElementState.Hover)
                return Style;

            var hoverStyle = FindHoverStyle();
            if (hoverStyle == null)
                return Style;

            var merged = new ComputedStyle();
            merged.FontSize = hoverStyle.SetProperties.Contains("font-size") ? hoverStyle.FontSize : Style.FontSize;
            merged.MarginTop = hoverStyle.SetProperties.Contains("margin-top") || hoverStyle.SetProperties.Contains("margin") ? hoverStyle.MarginTop : Style.MarginTop;
            merged.MarginBottom = hoverStyle.SetProperties.Contains("margin-bottom") || hoverStyle.SetProperties.Contains("margin") ? hoverStyle.MarginBottom : Style.MarginBottom;
            merged.MarginLeft = hoverStyle.SetProperties.Contains("margin-left") || hoverStyle.SetProperties.Contains("margin") ? hoverStyle.MarginLeft : Style.MarginLeft;
            merged.MarginRight = hoverStyle.SetProperties.Contains("margin-right") || hoverStyle.SetProperties.Contains("margin") ? hoverStyle.MarginRight : Style.MarginRight;
            merged.PaddingTop = hoverStyle.SetProperties.Contains("padding-top") || hoverStyle.SetProperties.Contains("padding") ? hoverStyle.PaddingTop : Style.PaddingTop;
            merged.PaddingBottom = hoverStyle.SetProperties.Contains("padding-bottom") || hoverStyle.SetProperties.Contains("padding") ? hoverStyle.PaddingBottom : Style.PaddingBottom;
            merged.PaddingLeft = hoverStyle.SetProperties.Contains("padding-left") || hoverStyle.SetProperties.Contains("padding") ? hoverStyle.PaddingLeft : Style.PaddingLeft;
            merged.PaddingRight = hoverStyle.SetProperties.Contains("padding-right") || hoverStyle.SetProperties.Contains("padding") ? hoverStyle.PaddingRight : Style.PaddingRight;
            merged.Color = hoverStyle.SetProperties.Contains("color") ? hoverStyle.Color : Style.Color;
            merged.BackgroundColor = hoverStyle.SetProperties.Contains("background-color") ? hoverStyle.BackgroundColor : Style.BackgroundColor;
            merged.Display = hoverStyle.SetProperties.Contains("display") ? hoverStyle.Display : Style.Display;
            merged.FlexDirection = hoverStyle.SetProperties.Contains("flex-direction") ? hoverStyle.FlexDirection : Style.FlexDirection;
            merged.TextDecoration = hoverStyle.SetProperties.Contains("text-decoration") ? hoverStyle.TextDecoration : Style.TextDecoration;
            merged.BoxSizing = hoverStyle.SetProperties.Contains("box-sizing") ? hoverStyle.BoxSizing : Style.BoxSizing;
            merged.Width = hoverStyle.SetProperties.Contains("width") ? hoverStyle.Width : Style.Width;
            merged.Height = hoverStyle.SetProperties.Contains("height") ? hoverStyle.Height : Style.Height;
            merged.TextAlign = hoverStyle.SetProperties.Contains("text-align") ? hoverStyle.TextAlign : Style.TextAlign;
            merged.FontFamily = hoverStyle.SetProperties.Contains("font-family") ? hoverStyle.FontFamily : Style.FontFamily;
            merged.LineHeight = hoverStyle.SetProperties.Contains("line-height") ? hoverStyle.LineHeight : Style.LineHeight;
            return merged;
        }
    }

    private ComputedStyle? FindHoverStyle()
    {
        if (PseudoStyles.TryGetValue("hover", out var style))
            return style;

        var current = Parent;
        while (current != null)
        {
            if (current.PseudoStyles.TryGetValue("hover", out style))
                return style;
            current = current.Parent;
        }
        return null;
    }

    public BrowserElement? FindAncestorWithPseudoStyle()
    {
        if (PseudoStyles.Count > 0)
            return this;

        var current = Parent;
        while (current != null)
        {
            if (current.PseudoStyles.Count > 0)
                return current;
            current = current.Parent;
        }
        return null;
    }

    protected override void Cleanup()
    {
        foreach (var child in Children)
            child.Unref();
        Children.Clear();
    }
}
