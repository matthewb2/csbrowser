using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using CSBrowser.Dom;
using CSBrowser.Layout;

namespace CSBrowser.Html;

public sealed class HtmlLoader
{
    public async Task<BrowserElement> LoadAsync(string html)
    {
        Log.WriteLine("[HtmlLoader] Parsing HTML...");

        var context = BrowsingContext.New();
        var doc = await context.OpenAsync(req => req.Content(html));

        return Convert(doc.DocumentElement);
    }

    private BrowserElement Convert(IElement element)
    {
        var node = new BrowserElement();

        node.TagName = element.TagName.ToLower();

        if (node.TagName is "head" or "script" or "style" or "meta" or "link" or "title")
            node.Style.Display = DisplayType.None;

        Log.WriteLine($"  [HtmlLoader] <{node.TagName}>");

        node.Id = element.Id ?? "";

        var inlineStyle = element.GetAttribute("style");
        if (!string.IsNullOrEmpty(inlineStyle))
        {
            Log.WriteLine($"  [HtmlLoader]  inline style: \"{inlineStyle}\"");
            ApplyInlineStyle(node, inlineStyle);
        }

        if (element is IHtmlScriptElement script)
        {
            node.ScriptContent = script.Text;
        }
        else if (element.Children.Length == 0)
        {
            node.Text = element.TextContent.Trim();
        }

        foreach (var child in element.Children)
        {
            var childNode = Convert(child);
            node.Children.Add(childNode);
        }

        return node;
    }

    private void ApplyInlineStyle(BrowserElement node, string style)
    {
        var props = style.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var prop in props)
        {
            var parts = prop.Split(':', 2);
            if (parts.Length != 2) continue;

            var name = parts[0].Trim().ToLowerInvariant();
            var value = parts[1].Trim().ToLowerInvariant();

            switch (name)
            {
                case "display":
                    if (value == "flex")
                        node.Style.Display = DisplayType.Flex;
                    break;

                case "flex-direction":
                    if (value == "column")
                        node.Style.FlexDirection = FlexDirection.Column;
                    break;

                case "font-size":
                    if (float.TryParse(
                            value.Replace("px", ""),
                            out float fs))
                        node.Style.FontSize = fs;
                    break;

                case "margin":
                    if (float.TryParse(
                            value.Replace("px", ""),
                            out float m))
                    {
                        node.Style.MarginTop = m;
                        node.Style.MarginBottom = m;
                        node.Style.MarginLeft = m;
                        node.Style.MarginRight = m;
                    }
                    break;

                case "color":
                    node.Style.Color = Css.CssColorParser.Parse(value);
                    break;

                case "background-color":
                    node.Style.BackgroundColor = Css.CssColorParser.Parse(value);
                    break;
            }
        }
    }
}
