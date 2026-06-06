using CSBrowser.Dom;

namespace CSBrowser.JavaScript;

public sealed class BrowserDocument
{
    private readonly BrowserElement _root;

    public BrowserDocument(
        BrowserElement root)
    {
        _root = root;
    }

    public BrowserElement?
        GetElementById(string id)
    {
        return FindById(_root, id);
    }

    private static BrowserElement?
        FindById(
            BrowserElement element,
            string id)
    {
        if (element.Id == id)
            return element;

        foreach (var child
            in element.Children)
        {
            var found =
                FindById(child, id);

            if (found != null)
                return found;
        }

        return null;
    }
}
