using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using CSBrowser.Dom;

namespace CSBrowser.Html;

public sealed class HtmlLoader
{
    public async Task<BrowserElement>
        LoadAsync(string html)
    {
        var context =
            BrowsingContext.New();

        var doc =
            await context.OpenAsync(
                req => req.Content(html));

        return Convert(
            doc.DocumentElement);
    }

    private BrowserElement Convert(
    IElement element)
    {
        var node =
            new BrowserElement();

        node.TagName =
            element.TagName.ToLower();

        node.Id =
            element.Id;

        if (element is IHtmlScriptElement script)
        {
            node.ScriptContent =
                script.Text;
        }
        else if (element.Children.Length == 0)
        {
            node.Text =
                element.TextContent.Trim();
        }

        foreach (var child
            in element.Children)
        {
            node.Children.Add(
                Convert(child));
        }

        return node;
    }
}