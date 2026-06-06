using CSBrowser.Layout;

namespace CSBrowser.Dom;

public sealed class BrowserElement
{
    public string TagName { get; set; } = "";

    public string Text { get; set; } = "";

    public string Id { get; set; } = "";

    public string ScriptContent { get; set; } = "";

    public ComputedStyle Style
        = new();

    public List<BrowserElement> Children
        = new();
}