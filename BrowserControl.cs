using CSBrowser.Dom;
using CSBrowser.Render;

namespace CSBrowser;

public sealed class BrowserControl : UserControl
{
    private readonly BrowserSession _session = new();
    private readonly InputRouter _inputRouter = null!;

    private int _paintCount = 0;

    public BrowserControl()
    {
        AutoScroll = true;
        DoubleBuffered = true;
        _inputRouter = new InputRouter(this, _session);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _session.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Log.WriteLine($"[BrowserControl] OnResize: Control Size = {Width}x{Height}");
        _session.Relayout(Width);
        AutoScrollMinSize = new Size(Width, (int)(_session.LayoutRoot?.Bounds.Height + 20 ?? Height));
        _inputRouter.UpdateCaretPos();
        Invalidate();
    }

    public void LoadDocument(BrowserElement root)
    {
        Log.WriteLine("[BrowserControl] LoadDocument started.");
        _session.LoadDocument(root, Width);
        AutoScrollMinSize = new Size(Width, (int)(_session.LayoutRoot?.Bounds.Height + 20 ?? Height));
        Invalidate();
        Log.WriteLine("[BrowserControl] LoadDocument finished.");
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _inputRouter.HandleMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _inputRouter.HandleMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _inputRouter.HandleMouseLeave();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!_inputRouter.HandleKeyDown(e))
        {
        }
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        _inputRouter.HandleKeyPress(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _inputRouter.OnGotFocus();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        _inputRouter.OnLostFocus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var displayList = _session.DisplayList;
        if (displayList == null || _session.HitTestTree == null)
        {
            Log.WriteLine($"[Paint] Skipped. DisplayList={displayList != null}, HitTestTree={_session.HitTestTree != null}");
            return;
        }

        _paintCount++;

        Log.WriteLine($"[Paint #{_paintCount}] Initial Paint: Rendering all {displayList.Count} items.");

        e.Graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

        var renderer = new GdiRenderer();
        foreach (var item in displayList)
        {
            renderer.RenderItem(e.Graphics, item);
        }
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_VSCROLL = 0x0115;
        const int WM_HSCROLL = 0x0114;
        const int WM_MOUSEWHEEL = 0x020A;

        base.WndProc(ref m);

        if (m.Msg is WM_VSCROLL or WM_HSCROLL or WM_MOUSEWHEEL)
        {
            _inputRouter.UpdateCaretPos();
        }
    }
}
