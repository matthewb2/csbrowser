namespace CSBrowser;

public abstract class RefCounted : IDisposable
{
    private int _refCount = 1;
    private bool _disposed;

    public void Ref()
    {
        Interlocked.Increment(ref _refCount);
    }

    public void Unref()
    {
        if (Interlocked.Decrement(ref _refCount) == 0)
        {
            Dispose(true);
        }
    }

    public void Dispose()
    {
        Unref();
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
        if (disposing)
        {
            Cleanup();
        }
    }

    protected abstract void Cleanup();
}
