namespace CSBrowser.JavaScript;

public sealed class JsDocument
{
    private readonly BrowserDocument _doc;

    public JsDocument(
        BrowserDocument doc)
    {
        _doc = doc;
    }

    public JsElement?
        getElementById(string id)
    {
        var element =
            _doc.GetElementById(id);

        return element != null
            ? new JsElement(element)
            : null;
    }
}
