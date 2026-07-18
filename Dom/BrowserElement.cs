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

    public ComputedStyle Style = new();

    public List<BrowserElement> Children = new();
    public Dictionary<string, List<EventListenerInfo>> EventListeners = new();
    public Dictionary<string, string> OnEventHandlers = new();

    protected override void Cleanup()
    {
        foreach (var child in Children)
            child.Unref();
        Children.Clear();
    }
}
