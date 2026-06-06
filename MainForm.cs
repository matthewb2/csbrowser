using CSBrowser.Css;
using CSBrowser.Html;

namespace CSBrowser;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        Load += MainForm_Load;
    }

    private async void MainForm_Load(
        object? sender,
        EventArgs e)
    {
        var browser = new BrowserControl();
        browser.Dock = DockStyle.Fill;
        Controls.Add(browser);

        string html =
        """
        <html>
        <body>

        <div id="container"
             style="display:flex; flex-direction:row;">
            <div style="font-size:20px; margin:0px; color:red; background-color:#777799">
                One
            </div>
            <div style="font-size:20px; margin:0px; color:green;background-color:#eee">
                Two
            </div>
            <div style="font-size:20px; margin:0px; color:blue; background-color:#777799">
                Three
            </div>
        </div>

        </body>
        </html>
        """;

        string css =
        """
        h1 {
            font-size:32px;
            margin:20px;
            color:red;
        }
        p {
            font-size:16px;
            margin:10px;
            color:blue;
        }
        """;

        var loader = new HtmlLoader();
        var doc = await loader.LoadAsync(html);

        var cssLoader = new CssLoader();
        var sheet = cssLoader.Parse(css);

        var resolver = new StyleResolver(sheet);
        resolver.Apply(doc);

        browser.LoadDocument(doc);
    }
}
