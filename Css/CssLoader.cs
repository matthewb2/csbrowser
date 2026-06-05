using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;

namespace CSBrowser.Css;

public sealed class CssLoader
{
    public ICssStyleSheet Parse(
        string css)
    {
        var parser =
            new CssParser();

        return parser.ParseStyleSheet(css);
    }
}