using System.Diagnostics;

namespace CSBrowser;

internal static class Log
{
    public static void WriteLine(string message)
    {
        Debug.WriteLine(message);
    }
}
