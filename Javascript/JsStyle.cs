using CSBrowser.Dom;

namespace CSBrowser.JavaScript;

public sealed class JsStyle
{
    private readonly BrowserElement _element;

    public JsStyle(BrowserElement element)
    {
        _element = element;
    }

    public string color
    {
        set => _element.NormalStyle.Color = Css.CssColorParser.Parse(value);
    }
}
