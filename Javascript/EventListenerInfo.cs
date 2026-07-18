namespace CSBrowser.JavaScript;

public sealed class EventListenerInfo
{
    public Delegate Callback { get; }
    public bool Once { get; }
    public bool Capture { get; }
    public bool Passive { get; }

    public EventListenerInfo(
        Delegate callback,
        bool once = false,
        bool capture = false,
        bool passive = false)
    {
        Callback = callback;
        Once = once;
        Capture = capture;
        Passive = passive;
    }
}
