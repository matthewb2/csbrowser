using CSBrowser.Layout;

namespace CSBrowser.Dom;

public sealed class BrowserElement
{
    public string TagName { get; set; } = "";

    public string Text { get; set; } = "";

    public List<BrowserElement> Children
        = new();

    public ComputedStyle Style
        = new();
}