using CSBrowser.Dom;

namespace CSBrowser.Css;

internal sealed class CssSelector
{
    public string? TagName { get; init; }
    public string? Id { get; init; }
    public string? ClassName { get; init; }

    public bool Matches(BrowserElement element)
    {
        if (TagName != null && TagName != "*" && element.TagName != TagName)
            return false;

        if (Id != null && element.Id != Id)
            return false;

        if (ClassName != null)
        {
            var classes = element.ClassName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (!Array.Exists(classes, c => c == ClassName))
                return false;
        }

        return true;
    }

    public static List<CssSelector> ParseList(string selectorText)
    {
        var parts = selectorText.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var list = new List<CssSelector>();

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.Length == 0)
                throw new InvalidOperationException(
                    "SyntaxError: empty selector in list");

            list.Add(ParseSingle(trimmed));
        }

        if (list.Count == 0)
            throw new InvalidOperationException(
                "SyntaxError: missing selector");

        return list;
    }

    private static CssSelector ParseSingle(string selector)
    {
        string? tagName = null;
        string? id = null;
        string? className = null;

        int i = 0;

        // Parse tag name (letters, digits, hyphens)
        if (selector[i] is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '*')
        {
            int start = i;
            while (i < selector.Length &&
                   (char.IsLetterOrDigit(selector[i]) ||
                    selector[i] == '-' || selector[i] == '*'))
                i++;

            tagName = selector[start..i].ToLowerInvariant();
        }

        // Parse #id and .class parts
        while (i < selector.Length)
        {
            if (selector[i] == '#')
            {
                i++; // skip #
                int start = i;
                while (i < selector.Length &&
                       (char.IsLetterOrDigit(selector[i]) ||
                        selector[i] == '-' || selector[i] == '_'))
                    i++;

                if (i == start)
                    throw new InvalidOperationException(
                        "SyntaxError: empty id selector");

                id = selector[start..i];
            }
            else if (selector[i] == '.')
            {
                i++; // skip .
                int start = i;
                while (i < selector.Length &&
                       (char.IsLetterOrDigit(selector[i]) ||
                        selector[i] == '-' || selector[i] == '_'))
                    i++;

                if (i == start)
                    throw new InvalidOperationException(
                        "SyntaxError: empty class selector");

                className = selector[start..i];
            }
            else
            {
                throw new InvalidOperationException(
                    $"SyntaxError: unexpected character '{selector[i]}'");
            }
        }

        return new CssSelector
        {
            TagName = tagName,
            Id = id,
            ClassName = className
        };
    }
}
