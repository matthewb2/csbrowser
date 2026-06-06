using CSBrowser.Dom;

namespace CSBrowser.JavaScript;

public sealed class JsElement
{
    private readonly BrowserElement _element;

    public JsElement(
        BrowserElement element)
    {
        _element = element;
    }

    public string innerText
    {
        get => _element.Text;
        set => _element.Text = value;
    }
}
