using UnityEngine;

public static class RectTransformExtensions
{
    public enum AnchorPresets
    {
        TopLeft,
        TopCenter,
        TopRight,

        MiddleLeft,
        MiddleCenter,
        MiddleRight,

        BottomLeft,
        BottonCenter,
        BottomRight,

        VertStretchLeft,
        VertStretchRight,
        VertStretchCenter,

        HorStretchTop,
        HorStretchMiddle,
        HorStretchBottom,

        StretchAll
    }

    public enum PivotPresets
    {
        TopLeft,
        TopCenter,
        TopRight,

        MiddleLeft,
        MiddleCenter,
        MiddleRight,

        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    public static void SetAnchor(this RectTransform source, AnchorPresets allign, int offsetX = 0, int offsetY = 0)
    {
        source.anchoredPosition = new Vector3(offsetX, offsetY, 0);

        switch (allign)
        {
            case (AnchorPresets.TopLeft):
            {
                source.anchorMin = new Vector2(0, 1);
                source.anchorMax = new Vector2(0, 1);
                break;
            }
            case (AnchorPresets.TopCenter):
            {
                source.anchorMin = new Vector2(0.5f, 1);
                source.anchorMax = new Vector2(0.5f, 1);
                break;
            }
            case (AnchorPresets.TopRight):
            {
                source.anchorMin = new Vector2(1, 1);
                source.anchorMax = new Vector2(1, 1);
                break;
            }

            case (AnchorPresets.MiddleLeft):
            {
                source.anchorMin = new Vector2(0, 0.5f);
                source.anchorMax = new Vector2(0, 0.5f);
                break;
            }
            case (AnchorPresets.MiddleCenter):
            {
                source.anchorMin = new Vector2(0.5f, 0.5f);
                source.anchorMax = new Vector2(0.5f, 0.5f);
                break;
            }
            case (AnchorPresets.MiddleRight):
            {
                source.anchorMin = new Vector2(1, 0.5f);
                source.anchorMax = new Vector2(1, 0.5f);
                break;
            }

            case (AnchorPresets.BottomLeft):
            {
                source.anchorMin = new Vector2(0, 0);
                source.anchorMax = new Vector2(0, 0);
                break;
            }
            case (AnchorPresets.BottonCenter):
            {
                source.anchorMin = new Vector2(0.5f, 0);
                source.anchorMax = new Vector2(0.5f, 0);
                break;
            }
            case (AnchorPresets.BottomRight):
            {
                source.anchorMin = new Vector2(1, 0);
                source.anchorMax = new Vector2(1, 0);
                break;
            }

            case (AnchorPresets.HorStretchTop):
            {
                source.anchorMin = new Vector2(0, 1);
                source.anchorMax = new Vector2(1, 1);
                break;
            }
            case (AnchorPresets.HorStretchMiddle):
            {
                source.anchorMin = new Vector2(0, 0.5f);
                source.anchorMax = new Vector2(1, 0.5f);
                break;
            }
            case (AnchorPresets.HorStretchBottom):
            {
                source.anchorMin = new Vector2(0, 0);
                source.anchorMax = new Vector2(1, 0);
                break;
            }

            case (AnchorPresets.VertStretchLeft):
            {
                source.anchorMin = new Vector2(0, 0);
                source.anchorMax = new Vector2(0, 1);
                break;
            }
            case (AnchorPresets.VertStretchCenter):
            {
                source.anchorMin = new Vector2(0.5f, 0);
                source.anchorMax = new Vector2(0.5f, 1);
                break;
            }
            case (AnchorPresets.VertStretchRight):
            {
                source.anchorMin = new Vector2(1, 0);
                source.anchorMax = new Vector2(1, 1);
                break;
            }

            case (AnchorPresets.StretchAll):
            {
                source.anchorMin = new Vector2(0, 0);
                source.anchorMax = new Vector2(1, 1);
                break;
            }
        }
    }

    public static void SetPivot(this RectTransform source, PivotPresets preset)
    {
        switch (preset)
        {
            case (PivotPresets.TopLeft):
            {
                source.pivot = new Vector2(0, 1);
                break;
            }
            case (PivotPresets.TopCenter):
            {
                source.pivot = new Vector2(0.5f, 1);
                break;
            }
            case (PivotPresets.TopRight):
            {
                source.pivot = new Vector2(1, 1);
                break;
            }

            case (PivotPresets.MiddleLeft):
            {
                source.pivot = new Vector2(0, 0.5f);
                break;
            }
            case (PivotPresets.MiddleCenter):
            {
                source.pivot = new Vector2(0.5f, 0.5f);
                break;
            }
            case (PivotPresets.MiddleRight):
            {
                source.pivot = new Vector2(1, 0.5f);
                break;
            }

            case (PivotPresets.BottomLeft):
            {
                source.pivot = new Vector2(0, 0);
                break;
            }
            case (PivotPresets.BottomCenter):
            {
                source.pivot = new Vector2(0.5f, 0);
                break;
            }
            case (PivotPresets.BottomRight):
            {
                source.pivot = new Vector2(1, 0);
                break;
            }
        }
    }

    public static void SetDefaultScale(this RectTransform trans)
    {
        trans.localScale = new Vector3(1, 1, 1);
    }

    public static void SetPivotAndAnchors(this RectTransform trans, Vector2 vec)
    {
        trans.pivot = vec;
        trans.anchorMin = vec;
        trans.anchorMax = vec;
    }

    public static Vector2 GetSize(this RectTransform trans)
    {
        return trans.rect.size;
    }

    public static float GetWidth(this RectTransform trans)
    {
        return trans.rect.width;
    }

    public static float GetHeight(this RectTransform trans)
    {
        return trans.rect.height;
    }

    public static void SetPositionOfPivot(this RectTransform trans, Vector2 newPos)
    {
        trans.localPosition = new Vector3(newPos.x, newPos.y, trans.localPosition.z);
    }

    public static void SetLeftBottomPosition(this RectTransform trans, Vector2 newPos)
    {
        trans.localPosition = new Vector3(newPos.x + (trans.pivot.x * trans.rect.width),
            newPos.y + (trans.pivot.y * trans.rect.height), trans.localPosition.z);
    }

    public static void SetLeftTopPosition(this RectTransform trans, Vector2 newPos)
    {
        trans.localPosition = new Vector3(newPos.x + (trans.pivot.x * trans.rect.width),
            newPos.y - ((1f - trans.pivot.y) * trans.rect.height), trans.localPosition.z);
    }

    public static void SetRightBottomPosition(this RectTransform trans, Vector2 newPos)
    {
        trans.localPosition = new Vector3(newPos.x - ((1f - trans.pivot.x) * trans.rect.width),
            newPos.y + (trans.pivot.y * trans.rect.height), trans.localPosition.z);
    }

    public static void SetRightTopPosition(this RectTransform trans, Vector2 newPos)
    {
        trans.localPosition = new Vector3(newPos.x - ((1f - trans.pivot.x) * trans.rect.width),
            newPos.y - ((1f - trans.pivot.y) * trans.rect.height), trans.localPosition.z);
    }

    public static void SetSize(this RectTransform trans, Vector2 newSize)
    {
        Vector2 oldSize = trans.rect.size;
        Vector2 deltaSize = newSize - oldSize;
        trans.offsetMin = trans.offsetMin - new Vector2(deltaSize.x * trans.pivot.x, deltaSize.y * trans.pivot.y);
        trans.offsetMax = trans.offsetMax +
                          new Vector2(deltaSize.x * (1f - trans.pivot.x), deltaSize.y * (1f - trans.pivot.y));
    }

    public static void SetWidth(this RectTransform trans, float newSize)
    {
        SetSize(trans, new Vector2(newSize, trans.rect.size.y));
    }

    public static void SetHeight(this RectTransform trans, float newSize)
    {
        SetSize(trans, new Vector2(trans.rect.size.x, newSize));
    }

    public static Vector3 GetBottomLeftWorldCorner(this RectTransform trans)
    {
        Vector3[] v = new Vector3[4];
        trans.GetWorldCorners(v);
        return v[0];
    }

    public static Vector3 GetTopLeftWorldCorner(this RectTransform trans)
    {
        Vector3[] v = new Vector3[4];
        trans.GetWorldCorners(v);
        return v[1];
    }

    public static Vector3 GetTopRightWorldCorner(this RectTransform trans)
    {
        Vector3[] v = new Vector3[4];
        trans.GetWorldCorners(v);
        return v[2];
    }

    public static Vector3 GetBottomRightWorldCorner(this RectTransform trans)
    {
        Vector3[] v = new Vector3[4];
        trans.GetWorldCorners(v);
        return v[3];
    }

    public static Vector2 GetWorldSize(this RectTransform trans)
    {
        Vector3[] v = new Vector3[4];
        trans.GetWorldCorners(v);
        var loverLeft = v[0];
        var upperLeft = v[1];
        var upperRight = v[2];
        var worldDistanceWidth = (upperLeft - upperRight).magnitude;
        var worldDistanceHeight = (upperLeft - loverLeft).magnitude;
        return new Vector2(worldDistanceWidth, worldDistanceHeight);
    }

    /// <summary>
    /// Assume lover left is (0,0) pixel
    /// </summary>
    /// <param name="rectTransform"></param>
    /// <param name="worldPoint"></param>
    /// <returns></returns>
    public static Vector2 GetPixelFromWorldPoint(this RectTransform rectTransform, Vector3 worldPoint)
    {
        Vector3[] v = new Vector3[4];
        rectTransform.GetWorldCorners(v);
        var loverLeft = v[0];
        var upperLeft = v[1];
        var upperRight = v[2];
        var worldDistanceWidth = (upperLeft - upperRight).magnitude;
        var worldDistanceHeight = (upperLeft - loverLeft).magnitude;
        Vector2 imagePos = Vector2.one * -1;
        var size = rectTransform.GetSize();

        // Assume lover left is (0,0) pixel
        // Project on worldHeight
        var vectorToWorldPoint = loverLeft - worldPoint;
        var upperLeftPosition = upperLeft;
        var projectOnHeight = Vector3.Project(
            vectorToWorldPoint,
            loverLeft - upperLeftPosition
        );
        // Project on worldWidth
        var projectOnWidth = Vector3.Project(
            vectorToWorldPoint,
            upperLeftPosition - upperRight
        );
        // if using sqrt - we will not know if it is inside
        var percentWidth = projectOnWidth.magnitude / worldDistanceWidth;
        var percentHeight = projectOnHeight.magnitude / worldDistanceHeight;

        if (percentWidth >= 0 && percentWidth <= 1)
        {
            imagePos = imagePos.SetX(percentWidth * size.x);
        }

        if (percentHeight >= 0 && percentHeight <= 1)
        {
            imagePos = imagePos.SetY(percentHeight * size.y);
        }

        return imagePos;
    }

    public static bool IsInsideBoundary(this RectTransform current, Bounds other)
    {
        return other.Intersects(current.GetBounds());
    }

    public static Vector3 WorldCenter(this RectTransform current)
    {
        Vector3[] worldCorners = new Vector3[4];
        current.GetWorldCorners(worldCorners);
        var bound = new Bounds(current.position, Vector3.zero);
        for (int i = 1; i < worldCorners.Length; i++)
        {
            bound.Encapsulate(worldCorners[i]);
        }

        return bound.center;
    }

    public static Bounds GetBounds(this RectTransform current)
    {
        Vector3[] worldCorners = new Vector3[4];
        current.GetWorldCorners(worldCorners);
        var bound = new Bounds(current.position, Vector3.zero);
        for (int i = 1; i < worldCorners.Length; i++)
        {
            bound.Encapsulate(worldCorners[i]);
        }

        var bounds = new Bounds(current.WorldCenter(), current.GetWorldSize());
        var plane = new Plane(worldCorners[0], worldCorners[1], worldCorners[2]);
        bounds.Encapsulate(bounds.center + plane.normal * 0.01f);
        bounds.Encapsulate(bounds.center - plane.normal * 0.01f);
        return bounds;
    }
}