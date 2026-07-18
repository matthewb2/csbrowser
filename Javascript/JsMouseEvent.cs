namespace CSBrowser.JavaScript;

public sealed class JsMouseEvent
{
    public string type { get; }
    public double clientX { get; }
    public double clientY { get; }
    public double screenX { get; }
    public double screenY { get; }
    public int button { get; }
    public bool altKey { get; }
    public bool ctrlKey { get; }
    public bool shiftKey { get; }
    public bool metaKey { get; }

    public JsMouseEvent(
        string type,
        double clientX,
        double clientY,
        double screenX,
        double screenY,
        int button,
        bool altKey,
        bool ctrlKey,
        bool shiftKey,
        bool metaKey)
    {
        this.type = type;
        this.clientX = clientX;
        this.clientY = clientY;
        this.screenX = screenX;
        this.screenY = screenY;
        this.button = button;
        this.altKey = altKey;
        this.ctrlKey = ctrlKey;
        this.shiftKey = shiftKey;
        this.metaKey = metaKey;
    }
}
