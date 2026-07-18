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

    public JsElement[]
        getElementsByTagName(
            string tagName)
    {
        var elements =
            _doc.GetElementsByTagName(
                tagName);

        return elements
            .Select(e => new JsElement(e))
            .ToArray();
    }

    public JsElement[]
        querySelectorAll(
            string selectors)
    {
        var elements =
            _doc.QuerySelectorAll(
                selectors);

        return elements
            .Select(e => new JsElement(e))
            .ToArray();
    }
}
