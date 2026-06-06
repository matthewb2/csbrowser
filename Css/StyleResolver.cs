using AngleSharp.Css.Dom;
using CSBrowser.Dom;
using CSBrowser.Layout;

namespace CSBrowser.Css;

public sealed class StyleResolver
{
    private readonly ICssStyleSheet _sheet;

    public StyleResolver(ICssStyleSheet sheet)
    {
        _sheet = sheet;
    }

    public void Apply(BrowserElement root)
    {
        Log.WriteLine("[StyleResolver] Applying stylesheet...");
        ApplyRecursive(root);
    }

    private void ApplyRecursive(BrowserElement element)
    {
        foreach (var rule in _sheet.Rules)
        {
            if (rule is not ICssStyleRule styleRule)
                continue;

            if (styleRule.SelectorText.Equals(
                    element.TagName,
                    StringComparison.OrdinalIgnoreCase))
            {
                ApplyRule(element, styleRule);
            }
        }

        foreach (var child in element.Children)
            ApplyRecursive(child);
    }

    private void ApplyRule(
        BrowserElement element,
        ICssStyleRule rule)
    {
        var style = rule.Style;

        var fontSize = style.GetPropertyValue("font-size");
        if (float.TryParse(fontSize.Replace("px", ""), out float fs))
        {
            Log.WriteLine($"  [Style] <{element.TagName}> font-size={fs}");
            element.Style.FontSize = fs;
        }

        var margin = style.GetPropertyValue("margin");
        if (float.TryParse(margin.Replace("px", ""), out float m))
        {
            Log.WriteLine($"  [Style] <{element.TagName}> margin={m}");
            element.Style.MarginTop = m;
            element.Style.MarginBottom = m;
            element.Style.MarginLeft = m;
            element.Style.MarginRight = m;
        }

        var color = style.GetPropertyValue("color");
        if (!string.IsNullOrEmpty(color))
        {
            Log.WriteLine($"  [Style] <{element.TagName}> color={color}");
            element.Style.Color = CssColorParser.Parse(color);
        }

        var bgColor = style.GetPropertyValue("background-color");
        if (!string.IsNullOrEmpty(bgColor))
        {
            Log.WriteLine($"  [Style] <{element.TagName}> background-color={bgColor}");
            element.Style.BackgroundColor = CssColorParser.Parse(bgColor);
        }

        var display = style.GetPropertyValue("display");
        if (!string.IsNullOrEmpty(display))
        {
            Log.WriteLine($"  [Style] <{element.TagName}> display={display}");
            if (display.Equals("flex", StringComparison.OrdinalIgnoreCase))
                element.Style.Display = DisplayType.Flex;
        }

        var flexDir = style.GetPropertyValue("flex-direction");
        if (string.IsNullOrEmpty(flexDir))
        {
            var cssText = style.CssText;
            var idx = cssText.IndexOf("flex-direction", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var val = cssText.AsSpan(idx + 15);
                var colon = val.IndexOf(':');
                var semi = val.IndexOf(';');
                if (colon >= 0)
                {
                    var end = semi > colon ? semi : val.Length;
                    flexDir = val.Slice(colon + 1, end - colon - 1).Trim().ToString();
                }
            }
        }

        if (!string.IsNullOrEmpty(flexDir))
        {
            Log.WriteLine($"  [Style] <{element.TagName}> flex-direction={flexDir}");
            if (flexDir.Equals("column", StringComparison.OrdinalIgnoreCase))
                element.Style.FlexDirection = Layout.FlexDirection.Column;
        }
    }
}
