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

    private int _nextId = 1;
    private readonly Dictionary<int, System.Threading.Timer> _timers = new();

    public int setTimeout(
        Delegate callback,
        int delay = 0,
        params object?[] args)
    {
        var id = _nextId++;

        if (delay < 0)
            delay = 0;

        if (delay > 2147483647)
            delay = 0;

        var ctx = SynchronizationContext.Current;
        var invokeArgs = args ?? Array.Empty<object?>();

        var timer = new System.Threading.Timer(_ =>
        {
            ctx?.Post(__ =>
            {
                try
                {
                    callback.DynamicInvoke(invokeArgs);
                }
                catch (Exception ex)
                {
                    Log.WriteLine(
                        $"[setTimeout] error: {ex.Message}");
                }
                finally
                {
                    lock (_timers)
                    {
                        _timers.Remove(id);
                    }
                }
            }, null);
        }, null, delay, System.Threading.Timeout.Infinite);

        lock (_timers)
        {
            _timers[id] = timer;
        }

        return id;
    }

    public void clearTimeout(int id)
    {
        lock (_timers)
        {
            if (_timers.TryGetValue(id,
                    out var timer))
            {
                timer.Dispose();
                _timers.Remove(id);
            }
        }
    }
}
