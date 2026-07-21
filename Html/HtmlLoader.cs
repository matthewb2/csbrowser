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

        bool hasContentChildren = false;
        foreach (var child in element.ChildNodes)
        {
            if (child is IElement childElement)
            {
                var childNode = Convert(childElement);
                childNode.Parent = node;
                node.Children.Add(childNode);
                hasContentChildren = true;
            }
            else if (child is IText textNode)
            {
                var text = textNode.Data;
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var textChild = new BrowserElement();
                textChild.TagName = "#text";
                textChild.Text = text.Trim();
                textChild.Style.Display = DisplayType.Inline;
                textChild.Parent = node;
                node.Children.Add(textChild);
                hasContentChildren = true;
            }
        }

        if (!hasContentChildren)
        {
            node.Text = element.TextContent.Trim();
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
            TryParseCssLength(fontSize, out float fs))
        {
            Log.WriteLine($"    [Css] <{tagName}> font-size={fs}");
            style.FontSize = fs;
            style.SetProperties.Add("font-size");
        }

        var margin = css.GetPropertyValue("margin");
        if (!string.IsNullOrEmpty(margin) &&
            TryParseCssLength(margin, out float m))
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
            TryParseCssLength(marginTop, out float mt))
        {
            style.MarginTop = mt;
            style.SetProperties.Add("margin-top");
        }

        var marginBottom = css.GetPropertyValue("margin-bottom");
        if (!string.IsNullOrEmpty(marginBottom) &&
            TryParseCssLength(marginBottom, out float mb))
        {
            style.MarginBottom = mb;
            style.SetProperties.Add("margin-bottom");
        }

        var marginLeft = css.GetPropertyValue("margin-left");
        if (!string.IsNullOrEmpty(marginLeft) &&
            TryParseCssLength(marginLeft, out float ml))
        {
            style.MarginLeft = ml;
            style.SetProperties.Add("margin-left");
        }

        var marginRight = css.GetPropertyValue("margin-right");
        if (!string.IsNullOrEmpty(marginRight) &&
            TryParseCssLength(marginRight, out float mr))
        {
            style.MarginRight = mr;
            style.SetProperties.Add("margin-right");
        }

        var padding = css.GetPropertyValue("padding");
        if (!string.IsNullOrEmpty(padding) &&
            TryParseCssLength(padding, out float p))
        {
            style.PaddingTop = p;
            style.PaddingBottom = p;
            style.PaddingLeft = p;
            style.PaddingRight = p;
            style.SetProperties.Add("padding");
        }

        var paddingTop = css.GetPropertyValue("padding-top");
        if (!string.IsNullOrEmpty(paddingTop) &&
            TryParseCssLength(paddingTop, out float pt))
        {
            style.PaddingTop = pt;
            style.SetProperties.Add("padding-top");
        }

        var paddingBottom = css.GetPropertyValue("padding-bottom");
        if (!string.IsNullOrEmpty(paddingBottom) &&
            TryParseCssLength(paddingBottom, out float pb))
        {
            style.PaddingBottom = pb;
            style.SetProperties.Add("padding-bottom");
        }

        var paddingLeft = css.GetPropertyValue("padding-left");
        if (!string.IsNullOrEmpty(paddingLeft) &&
            TryParseCssLength(paddingLeft, out float pl))
        {
            style.PaddingLeft = pl;
            style.SetProperties.Add("padding-left");
        }

        var paddingRight = css.GetPropertyValue("padding-right");
        if (!string.IsNullOrEmpty(paddingRight) &&
            TryParseCssLength(paddingRight, out float pr))
        {
            style.PaddingRight = pr;
            style.SetProperties.Add("padding-right");
        }

        var fontFamily = css.GetPropertyValue("font-family");
        if (!string.IsNullOrEmpty(fontFamily))
        {
            // take the first font name, strip quotes
            var firstFont = fontFamily
                .Split(',')[0]
                .Trim()
                .Trim('\'', '"');

            if (!string.IsNullOrEmpty(firstFont))
            {
                Log.WriteLine($"    [Css] <{tagName}> font-family={firstFont}");
                style.FontFamily = firstFont;
                style.SetProperties.Add("font-family");
            }
        }

        var fontWeight = css.GetPropertyValue("font-weight");
        if (!string.IsNullOrEmpty(fontWeight))
        {
            int fw;
            if (fontWeight.Equals("bold", StringComparison.OrdinalIgnoreCase))
                fw = 700;
            else if (fontWeight.Equals("normal", StringComparison.OrdinalIgnoreCase))
                fw = 400;
            else if (int.TryParse(fontWeight, out fw)) { }
            else
                fw = 400;

            style.IsBold = fw >= 600;
            style.SetProperties.Add("font-weight");
            Log.WriteLine($"    [Css] <{tagName}> font-weight={fw} -> IsBold={style.IsBold}");
        }

        var rawLineHeight = css.GetPropertyValue("line-height");
        if (!string.IsNullOrEmpty(rawLineHeight))
        {
            if (rawLineHeight.Equals("normal",
                    StringComparison.OrdinalIgnoreCase))
            {
                style.LineHeight = 0;
                style.LineHeightIsMultiplier = false;
                style.SetProperties.Add("line-height");
            }
            else if (rawLineHeight.EndsWith("px") &&
                     float.TryParse(
                         rawLineHeight.Replace("px", ""),
                         out float lhPx))
            {
                Log.WriteLine($"    [Css] <{tagName}> line-height={lhPx}px");
                style.LineHeight = lhPx;
                style.LineHeightIsMultiplier = false;
                style.SetProperties.Add("line-height");
            }
            else if (rawLineHeight.EndsWith("%") &&
                     float.TryParse(
                         rawLineHeight.TrimEnd('%'),
                         out float lhp))
            {
                Log.WriteLine($"    [Css] <{tagName}> line-height={lhp}%");
                style.LineHeight = lhp / 100f;
                style.LineHeightIsMultiplier = true;
                style.SetProperties.Add("line-height");
            }
            else if (float.TryParse(rawLineHeight, out float lh))
            {
                // unitless multiplier (e.g. line-height: 1.5)
                Log.WriteLine($"    [Css] <{tagName}> line-height={lh} (multiplier)");
                style.LineHeight = lh;
                style.LineHeightIsMultiplier = true;
                style.SetProperties.Add("line-height");
            }
        }

        ParseBorder(style, tagName, css);

        var textAlign = css.GetPropertyValue("text-align");
        if (!string.IsNullOrEmpty(textAlign))
        {
            var resolved = TextAlignType.Left;
            if (textAlign.Equals("center", StringComparison.OrdinalIgnoreCase))
                resolved = TextAlignType.Center;
            else if (textAlign.Equals("right", StringComparison.OrdinalIgnoreCase))
                resolved = TextAlignType.Right;
            Log.WriteLine($"    [Css] <{tagName}> text-align={textAlign} -> resolved={resolved} (before={style.TextAlign})");
            style.TextAlign = resolved;
            style.SetProperties.Add("text-align");
        }

        var colorVal = css.GetPropertyValue("color");
        if (!string.IsNullOrEmpty(colorVal))
        {
            Log.WriteLine($"    [Css] <{tagName}> color={colorVal}");
            style.Color = Css.CssColorParser.Parse(colorVal);
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

        var flexWrap = css.GetPropertyValue("flex-wrap");
        if (!string.IsNullOrEmpty(flexWrap))
        {
            Log.WriteLine($"    [Css] <{tagName}> flex-wrap={flexWrap}");
            style.FlexWrap = flexWrap.Equals("wrap", StringComparison.OrdinalIgnoreCase)
                ? FlexWrapType.Wrap
                : FlexWrapType.NoWrap;
            style.SetProperties.Add("flex-wrap");
        }

        var gap = css.GetPropertyValue("gap");
        if (!string.IsNullOrEmpty(gap) &&
            TryParseCssLength(gap, out float gapVal))
        {
            Log.WriteLine($"    [Css] <{tagName}> gap={gapVal}");
            style.Gap = gapVal;
            style.SetProperties.Add("gap");
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
            TryParseCssLength(cssWidth, out float cw))
        {
            Log.WriteLine($"    [Css] <{tagName}> width={cw}");
            style.Width = cw;
            style.SetProperties.Add("width");
        }

        var cssHeight = css.GetPropertyValue("height");
        if (!string.IsNullOrEmpty(cssHeight) &&
            TryParseCssLength(cssHeight, out float ch))
        {
            Log.WriteLine($"    [Css] <{tagName}> height={ch}");
            style.Height = ch;
            style.SetProperties.Add("height");
        }
    }

    private static void ParseBorder(
        ComputedStyle style,
        string tagName,
        ICssStyleDeclaration css)
    {
        // border shorthand (all sides)
        var border = css.GetPropertyValue("border");
        if (!string.IsNullOrEmpty(border) &&
            TryParseBorderValue(border,
                out var bw, out var bs, out var bc))
        {
            Log.WriteLine($"    [Css] <{tagName}> border={bw} {bs} {bc}");
            ApplyBorderSide(style, "all", bw, bs, bc);
            style.SetProperties.Add("border");
        }

        // individual side shorthands
        foreach (var side in new[] {
            "top", "bottom", "left", "right" })
        {
            var val = css.GetPropertyValue(
                $"border-{side}");

            if (!string.IsNullOrEmpty(val) &&
                TryParseBorderValue(val,
                    out var w, out var s, out var c))
            {
                Log.WriteLine(
                    $"    [Css] <{tagName}> border-{side}={w} {s} {c}");
                ApplyBorderSide(style, side, w, s, c);
                style.SetProperties.Add($"border-{side}");
            }
        }
    }

    private static bool TryParseBorderValue(
        string value,
        out float width,
        out Layout.BorderStyle style,
        out Color color)
    {
        width = 0;
        style = Layout.BorderStyle.None;
        color = Color.Black;

        var tokens = SplitBorderTokens(value);

        foreach (var token in tokens)
        {
            if (TryParseCssLength(token, out float w))
            {
                width = w;
            }
            else
            {
                var lower = token.ToLowerInvariant();
                if (lower is "solid" or "dashed" or "dotted")
                {
                    style = lower switch
                    {
                        "solid" => Layout.BorderStyle.Solid,
                        "dashed" => Layout.BorderStyle.Dashed,
                        "dotted" => Layout.BorderStyle.Dotted,
                        _ => Layout.BorderStyle.None
                    };
                }
                else if (lower != "none")
                {
                    color = Css.CssColorParser.Parse(token);
                }
            }
        }

        return true;
    }

    private static List<string> SplitBorderTokens(string value)
    {
        var tokens = new List<string>();
        int depth = 0;
        int start = -1;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];

            if (c == '(')
                depth++;
            else if (c == ')')
                depth--;

            if (c == ' ' && depth == 0)
            {
                if (start >= 0)
                {
                    tokens.Add(value[start..i]);
                    start = -1;
                }
            }
            else if (start < 0)
            {
                start = i;
            }
        }

        if (start >= 0)
            tokens.Add(value[start..]);

        return tokens;
    }

    private static void ApplyBorderSide(
        ComputedStyle style,
        string side,
        float width,
        Layout.BorderStyle borderStyle,
        Color color)
    {
        var apply = (ref BorderSide s) =>
        {
            s.Width = width;
            s.Style = borderStyle;
            s.Color = color;
        };

        if (side == "all")
        {
            apply(ref style.BorderTop);
            apply(ref style.BorderBottom);
            apply(ref style.BorderLeft);
            apply(ref style.BorderRight);
        }
        else if (side == "top")
            apply(ref style.BorderTop);
        else if (side == "bottom")
            apply(ref style.BorderBottom);
        else if (side == "left")
            apply(ref style.BorderLeft);
        else if (side == "right")
            apply(ref style.BorderRight);
    }

    private static bool TryParseCssLength(string value, out float result, float referencePx = 16f)
    {
        result = 0;
        if (string.IsNullOrEmpty(value))
            return false;
        value = value.Trim();

        if (value.EndsWith("px") && float.TryParse(value.AsSpan(0, value.Length - 2), out float px))
        { result = px; return true; }
        if (value.EndsWith("rem") && float.TryParse(value.AsSpan(0, value.Length - 3), out float rem))
        { result = rem * referencePx; return true; }
        if (value.EndsWith("em") && float.TryParse(value.AsSpan(0, value.Length - 2), out float em))
        { result = em * referencePx; return true; }
        if (value.EndsWith("pt") && float.TryParse(value.AsSpan(0, value.Length - 2), out float pt))
        { result = pt * 96f / 72f; return true; }

        if (float.TryParse(value, out float unitless))
        { result = unitless; return true; }

        return false;
    }
}
