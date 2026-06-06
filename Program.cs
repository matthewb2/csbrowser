namespace CSBrowser;

static class Program
{
    [STAThread]
    static void Main()
    {
        Log.WriteLine("=== CSBrowser Debug ===");

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
