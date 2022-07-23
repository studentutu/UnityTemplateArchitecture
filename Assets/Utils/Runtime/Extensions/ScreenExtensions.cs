using System;
using UnityEngine;

public static class ScreenExtensions
{
    public static Vector2 GetAspectRatio(Vector2 xy)
    {
        float f = xy.x / xy.y;
        int i = 0;
        while (true)
        {
            i++;
            if (System.Math.Round(f * i, 2) == Mathf.RoundToInt(f * i))
                break;
        }

        return new Vector2((float) System.Math.Round(f * i, 2), i);
    }

    /// <summary>
    /// Modifies the src to fill into the destination relative to the center of the src.
    /// </summary>
    /// <param name="destinationImageSize"></param>
    /// <param name="srcImageSize"></param>
    /// <returns>Scale, Offset</returns>
    public static Tuple<Vector2, Vector2> GetSubSetOfImage(
        Vector2 destinationImageSize, 
        Vector2 srcImageSize)
    {
        var fractionW = (float) srcImageSize.x / destinationImageSize.x;
        var fractionH = (float) srcImageSize.y / destinationImageSize.y;

        var offsetW = (1 - fractionW) / 2;
        var offsetH = (1 - fractionH) / 2;

        Vector2 scale = new Vector2(fractionW, fractionH);
        Vector2 offset = new Vector2(offsetW, offsetH);

        return new Tuple<Vector2, Vector2>(scale, offset);
    }
}