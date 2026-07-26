using CSBrowser.Dom;
using CSBrowser.JavaScript;
using CSBrowser.Layout;
using CSBrowser.Render;
using System.Runtime.InteropServices;

namespace CSBrowser;

public sealed class InputRouter
{
    private readonly BrowserControl _control;
    private readonly BrowserSession _session;

    private BrowserElement? _hoveredElement;
    private DisplayItem? _hoveredDisplayItem;

    private BrowserElement? _focusedElement;
    private int _caretPosition;
    private bool _caretActive;

    #region Win32 Caret P/Invoke

    [DllImport("user32.dll")]
    private static extern bool CreateCaret(IntPtr hWnd, IntPtr hBitmap, int nWidth, int nHeight);

    [DllImport("user32.dll")]
    private static extern bool ShowCaret(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool HideCaret(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetCaretPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern bool DestroyCaret();

    #endregion

    public InputRouter(BrowserControl control, BrowserSession session)
    {
        _control = control;
        _session = session;
    }

    public void HandleMouseDown(MouseEventArgs e)
    {
        if (_session.HitTestTree == null)
            return;

        var scrolled = new Point(e.X - _control.AutoScrollPosition.X, e.Y - _control.AutoScrollPosition.Y);
        Log.WriteLine($"[Mouse] Down at Screen({e.X}, {e.Y}), Scroll({_control.AutoScrollPosition.X}, {_control.AutoScrollPosition.Y}) -> DocPos({scrolled.X}, {scrolled.Y}), Button: {e.Button}");

        var hitItem = _session.HitTestTree.HitTest(scrolled.X, scrolled.Y);
        var hitElement = hitItem?.Element;

        bool isTextInput = hitElement != null &&
            hitElement.TagName == "input" &&
            IsTextInputType(hitElement);

        if (isTextInput)
        {
            if (_focusedElement != hitElement)
            {
                _focusedElement = hitElement;
                _caretPosition = hitElement?.Text?.Length ?? 0;
                _caretActive = true;

                _control.Focus();
                CreateCaret(_control.Handle, IntPtr.Zero, 2, (int)(hitElement!.Style?.FontSize ?? 16));
                ShowCaret(_control.Handle);
                UpdateCaretPos();
            }
        }
        else
        {
            if (_caretActive)
            {
                HideCaret(_control.Handle);
                DestroyCaret();
                _caretActive = false;
            }
            _focusedElement = null;
        }

        DispatchMouseEvent("mousedown", scrolled, e.Button);
    }

    public void HandleMouseMove(MouseEventArgs e)
    {
        var scrolled = new Point(e.X - _control.AutoScrollPosition.X, e.Y - _control.AutoScrollPosition.Y);
        UpdateHoverState(scrolled);
        DispatchMouseEvent("mousemove", scrolled, e.Button);
    }

    public void HandleMouseLeave()
    {
        Log.WriteLine("[Mouse] Leave control area.");
        ClearHoverState();
    }

    public bool HandleKeyDown(KeyEventArgs e)
    {
        if (_focusedElement == null)
            return false;

        if (e.KeyCode == Keys.Back)
        {
            if (_caretPosition > 0)
            {
                var text = _focusedElement.Text ?? "";
                _focusedElement.Text = text.Remove(_caretPosition - 1, 1);
                _caretPosition--;
                _session.Relayout(_control.Width);
                _control.Invalidate();
                UpdateCaretPos();
            }
            e.Handled = true;
            return true;
        }

        if (e.KeyCode == Keys.Delete)
        {
            var text = _focusedElement.Text ?? "";
            if (_caretPosition < text.Length)
            {
                _focusedElement.Text = text.Remove(_caretPosition, 1);
                _session.Relayout(_control.Width);
                _control.Invalidate();
                UpdateCaretPos();
            }
            e.Handled = true;
            return true;
        }

        if (e.KeyCode == Keys.Left)
        {
            if (_caretPosition > 0)
            {
                _caretPosition--;
                UpdateCaretPos();
            }
            e.Handled = true;
            return true;
        }

        if (e.KeyCode == Keys.Right)
        {
            var text = _focusedElement.Text ?? "";
            if (_caretPosition < text.Length)
            {
                _caretPosition++;
                UpdateCaretPos();
            }
            e.Handled = true;
            return true;
        }

        if (e.KeyCode == Keys.Home)
        {
            _caretPosition = 0;
            UpdateCaretPos();
            e.Handled = true;
            return true;
        }

        if (e.KeyCode == Keys.End)
        {
            _caretPosition = (_focusedElement.Text?.Length) ?? 0;
            UpdateCaretPos();
            e.Handled = true;
            return true;
        }

        return false;
    }

    public void HandleKeyPress(KeyPressEventArgs e)
    {
        if (_focusedElement == null)
            return;

        if (e.KeyChar < 32)
            return;

        var text = _focusedElement.Text ?? "";
        _focusedElement.Text = text.Insert(_caretPosition, e.KeyChar.ToString());
        _caretPosition++;
        e.Handled = true;

        _session.Relayout(_control.Width);
        _control.Invalidate();
        UpdateCaretPos();
    }

    public void UpdateCaretPos()
    {
        if (_focusedElement == null || !_caretActive)
            return;

        var bounds = GetFocusedElementBounds();
        if (bounds.IsEmpty)
            return;

        var text = _focusedElement.Text ?? "";
        float caretX = CalculateTextWidth(text.Substring(0, _caretPosition)) + 1;
        float caretY = bounds.Y;

        Point pt = CalculateCaretScreenPos(bounds, caretX, caretY);
        SetCaretPos(pt.X, pt.Y);
    }

    private RectangleF GetFocusedElementBounds()
    {
        if (_session.LayoutRoot == null || _focusedElement == null)
            return RectangleF.Empty;

        var node = FindLayoutNode(_session.LayoutRoot, _focusedElement);
        return node?.Bounds ?? RectangleF.Empty;
    }

    private static LayoutNode? FindLayoutNode(LayoutNode node, BrowserElement target)
    {
        if (node.BrowserElement == target)
            return node;

        foreach (var child in node.Children)
        {
            var found = FindLayoutNode(child, target);
            if (found != null)
                return found;
        }

        return null;
    }

    private float CalculateTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        var fontFamily = _focusedElement?.Style?.FontFamily ?? "Arial";
        var fontSize = _focusedElement?.Style?.FontSize ?? 16;
        var fontStyle = _focusedElement?.Style?.IsBold == true ? FontStyle.Bold : FontStyle.Regular;

        using var bmp = new Bitmap(1, 1);
        using var g = Graphics.FromImage(bmp);
        using var font = new Font(fontFamily, fontSize, fontStyle, GraphicsUnit.Pixel);
        return g.MeasureString(text, font).Width;
    }

    private Point CalculateCaretScreenPos(RectangleF bounds, float caretX, float caretY)
    {
        int clientX = (int)(bounds.X + caretX) + _control.AutoScrollPosition.X;
        int clientY = (int)(bounds.Y + caretY) + _control.AutoScrollPosition.Y;

        if (_focusedElement?.Style?.IsBold == true)
        {
            clientX += 1;
        }

        return new Point(clientX, clientY);
    }

    public void OnGotFocus()
    {
        if (_focusedElement != null && _caretActive)
        {
            CreateCaret(_control.Handle, IntPtr.Zero, 2, (int)(_focusedElement.Style?.FontSize ?? 16));
            ShowCaret(_control.Handle);
            UpdateCaretPos();
        }
    }

    public void OnLostFocus()
    {
        if (_caretActive)
        {
            HideCaret(_control.Handle);
            DestroyCaret();
            _caretActive = false;
        }
    }

    private static bool IsTextInputType(BrowserElement element)
    {
        var type = element.Source?.GetAttribute("type")?.ToLowerInvariant();
        return string.IsNullOrEmpty(type) ||
            type is "text" or "password" or "email" or "number" or "search" or "tel" or "url";
    }

    #region Hover

    private void UpdateHoverState(Point pos)
    {
        if (_session.HitTestTree == null)
            return;

        var foundItem = _session.HitTestTree.HitTest(pos.X, pos.Y);
        BrowserElement? found = foundItem?.Element;

        BrowserElement? foundAncestor = found?.FindAncestorWithPseudoStyle();
        BrowserElement? oldAncestor = _hoveredElement?.FindAncestorWithPseudoStyle();

        if (foundAncestor == oldAncestor)
            return;

        Log.WriteLine($"[Hover] State change detected at DocPos({pos.X}, {pos.Y})");

        if (oldAncestor != null)
        {
            ClearHoverRecursive(oldAncestor);
            _session.RebuildDisplayList();
            _control.Invalidate(TransformToClient(GetPseudoStyledBounds(oldAncestor)));
        }

        _hoveredElement = found;

        if (foundAncestor != null)
        {
            SetHoverRecursive(foundAncestor);
            _session.RebuildDisplayList();
            _control.Invalidate(TransformToClient(GetPseudoStyledBounds(foundAncestor)));
        }

        if (foundItem != _hoveredDisplayItem)
        {
            if (_hoveredDisplayItem != null)
                _control.Invalidate(TransformToClient(_hoveredDisplayItem.Bounds));

            _hoveredDisplayItem = foundItem;

            if (_hoveredDisplayItem != null)
                _control.Invalidate(TransformToClient(_hoveredDisplayItem.Bounds));
        }
    }

    private RectangleF GetPseudoStyledBounds(BrowserElement element)
    {
        return _session.ElementBoundsCache.TryGetValue(element, out var bounds) ? bounds : RectangleF.Empty;
    }

    private Rectangle TransformToClient(RectangleF docBounds)
    {
        int x = (int)(docBounds.X + _control.AutoScrollPosition.X);
        int y = (int)(docBounds.Y + _control.AutoScrollPosition.Y);
        int w = (int)Math.Ceiling(docBounds.Width);
        int h = (int)Math.Ceiling(docBounds.Height);
        return new Rectangle(x, y, w, h);
    }

    private void ClearHoverState()
    {
        if (_hoveredElement != null)
        {
            var ancestor = _hoveredElement.FindAncestorWithPseudoStyle();
            if (ancestor != null)
            {
                ClearHoverRecursive(ancestor);
                _session.RebuildDisplayList();
                _control.Invalidate(TransformToClient(GetPseudoStyledBounds(ancestor)));
            }
            _hoveredElement = null;
        }

        if (_hoveredDisplayItem != null)
        {
            _control.Invalidate(TransformToClient(_hoveredDisplayItem.Bounds));
            _hoveredDisplayItem = null;
        }

        Log.WriteLine("[Hover] Cleared all hover states due to leave.");
    }

    private static void SetHoverRecursive(BrowserElement element)
    {
        element.State = ElementState.Hover;
        foreach (var child in element.Children)
            child.State = ElementState.Hover;
    }

    private static void ClearHoverRecursive(BrowserElement element)
    {
        element.State = ElementState.Normal;
        foreach (var child in element.Children)
            child.State = ElementState.Normal;
    }

    #endregion

    #region JS Event Dispatch

    private void DispatchMouseEvent(string eventType, Point pos, MouseButtons button)
    {
        if (_session.HitTestTree == null)
            return;

        var hitItem = _session.HitTestTree.HitTest(pos.X, pos.Y);
        if (hitItem?.Element == null)
            return;

        var element = hitItem.Element;

        if (!element.EventListeners.TryGetValue(eventType, out var listeners))
            return;

        var screen = _control.PointToScreen(pos);
        Log.WriteLine($"[Event] Dispatched '{eventType}' to Element <{element.TagName}> id={element.Id}");

        var jsEvent = new JsMouseEvent(
            type: eventType,
            clientX: pos.X,
            clientY: pos.Y,
            screenX: screen.X,
            screenY: screen.Y,
            button: button switch
            {
                MouseButtons.Left => 0,
                MouseButtons.Middle => 1,
                MouseButtons.Right => 2,
                _ => 0
            },
            altKey: Control.ModifierKeys.HasFlag(Keys.Alt),
            ctrlKey: Control.ModifierKeys.HasFlag(Keys.Control),
            shiftKey: Control.ModifierKeys.HasFlag(Keys.Shift),
            metaKey: false);

        var toRemove = new List<EventListenerInfo>();

        foreach (var info in listeners)
        {
            if (info.Callback is Action<JsMouseEvent> cb)
            {
                cb(jsEvent);

                if (info.Once)
                    toRemove.Add(info);
            }
        }

        foreach (var info in toRemove)
            listeners.Remove(info);
    }

    #endregion
}
