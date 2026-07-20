using System.Drawing;

namespace CSBrowser.Render;

public sealed class QuadtreeNode
{
    private const int MaxElements = 8;

    private readonly RectangleF _bounds;
    private List<DisplayItem>? _items;
    private QuadtreeNode[]? _children;

    public QuadtreeNode(RectangleF bounds)
    {
        _bounds = bounds;
    }

    public void Insert(DisplayItem item)
    {
        if (_children != null)
        {
            foreach (var child in _children)
            {
                if (child._bounds.IntersectsWith(item.Bounds))
                    child.Insert(item);
            }
            return;
        }

        _items ??= new List<DisplayItem>();
        _items.Add(item);

        if (_items.Count > MaxElements && _bounds.Width > 16 && _bounds.Height > 16)
            Split();
    }

    private void Split()
    {
        float midW = _bounds.Width / 2;
        float midH = _bounds.Height / 2;
        float x = _bounds.X;
        float y = _bounds.Y;

        _children = new QuadtreeNode[4]
        {
            new(new RectangleF(x, y, midW, midH)),
            new(new RectangleF(x + midW, y, midW, midH)),
            new(new RectangleF(x, y + midH, midW, midH)),
            new(new RectangleF(x + midW, y + midH, midW, midH)),
        };

        foreach (var item in _items!)
        {
            foreach (var child in _children)
            {
                if (child._bounds.IntersectsWith(item.Bounds))
                    child.Insert(item);
            }
        }

        _items = null;
    }

    public DisplayItem? HitTest(float x, float y)
    {
        if (!_bounds.Contains(x, y))
            return null;

        if (_children != null)
        {
            foreach (var child in _children)
            {
                var hit = child.HitTest(x, y);
                if (hit != null) return hit;
            }
        }
        else if (_items != null)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (_items[i].Bounds.Contains(x, y))
                    return _items[i];
            }
        }

        return null;
    }

    public void QueryRegion(RectangleF region, List<DisplayItem> result)
    {
        if (!_bounds.IntersectsWith(region))
            return;

        if (_children != null)
        {
            foreach (var child in _children)
                child.QueryRegion(region, result);
        }
        else if (_items != null)
        {
            foreach (var item in _items)
            {
                if (item.Bounds.IntersectsWith(region))
                    result.Add(item);
            }
        }
    }
}
