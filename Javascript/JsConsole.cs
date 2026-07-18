namespace CSBrowser.JavaScript;

public sealed class JsConsole
{
    public void log(object? message)
    {
        Log.WriteLine(
            "[console.log] " +
            (message?.ToString() ?? "null"));
    }

    public void log(
        object? message,
        params object?[] args)
    {
        var parts = new List<string>();
        parts.Add(message?.ToString() ?? "null");

        foreach (var arg in args)
            parts.Add(arg?.ToString() ?? "null");

        Log.WriteLine(
            "[console.log] " +
            string.Join(" ", parts));
    }
}
