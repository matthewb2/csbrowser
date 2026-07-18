using CSBrowser.Css;
using CSBrowser.Html;

namespace CSBrowser;

public partial class MainForm : Form
{
    private BrowserControl _browser = null!;

    public MainForm()
    {
        InitializeComponent();
        Load += MainForm_Load;
    }

    private async void MainForm_Load(
        object? sender,
        EventArgs e)
    {
        _browser = new BrowserControl();
        _browser.Dock = DockStyle.Fill;
        Controls.Add(_browser);

        var menu = new MenuStrip();
        var fileMenu = new ToolStripMenuItem("&File");
        var openItem = new ToolStripMenuItem("&Open...");
        openItem.ShortcutKeys = Keys.Control | Keys.O;
        openItem.Click += OnOpenFile;
        fileMenu.DropDownItems.Add(openItem);
        menu.Items.Add(fileMenu);
        Controls.Add(menu);
        MainMenuStrip = menu;

        string html =
        """
        <html>
        <body>

        <div id="container"
             style="display:flex; flex-direction:row;">
            <div id="_one" style="font-size:20px; margin:0px; color:red; background-color:#777799">
                One
            </div>
            <div id="_two" style="font-size:20px; margin:0px; color:green;background-color:#eee">
                Two
            </div>
            <div onmousemove="getPos(event)" id="_three" style="font-size:20px; margin:0px; color:blue; background-color:#777799">
                Three
            </div>
        </div>

        <script>
            var el = document.getElementById('_one'); 
            el.style.color = 'yellow';
            //alert("이것은 테스트입니다");
            var topic = "mouse position";
            console.log(`Fetched data from ${topic}`);
            var el2 = document.querySelectorAll('div');
            function getPos(e){
        	    x=e.clientX;
        	    y=e.clientY;
        	    cursor="Your Mouse Position Is : " + x + " and " + y ;
        	    console.log(cursor);
            }
        </script>

        </body>
        </html>
        """;

        string css =
        """
        body {
            margin: 0;
            padding: 0;
            }

        """;

        await LoadHtml(html, css);
    }

    private async void OnOpenFile(
        object? sender,
        EventArgs e)
    {
        using var dialog = new OpenFileDialog();
        dialog.Filter = "HTML files (*.html;*.htm)|*.html;*.htm|All files (*.*)|*.*";
        dialog.Title = "Open HTML File";

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var html = await File.ReadAllTextAsync(dialog.FileName);
            await LoadHtml(html, null);
        }
    }

    private async Task LoadHtml(
        string html,
        string? css)
    {
        var loader = new HtmlLoader();
        var doc = await loader.LoadAsync(html);

        if (!string.IsNullOrEmpty(css))
        {
            var cssLoader = new CssLoader();
            var sheet = cssLoader.Parse(css);
            var resolver = new StyleResolver(sheet);
            resolver.Apply(doc);
        }

        _browser.LoadDocument(doc);
        doc.Unref();
    }
}