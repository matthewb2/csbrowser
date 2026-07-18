using System.Text.RegularExpressions;

namespace CSBrowser.JavaScript;

internal static class ScriptPreprocessor
{
    public static string StripHtmlComments(string script)
    {
        // Remove <!-- and --> (HTML comment markers inside <script>)
        // <!-- at line start or beginning of script
        script = Regex.Replace(script, @"^\s*<!--", "", RegexOptions.Multiline);
        // --> at line start
        script = Regex.Replace(script, @"^\s*-->", "", RegexOptions.Multiline);
        // also handle //--> pattern (common legacy pattern)
        script = Regex.Replace(script, @"^\s*//-->", "", RegexOptions.Multiline);

        return script.Trim();
    }
}
