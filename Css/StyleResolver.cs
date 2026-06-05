using AngleSharp.Css.Dom;
using CSBrowser.Dom;

namespace CSBrowser.Css;

public sealed class StyleResolver
{
    private readonly ICssStyleSheet _sheet;

    public StyleResolver(
        ICssStyleSheet sheet)
    {
        _sheet = sheet;
    }

    public void Apply(
        BrowserElement root)
    {
        ApplyRecursive(root);
    }

    private void ApplyRecursive(
        BrowserElement element)
    {
        foreach (var rule
            in _sheet.Rules)
        {
            if (rule is not ICssStyleRule styleRule)
                continue;

            if (styleRule.SelectorText
                    .Equals(
                        element.TagName,
                        StringComparison.OrdinalIgnoreCase))
            {
                ApplyRule(
                    element,
                    styleRule);
            }
        }

        foreach (var child
            in element.Children)
        {
            ApplyRecursive(child);
        }
    }
    private void ApplyRule(
    BrowserElement element,
    ICssStyleRule rule)
    {
        var style = rule.Style;

        var fontSize =
            style.GetPropertyValue(
                "font-size");

        if (float.TryParse(
            fontSize.Replace("px", ""),
            out float fs))
        {
            element.Style.FontSize = fs;
        }

        var margin =
            style.GetPropertyValue(
                "margin");

        if (float.TryParse(
            margin.Replace("px", ""),
            out float m))
        {
            element.Style.MarginTop = m;
            element.Style.MarginBottom = m;
            element.Style.MarginLeft = m;
            element.Style.MarginRight = m;
        }

        var color =
            style.GetPropertyValue(
                "color");

        if (!string.IsNullOrEmpty(color))
        {
            element.Style.Color =
                CssColorParser.Parse(color);
        }
    }
}