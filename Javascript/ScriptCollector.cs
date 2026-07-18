using CSBrowser.Dom;

namespace CSBrowser.JavaScript;

public static class ScriptCollector
{
    public static List<string>
        Collect(BrowserElement root)
    {
        var result =
            new List<string>();

        Visit(root, result);

        return result;
    }

    private static void Visit(
        BrowserElement node,
        List<string> scripts)
    {
        if (node.TagName == "script")
        {
            if (!string.IsNullOrWhiteSpace(
                node.ScriptContent))
            {
                var cleaned =
                    ScriptPreprocessor
                        .StripHtmlComments(
                            node.ScriptContent);

                scripts.Add(cleaned);
            }
        }

        foreach (var child
            in node.Children)
        {
            Visit(child, scripts);
        }
    }
}