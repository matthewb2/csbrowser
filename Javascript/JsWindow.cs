namespace CSBrowser.JavaScript;

public sealed class JsWindow
{
    public void alert(
        string message)
    {
        MessageBox.Show(
            message,
            "CSBrowser");
    }
}
