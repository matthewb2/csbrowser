using System.Drawing;

namespace CSBrowser.Css;

public static class CssColorParser
{
    public static Color Parse(
        string value)
    {
        value = value.Trim()
                     .ToLower();

        return value switch
        {
            "red" => Color.Red,
            "green" => Color.Green,
            "blue" => Color.Blue,
            "black" => Color.Black,
            "white" => Color.White,

            _ => ParseComplex(value)
        };
    }

    private static Color ParseComplex(
        string value)
    {
        if (value.StartsWith("rgb"))
        {
            var nums =
                value.Replace("rgba(", "")
                     .Replace("rgb(", "")
                     .Replace(")", "")
                     .Split(',');

            if (nums.Length < 3)
                return Color.Black;

            if (!int.TryParse(
                nums[0].Trim(),
                out var r))
                return Color.Black;

            if (!int.TryParse(
                nums[1].Trim(),
                out var g))
                return Color.Black;

            if (!int.TryParse(
                nums[2].Trim(),
                out var b))
                return Color.Black;

            return Color.FromArgb(r, g, b);
        }

        try
        {
            return ColorTranslator.FromHtml(value);
        }
        catch
        {
            return Color.Black;
        }
    }
}
