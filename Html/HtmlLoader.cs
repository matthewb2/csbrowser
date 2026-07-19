using AngleSharp;
using AngleSharp.Css;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using CSBrowser.Dom;
using CSBrowser.Layout;

namespace CSBrowser.Html;

public sealed class HtmlLoader
{
    private string? _baseDir;

    public async Task<BrowserElement> LoadAsync(string html, string? baseDir = null)
    {
        Log.WriteLine("[HtmlLoader] Parsing HTML...");

        _baseDir = baseDir;

        var config = Configuration.Default.WithCss();
        var context = BrowsingContext.New(config);
        var doc = await context.OpenAsync(req => req.Content(html));

        var root = Convert(doc.DocumentElement);

        await ApplyLinkedStylesheets(root, doc);
        ApplyStyles(root, doc);
        ApplyInlineStyles(root);

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

        var inlineStyle = element.GetStyle();
        if (inlineStyle != null && inlineStyle.Length > 0)
        {
            Log.WriteLine($"  [HtmlLoader]  inline style detected ({inlineStyle.Length} props)");
            node.InlineStyle = inlineStyle;
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
        else if (element is IHtmlImageElement img)
        {
            var src = img.GetAttribute("src") ?? img.Source;
            Log.WriteLine($"  [HtmlLoader]  <img> detected: src={src ?? "null"} baseDir={_baseDir ?? "null"}");
            if (!string.IsNullOrEmpty(src) && !string.IsNullOrEmpty(_baseDir))
            {
                node.ImagePath = Path.GetFullPath(Path.Combine(_baseDir, src));
                Log.WriteLine($"  [HtmlLoader]  <img> resolved: {node.ImagePath} exists={File.Exists(node.ImagePath)}");
            }
        }
        else if (element.Children.Length == 0)
        {
            node.Text = element.TextContent.Trim();
        }

        foreach (var child in element.Children)
        {
            var childNode = Convert(child);
            childNode.Parent = node;
            node.Children.Add(childNode);
        }

        return node;
    }

    private async Task ApplyLinkedStylesheets(BrowserElement root, IDocument doc)
    {
        if (string.IsNullOrEmpty(_baseDir))
            return;

        Log.WriteLine("[HtmlLoader] Loading linked stylesheets...");

        var linkElements = doc.QuerySelectorAll("link[rel=\"stylesheet\"]");
        foreach (var link in linkElements)
        {
            var href = link.GetAttribute("href");
            if (string.IsNullOrEmpty(href))
                continue;

            var cssPath = Path.Combine(_baseDir, href);
            if (!File.Exists(cssPath))
            {
                Log.WriteLine($"  [HtmlLoader]  CSS file not found: {cssPath}");
                continue;
            }

            Log.WriteLine($"  [HtmlLoader]  loading CSS: {cssPath}");
            var cssText = await File.ReadAllTextAsync(cssPath);

            var parser = new AngleSharp.Css.Parser.CssParser();
            var sheet = parser.ParseStyleSheet(cssText);

            foreach (var rule in sheet.Rules)
            {
                if (rule is not ICssStyleRule styleRule)
                    continue;

                var selector = styleRule.SelectorText;
                Log.WriteLine($"  [HtmlLoader]  linked rule: {selector}");

                if (TryExtractPseudoSelector(selector, out string pseudoName, out string baseSelector))
                {
                    ApplyPseudoStyles(root, doc, baseSelector, pseudoName, styleRule.Style);
                    continue;
                }

                IHtmlCollection<IElement> matched;
                try
                {
                    matched = doc.QuerySelectorAll(selector);
                }
                catch
                {
                    Log.WriteLine($"  [HtmlLoader]  unsupported selector, skipping: {selector}");
                    continue;
                }

                foreach (var element in matched)
                {
                    var browserEl = FindBrowserElement(root, element);
                    if (browserEl == null)
                        continue;

                    ApplyCssDeclaration(browserEl.Style, browserEl.TagName, styleRule.Style);
                }
            }
        }
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

                var selector = styleRule.SelectorText;
                Log.WriteLine($"  [HtmlLoader]  rule: {selector}");

                if (TryExtractPseudoSelector(selector, out string pseudoName, out string baseSelector))
                {
                    ApplyPseudoStyles(root, doc, baseSelector, pseudoName, styleRule.Style);
                    continue;
                }

                IHtmlCollection<IElement> matched;
                try
                {
                    matched = doc.QuerySelectorAll(selector);
                }
                catch
                {
                    Log.WriteLine($"  [HtmlLoader]  unsupported selector, skipping: {selector}");
                    continue;
                }

                foreach (var element in matched)
                {
                    var browserEl = FindBrowserElement(root, element);
                    if (browserEl == null)
                        continue;

                    ApplyCssDeclaration(browserEl.Style, browserEl.TagName, styleRule.Style);
                }
            }
        }
    }

    private static void ApplyInlineStyles(BrowserElement root)
    {
        Log.WriteLine("[HtmlLoader] Applying inline styles...");

        if (root.InlineStyle != null)
            ApplyCssDeclaration(root.Style, root.TagName, root.InlineStyle);

        foreach (var child in root.Children)
            ApplyInlineStyles(child);
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

    private static bool TryExtractPseudoSelector(
        string selector, out string pseudoName, out string baseSelector)
    {
        pseudoName = "";
        baseSelector = "";

        int colonIndex = selector.IndexOf(':');
        if (colonIndex < 0)
            return false;

        pseudoName = selector[(colonIndex + 1)..].Trim().ToLowerInvariant();
        baseSelector = selector[..colonIndex].Trim();

        if (string.IsNullOrEmpty(baseSelector))
        {
            baseSelector = "*";
            return true;
        }

        return true;
    }

    private void ApplyPseudoStyles(
        BrowserElement root, IDocument doc,
        string baseSelector, string pseudoName,
        ICssStyleDeclaration css)
    {
        Log.WriteLine($"  [HtmlLoader]  pseudo rule: {baseSelector}:{pseudoName}");

        IHtmlCollection<IElement> matched;
        try
        {
            matched = doc.QuerySelectorAll(baseSelector);
        }
        catch
        {
            Log.WriteLine($"  [HtmlLoader]  unsupported base selector, skipping: {baseSelector}");
            return;
        }

        var pseudoStyle = new ComputedStyle();
        ApplyCssDeclaration(pseudoStyle, $"{baseSelector}:{pseudoName}", css);

        foreach (var element in matched)
        {
            var browserEl = FindBrowserElement(root, element);
            if (browserEl == null)
                continue;

            browserEl.PseudoStyles[pseudoName] = pseudoStyle;
            Log.WriteLine($"    [Css] <{browserEl.TagName}> stored :{pseudoName} pseudo-style");
        }
    }

    private static void ApplyCssDeclaration(
        ComputedStyle style, string tagName, ICssStyleDeclaration css)
    {
        var fontSize = css.GetPropertyValue("font-size");
        if (!string.IsNullOrEmpty(fontSize) &&
            float.TryParse(fontSize.Replace("px", ""), out float fs))
        {
            Log.WriteLine($"    [Css] <{tagName}> font-size={fs}");
            style.FontSize = fs;
            style.SetProperties.Add("font-size");
        }

        var margin = css.GetPropertyValue("margin");
        if (!string.IsNullOrEmpty(margin) &&
            float.TryParse(margin.Replace("px", ""), out float m))
        {
            Log.WriteLine($"    [Css] <{tagName}> margin={m}");
            style.MarginTop = m;
            style.MarginBottom = m;
            style.MarginLeft = m;
            style.MarginRight = m;
            style.SetProperties.Add("margin");
        }

        var marginTop = css.GetPropertyValue("margin-top");
        if (!string.IsNullOrEmpty(marginTop) &&
            float.TryParse(marginTop.Replace("px", ""), out float mt))
        {
            style.MarginTop = mt;
            style.SetProperties.Add("margin-top");
        }

        var marginBottom = css.GetPropertyValue("margin-bottom");
        if (!string.IsNullOrEmpty(marginBottom) &&
            float.TryParse(marginBottom.Replace("px", ""), out float mb))
        {
            style.MarginBottom = mb;
            style.SetProperties.Add("margin-bottom");
        }

        var marginLeft = css.GetPropertyValue("margin-left");
        if (!string.IsNullOrEmpty(marginLeft) &&
            float.TryParse(marginLeft.Replace("px", ""), out float ml))
        {
            style.MarginLeft = ml;
            style.SetProperties.Add("margin-left");
        }

        var marginRight = css.GetPropertyValue("margin-right");
        if (!string.IsNullOrEmpty(marginRight) &&
            float.TryParse(marginRight.Replace("px", ""), out float mr))
        {
            style.MarginRight = mr;
            style.SetProperties.Add("margin-right");
        }

        var padding = css.GetPropertyValue("padding");
        if (!string.IsNullOrEmpty(padding) &&
            float.TryParse(padding.Replace("px", ""), out float p))
        {
            style.PaddingTop = p;
            style.PaddingBottom = p;
            style.PaddingLeft = p;
            style.PaddingRight = p;
            style.SetProperties.Add("padding");
        }

        var paddingTop = css.GetPropertyValue("padding-top");
        if (!string.IsNullOrEmpty(paddingTop) &&
            float.TryParse(paddingTop.Replace("px", ""), out float pt))
        {
            style.PaddingTop = pt;
            style.SetProperties.Add("padding-top");
        }

        var paddingBottom = css.GetPropertyValue("padding-bottom");
        if (!string.IsNullOrEmpty(paddingBottom) &&
            float.TryParse(paddingBottom.Replace("px", ""), out float pb))
        {
            style.PaddingBottom = pb;
            style.SetProperties.Add("padding-bottom");
        }

        var paddingLeft = css.GetPropertyValue("padding-left");
        if (!string.IsNullOrEmpty(paddingLeft) &&
            float.TryParse(paddingLeft.Replace("px", ""), out float pl))
        {
            style.PaddingLeft = pl;
            style.SetProperties.Add("padding-left");
        }

        var paddingRight = css.GetPropertyValue("padding-right");
        if (!string.IsNullOrEmpty(paddingRight) &&
            float.TryParse(paddingRight.Replace("px", ""), out float pr))
        {
            style.PaddingRight = pr;
            style.SetProperties.Add("padding-right");
        }

        var color = css.GetPropertyValue("color");
        if (!string.IsNullOrEmpty(color))
        {
            Log.WriteLine($"    [Css] <{tagName}> color={color}");
            style.Color = Css.CssColorParser.Parse(color);
            style.SetProperties.Add("color");
        }

        var bgColor = css.GetPropertyValue("background-color");
        if (!string.IsNullOrEmpty(bgColor))
        {
            Log.WriteLine($"    [Css] <{tagName}> background-color={bgColor}");
            style.BackgroundColor = Css.CssColorParser.Parse(bgColor);
            style.SetProperties.Add("background-color");
        }

        var display = css.GetPropertyValue("display");
        if (!string.IsNullOrEmpty(display))
        {
            Log.WriteLine($"    [Css] <{tagName}> display={display}");
            if (display.Equals("flex", StringComparison.OrdinalIgnoreCase))
                style.Display = DisplayType.Flex;
            else if (display.Equals("none", StringComparison.OrdinalIgnoreCase))
                style.Display = DisplayType.None;
            else if (display.Equals("block", StringComparison.OrdinalIgnoreCase))
                style.Display = DisplayType.Block;
            else if (display.Equals("inline", StringComparison.OrdinalIgnoreCase))
                style.Display = DisplayType.Inline;
            style.SetProperties.Add("display");
        }

        var flexDir = css.GetPropertyValue("flex-direction");
        if (!string.IsNullOrEmpty(flexDir))
        {
            Log.WriteLine($"    [Css] <{tagName}> flex-direction={flexDir}");
            if (flexDir.Equals("column", StringComparison.OrdinalIgnoreCase))
                style.FlexDirection = Layout.FlexDirection.Column;
            else
                style.FlexDirection = Layout.FlexDirection.Row;
            style.SetProperties.Add("flex-direction");
        }

        var textDecoration = css.GetPropertyValue("text-decoration");
        if (!string.IsNullOrEmpty(textDecoration))
        {
            Log.WriteLine($"    [Css] <{tagName}> text-decoration={textDecoration}");
            if (textDecoration.Contains("underline", StringComparison.OrdinalIgnoreCase))
                style.TextDecoration = TextDecorationType.Underline;
            else if (textDecoration.Contains("overline", StringComparison.OrdinalIgnoreCase))
                style.TextDecoration = TextDecorationType.Overline;
            else if (textDecoration.Contains("line-through", StringComparison.OrdinalIgnoreCase))
                style.TextDecoration = TextDecorationType.LineThrough;
            else if (textDecoration.Contains("none", StringComparison.OrdinalIgnoreCase))
                style.TextDecoration = TextDecorationType.None;
            style.SetProperties.Add("text-decoration");
        }

        var boxSizing = css.GetPropertyValue("box-sizing");
        if (!string.IsNullOrEmpty(boxSizing))
        {
            Log.WriteLine($"    [Css] <{tagName}> box-sizing={boxSizing}");
            if (boxSizing.Equals("border-box", StringComparison.OrdinalIgnoreCase))
                style.BoxSizing = BoxSizingType.BorderBox;
            else
                style.BoxSizing = BoxSizingType.ContentBox;
            style.SetProperties.Add("box-sizing");
        }

        var cssWidth = css.GetPropertyValue("width");
        if (!string.IsNullOrEmpty(cssWidth) &&
            float.TryParse(cssWidth.Replace("px", ""), out float cw))
        {
            Log.WriteLine($"    [Css] <{tagName}> width={cw}");
            style.Width = cw;
            style.SetProperties.Add("width");
        }

        var cssHeight = css.GetPropertyValue("height");
        if (!string.IsNullOrEmpty(cssHeight) &&
            float.TryParse(cssHeight.Replace("px", ""), out float ch))
        {
            Log.WriteLine($"    [Css] <{tagName}> height={ch}");
            style.Height = ch;
            style.SetProperties.Add("height");
        }
    }
}
