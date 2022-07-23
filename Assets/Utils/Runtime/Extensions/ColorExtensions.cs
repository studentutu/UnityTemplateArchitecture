using System;
using UnityEngine;

public static class ColorExtensions
{
    [Serializable]
    public struct ColorSerialized
    {
        public float red;
        public float green;
        public float blue;
        public float alpha;
        
        public ColorSerialized(float fromRed, float fromGreen, float fromBlue, float fromAlpha)
        {
            red = fromRed;
            green = fromGreen;
            blue = fromBlue;
            alpha = fromAlpha;
        }
        
        public static implicit operator Color(ColorSerialized d)
        {
            return new Color(d.red,d.green, d.blue, d.alpha);
        }
    }
    
    [Serializable]
    public struct Color32Serialized
    {
        public byte red;
        public byte green;
        public byte blue;
        public byte alpha;
        
        public Color32Serialized(byte fromRed, byte fromGreen, byte fromBlue, byte fromAlpha)
        {
            red = fromRed;
            green = fromGreen;
            blue = fromBlue;
            alpha = fromAlpha;
        }
        
        public static implicit operator Color32(Color32Serialized d)
        {
            return new Color32(d.red,d.green, d.blue, d.alpha);
        }
    }

    public static ColorSerialized Serialized(this Color c)
    {
        return new ColorSerialized(c.r, c.g, c.b, c.a);
    }
    
    public static Color32Serialized Serialized(this Color32 c)
    {
        return new Color32Serialized(c.r, c.g, c.b, c.a);
    }

    public static Color SetRed(this Color color, float red) => new Color(red, color.g, color.b, color.a);
    public static Color SetGreen(this Color color, float green) => new Color(color.r, green, color.b, color.a);
    public static Color SetBlue(this Color color, float blue) => new Color(color.r, color.g, blue, color.a);
    public static Color SetAlpha(this Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    
    public static Color32 SetRed(this Color32 color, byte red) => new Color32(red, color.g, color.b, color.a);
    public static Color32 SetGreen(this Color32 color, byte green) => new Color32(color.r, green, color.b, color.a);
    public static Color32 SetBlue(this Color32 color, byte blue) => new Color32(color.r, color.g, blue, color.a);
    public static Color32 SetAlpha(this Color32 color, byte alpha) => new Color32(color.r, color.g, color.b, alpha);
    
     /// <summary>
        /// Attempts to make a color struct from the html color string.
        /// If parsing is failed magenta color will be returned.
        ///
        /// Strings that begin with '#' will be parsed as hexadecimal in the following way:
        /// #RGB (becomes RRGGBB)
        /// #RRGGBB
        /// #RGBA (becomes RRGGBBAA)
        /// #RRGGBBAA
        ///
        /// When not specified alpha will default to FF.
        ///     Strings that do not begin with '#' will be parsed as literal colors, with the following supported:
        /// red, cyan, blue, darkblue, lightblue, purple, yellow, lime, fuchsia, white, silver, grey, black, orange, brown, maroon, green, olive, navy, teal, aqua, magenta..
        /// </summary>
        /// <param name="htmlString">Case insensitive html string to be converted into a color.</param>
        /// <returns>The converted color.</returns>
        public static Color MakeColorFromHtml(string htmlString)
        {
            return MakeColorFromHtml(htmlString, Color.magenta);
        }

        /// <summary>
        /// Attempts to make a color struct from the html color string.
        /// If parsing is failed <see cref="fallbackColor"/> color will be returned.
        ///
        /// Strings that begin with '#' will be parsed as hexadecimal in the following way:
        /// #RGB (becomes RRGGBB)
        /// #RRGGBB
        /// #RGBA (becomes RRGGBBAA)
        /// #RRGGBBAA
        ///
        /// When not specified alpha will default to FF.
        ///     Strings that do not begin with '#' will be parsed as literal colors, with the following supported:
        /// red, cyan, blue, darkblue, lightblue, purple, yellow, lime, fuchsia, white, silver, grey, black, orange, brown, maroon, green, olive, navy, teal, aqua, magenta..
        /// </summary>
        /// <param name="htmlString">Case insensitive html string to be converted into a color.</param>
        /// <param name="fallbackColor">Color to fall back to in case the parsing is failed.</param>
        /// <returns>The converted color.</returns>
        public static Color MakeColorFromHtml(string htmlString, Color fallbackColor)
        {
            return ColorUtility.TryParseHtmlString(htmlString, out var color) ? color : fallbackColor;
        }


        public static string ColorToHTMLString(this Color color)
        {
            return ColorUtility.ToHtmlStringRGBA(color);
        }
}