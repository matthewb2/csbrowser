using Jint;

namespace CSBrowser.JavaScript;

public sealed class JsEngine
{
    private readonly Engine _engine;

    public JsEngine()
    {
        _engine = new Engine();
    }

    public void SetGlobal(
        string name,
        object value)
    {
        _engine.SetValue(name, value);
    }

    public void Execute(
        string script)
    {
        _engine.Execute(script);
    }
}
