using CSBrowser.Dom;

namespace CSBrowser.JavaScript;

public sealed class JsElement
{
    private readonly BrowserElement _element;

    public JsElement(
        BrowserElement element)
    {
        _element = element;
    }

    public string tagName => _element.TagName;

    public string innerText
    {
        get => _element.Text;
        set => _element.Text = value;
    }

    public JsStyle style => new(_element);

    public JsElement[]
        getElementsByTagName(
            string tagName)
    {
        var result =
            new List<JsElement>();

        FindByTagName(
            _element, tagName, result);

        return result.ToArray();
    }

    private static void
        FindByTagName(
            BrowserElement element,
            string tagName,
            List<JsElement> result)
    {
        foreach (var child
            in element.Children)
        {
            if (child.TagName == tagName)
                result.Add(
                    new JsElement(child));

            FindByTagName(
                child, tagName, result);
        }
    }

    public JsElement[]
        querySelectorAll(
            string selectors)
    {
        var doc = new BrowserDocument(_element);
        return doc
            .QuerySelectorAll(selectors)
            .Select(e => new JsElement(e))
            .ToArray();
    }

    public void addEventListener(
        string type,
        Action<JsMouseEvent> callback)
    {
        AddListener(type, callback, false, false, false);
    }

    public void addEventListener(
        string type,
        Action<JsMouseEvent> callback,
        bool useCapture)
    {
        AddListener(type, callback, false, useCapture, false);
    }

    public void addEventListener(
        string type,
        Action<JsMouseEvent> callback,
        Dictionary<string, object?>? options)
    {
        var once = false;
        var capture = false;
        var passive = false;

        if (options != null)
        {
            if (options.TryGetValue("once",
                    out var onceVal)
                && onceVal is bool o)
                once = o;

            if (options.TryGetValue("capture",
                    out var capVal)
                && capVal is bool c)
                capture = c;

            if (options.TryGetValue("passive",
                    out var passVal)
                && passVal is bool p)
                passive = p;
        }

        AddListener(type, callback, once, capture, passive);
    }

    private void AddListener(
        string type,
        Action<JsMouseEvent> callback,
        bool once,
        bool capture,
        bool passive)
    {
        var key = type.ToLowerInvariant();

        if (!_element.EventListeners
                .ContainsKey(key))
        {
            _element.EventListeners[key] =
                new List<EventListenerInfo>();
        }

        _element.EventListeners[key]
            .Add(new EventListenerInfo(
                callback, once, capture,
                passive));
    }

    public void removeEventListener(
        string type,
        Action<JsMouseEvent> callback)
    {
        var key = type.ToLowerInvariant();

        if (_element.EventListeners
                .TryGetValue(key, out var list))
        {
            list.RemoveAll(
                li => ReferenceEquals(
                    li.Callback, callback));
        }
    }

    public Action<JsMouseEvent>? onmousedown
    {
        get
        {
            var key = "mousedown";

            if (_element.EventListeners
                    .TryGetValue(key,
                        out var list)
                && list.Count > 0)
            {
                return list[0].Callback as
                    Action<JsMouseEvent>;
            }

            return null;
        }
        set
        {
            var key = "mousedown";
            _element.EventListeners[key] =
                new List<EventListenerInfo>();

            if (value != null)
                _element.EventListeners[key]
                    .Add(new EventListenerInfo(
                        value, false, false,
                        false));
        }
    }
}
