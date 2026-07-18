using CSBrowser.Css;
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

    public List<BrowserElement>
        GetElementsByTagName(
            string tagName)
    {
        var result = new List<BrowserElement>();
        FindByTagName(_root, tagName, result);
        return result;
    }

    private static void
        FindByTagName(
            BrowserElement element,
            string tagName,
            List<BrowserElement> result)
    {
        if (element.TagName == tagName)
            result.Add(element);

        foreach (var child
            in element.Children)
        {
            FindByTagName(
                child, tagName, result);
        }
    }

    public List<BrowserElement>
        QuerySelectorAll(
            string selectorText)
    {
        var result = new List<BrowserElement>();
        var selectors =
            CssSelector.ParseList(
                selectorText);

        FindBySelectors(
            _root, selectors, result);

        return result;
    }

    private static void
        FindBySelectors(
            BrowserElement element,
            List<CssSelector> selectors,
            List<BrowserElement> result)
    {
        foreach (var sel in selectors)
        {
            if (sel.Matches(element))
            {
                result.Add(element);
                break;
            }
        }

        foreach (var child
            in element.Children)
        {
            FindBySelectors(
                child, selectors, result);
        }
    }
}
