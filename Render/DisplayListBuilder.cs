using CSBrowser.Layout;

namespace CSBrowser.Render;

public sealed class DisplayListBuilder
{
    public List<DisplayItem>
        Build(LayoutNode root)
    {
        var list =
            new List<DisplayItem>();

        Visit(root, list);

        return list;
    }

    private void Visit(
    LayoutNode node,
    List<DisplayItem> list)
    {
        if (!string.IsNullOrWhiteSpace(
            node.Element.Text))
        {
            list.Add(
     new DisplayItem
     {
         Text =
             node.Element.Text,

         Bounds =
             node.Bounds,

         FontSize =
             node.Style.FontSize,

         Color =
             node.Style.Color
     });
        }

        foreach (var child
            in node.Children)
        {
            Visit(child, list);
        }
    }
}