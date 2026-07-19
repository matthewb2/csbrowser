using AngleSharp;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using CSBrowser.Dom;
using CSBrowser.Layout;

namespace CSBrowser.Html;

public sealed class HtmlLoader
{
    private readonly Dictionary<BrowserElement, string> _inlineStyles = new();

    public async Task<BrowserElement> LoadAsync(string html)
    {
        Log.WriteLine("[HtmlLoader] Parsing HTML...");

        var config = Configuration.Default.WithCss();
        var context = BrowsingContext.New(config);
        var doc = await context.OpenAsync(req => req.Content(html));

        var root = Convert(doc.DocumentElement);

        ApplyStyles(root, doc);
        ApplyInlineStyles();

        return root;
    }

    private BrowserElement Convert(IElement element)
    {
        var node = new BrowserElement();
        node.Source = element;

        node.TagName = element.TagName.ToLower();

        if (node.TagName is "head" or "script" or "style" or "meta" or "link" or "title")
            node.Style.Display = DisplayType.None;

        Log.WriteLine($"  [HtmlLoader] <{node.TagName}>");

        node.Id = element.Id ?? "";
        node.ClassName = element.GetAttribute("class") ?? "";

        var inlineStyle = element.GetAttribute("style");
        if (!string.IsNullOrEmpty(inlineStyle))
        {
            Log.WriteLine($"  [HtmlLoader]  inline style (deferred): \"{inlineStyle}\"");
            _inlineStyles[node] = inlineStyle;
        }

        foreach (var attr in element.Attributes)
        {
            var name = attr.Name.ToLowerInvariant();
            if (name.StartsWith("on") && name.Length > 2 && !string.IsNullOrEmpty(attr.Value))
            {
                var eventType = name[2..];
                node.OnEventHandlers[eventType] = attr.Value;
                Log.WriteLine($"  [HtmlLoader]  on{eventType}=\"{attr.Value}\"");
            }
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

    private void ApplyStyles(BrowserElement root, IDocument doc)
    {
        Log.WriteLine("[HtmlLoader] Applying <style> rules...");

        foreach (var sheet in doc.StyleSheets)
        {
            if (sheet is not ICssStyleSheet cssSheet)
                continue;

            foreach (var rule in cssSheet.Rules)
            {
                if (rule is not ICssStyleRule styleRule)
                    continue;

                Log.WriteLine($"  [HtmlLoader]  rule: {styleRule.SelectorText}");

                IHtmlCollection<IElement> matched;
                try
                {
                    matched = doc.QuerySelectorAll(styleRule.SelectorText);
                }
                catch
                {
                    Log.WriteLine($"  [HtmlLoader]  unsupported selector, skipping: {styleRule.SelectorText}");
                    continue;
                }

                foreach (var element in matched)
                {
                    var browserEl = FindBrowserElement(root, element);
                    if (browserEl == null)
                        continue;

                    ApplyCssDeclaration(browserEl, styleRule.Style);
                }
            }
        }
    }

    private static BrowserElement? FindBrowserElement(
        BrowserElement root, IElement target)
    {
        if (root.Source == target)
            return root;

        foreach (var child in root.Children)
        {
            var found = FindBrowserElement(child, target);
            if (found != null)
                return found;
        }

        return null;
    }

    private void ApplyCssDeclaration(
        BrowserElement node, ICssStyleDeclaration style)
    {
        var fontSize = style.GetPropertyValue("font-size");
        if (!string.IsNullOrEmpty(fontSize) &&
            float.TryParse(fontSize.Replace("px", ""), out float fs))
        {
            Log.WriteLine($"    [Css] <{node.TagName}> font-size={fs}");
            node.Style.FontSize = fs;
        }

        var margin = style.GetPropertyValue("margin");
        if (!string.IsNullOrEmpty(margin) &&
            float.TryParse(margin.Replace("px", ""), out float m))
        {
            Log.WriteLine($"    [Css] <{node.TagName}> margin={m}");
            node.Style.MarginTop = m;
            node.Style.MarginBottom = m;
            node.Style.MarginLeft = m;
            node.Style.MarginRight = m;
        }

        var marginTop = style.GetPropertyValue("margin-top");
        if (!string.IsNullOrEmpty(marginTop) &&
            float.TryParse(marginTop.Replace("px", ""), out float mt))
        {
            node.Style.MarginTop = mt;
        }

        var marginBottom = style.GetPropertyValue("margin-bottom");
        if (!string.IsNullOrEmpty(marginBottom) &&
            float.TryParse(marginBottom.Replace("px", ""), out float mb))
        {
            node.Style.MarginBottom = mb;
        }

        var marginLeft = style.GetPropertyValue("margin-left");
        if (!string.IsNullOrEmpty(marginLeft) &&
            float.TryParse(marginLeft.Replace("px", ""), out float ml))
        {
            node.Style.MarginLeft = ml;
        }

        var marginRight = style.GetPropertyValue("margin-right");
        if (!string.IsNullOrEmpty(marginRight) &&
            float.TryParse(marginRight.Replace("px", ""), out float mr))
        {
            node.Style.MarginRight = mr;
        }

        var color = style.GetPropertyValue("color");
        if (!string.IsNullOrEmpty(color))
        {
            Log.WriteLine($"    [Css] <{node.TagName}> color={color}");
            node.Style.Color = Css.CssColorParser.Parse(color);
        }

        var bgColor = style.GetPropertyValue("background-color");
        if (!string.IsNullOrEmpty(bgColor))
        {
            Log.WriteLine($"    [Css] <{node.TagName}> background-color={bgColor}");
            node.Style.BackgroundColor = Css.CssColorParser.Parse(bgColor);
        }

        var display = style.GetPropertyValue("display");
        if (!string.IsNullOrEmpty(display))
        {
            Log.WriteLine($"    [Css] <{node.TagName}> display={display}");
            if (display.Equals("flex", StringComparison.OrdinalIgnoreCase))
                node.Style.Display = DisplayType.Flex;
            else if (display.Equals("none", StringComparison.OrdinalIgnoreCase))
                node.Style.Display = DisplayType.None;
            else if (display.Equals("block", StringComparison.OrdinalIgnoreCase))
                node.Style.Display = DisplayType.Block;
            else if (display.Equals("inline", StringComparison.OrdinalIgnoreCase))
                node.Style.Display = DisplayType.Inline;
        }

        var flexDir = style.GetPropertyValue("flex-direction");
        if (!string.IsNullOrEmpty(flexDir))
        {
            Log.WriteLine($"    [Css] <{node.TagName}> flex-direction={flexDir}");
            if (flexDir.Equals("column", StringComparison.OrdinalIgnoreCase))
                node.Style.FlexDirection = Layout.FlexDirection.Column;
            else
                node.Style.FlexDirection = Layout.FlexDirection.Row;
        }
    }

    private void ApplyInlineStyles()
    {
        Log.WriteLine("[HtmlLoader] Applying inline styles...");

        foreach (var (node, styleText) in _inlineStyles)
        {
            ApplyInlineStyle(node, styleText);
        }

        _inlineStyles.Clear();
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
                    else if (value == "none")
                        node.Style.Display = DisplayType.None;
                    else if (value == "block")
                        node.Style.Display = DisplayType.Block;
                    else if (value == "inline")
                        node.Style.Display = DisplayType.Inline;
                    break;

                case "flex-direction":
                    if (value == "column")
                        node.Style.FlexDirection = Layout.FlexDirection.Column;
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

                case "margin-top":
                    if (float.TryParse(value.Replace("px", ""), out float mt))
                        node.Style.MarginTop = mt;
                    break;

                case "margin-bottom":
                    if (float.TryParse(value.Replace("px", ""), out float mb))
                        node.Style.MarginBottom = mb;
                    break;

                case "margin-left":
                    if (float.TryParse(value.Replace("px", ""), out float ml))
                        node.Style.MarginLeft = ml;
                    break;

                case "margin-right":
                    if (float.TryParse(value.Replace("px", ""), out float mr))
                        node.Style.MarginRight = mr;
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
