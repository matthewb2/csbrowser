namespace CSBrowser.JavaScript;

public sealed class JsConsole
{
    public void log(string message)
    {
        Log.WriteLine("[console.log] " + message);
    }
}
