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
        <head>
        <style>
        body {
            margin: 50px;
            padding: 0;
        }
        </style>
        </head>
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
        <div>
            <li>One</li>
            <li>Two</li>
            <li>Three</li>
        </div>

        <script>
            var el = document.getElementById('_one'); 
            el.style.color = 'yellow';
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

        await LoadHtml(html);
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
            await LoadHtml(html);
        }
    }

    private async Task LoadHtml(string html)
    {
        var loader = new HtmlLoader();
        var doc = await loader.LoadAsync(html);

        _browser.LoadDocument(doc);
        doc.Unref();
    }
}
