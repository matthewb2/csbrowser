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
        var style =
            rule.Style;

        if (float.TryParse(
            style.GetPropertyValue(
                "font-size")
                .Replace("px", ""),
            out float fontSize))
        {
            element.Style.FontSize =
                fontSize;
        }

        if (float.TryParse(
            style.GetPropertyValue(
                "margin")
                .Replace("px", ""),
            out float margin))
        {
            element.Style.MarginTop =
                margin;

            element.Style.MarginBottom =
                margin;

            element.Style.MarginLeft =
                margin;

            element.Style.MarginRight =
                margin;
        }
    }
}