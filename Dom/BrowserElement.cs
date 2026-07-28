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
    public string InputType { get; set; } = "";
    public string Placeholder { get; set; } = "";
    public bool IsChecked { get; set; }

    public IElement? Source;
    public ICssStyleDeclaration? InlineStyle;

    public ComputedStyle NormalStyle { get; set; } = new();
    public ComputedStyle HoverStyle { get; set; } = new();
    public bool IsHovered { get; set; }

    internal ComputedStyle? HoverOverrides { get; set; }

    public BrowserElement? Parent;
    public List<BrowserElement> Children = new();
    public Dictionary<string, List<EventListenerInfo>> EventListeners = new();
    public Dictionary<string, string> OnEventHandlers = new();

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

    protected override void Cleanup()
    {
        foreach (var child in Children)
            child.Unref();
        Children.Clear();
    }
}
