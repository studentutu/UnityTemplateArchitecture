using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public static class EnumExtensions
{
    
    /// <summary>
    /// Tries to parse string value to a specified enum type.
    /// </summary>
    /// <param name="value">String enum value.</param>
    /// <param name="result">Enum result.</param>
    public static bool TryParseEnum<T>(string value, out T result)
    {
        try
        {
            result = (T)Enum.Parse(typeof(T), value, true);
            return true;
        }
        catch (Exception)
        {
            result = default;
            return false;
        }
    }
    
    public static int GetNumberOfSetFlags(this Enum flagsEnum)
    {
        return new BitArray(new[] { Convert.ToInt32(flagsEnum) }).OfType<bool>().Count(x => x);
    }
    
    public static bool HasSingleFlag(this Enum flagsEnumToCheck, Enum targetFlag)
    {
        bool hasFlag = flagsEnumToCheck.HasFlag(targetFlag);
        if (hasFlag)
        {
            int toCheck = Convert.ToInt32(flagsEnumToCheck);
            int tryFlag = Convert.ToInt32(flagsEnumToCheck);
            // check for only flag
            hasFlag = (toCheck ^ tryFlag) == 0;
        }

        return hasFlag;
    }
    
    public static int GetLayerNumber(LayerMask layerMask)
    {
        return Mathf.FloorToInt(Mathf.Log(layerMask, 2));
    }

    public static void SetLayerMask(GameObject go, LayerMask layerMask)
    {
        int layerNumber = GetLayerNumber(layerMask);
        go.layer = layerNumber;
    }

    public static void SetLayerMaskRecursive(this GameObject go, int layerMask)
    {
        go.layer = layerMask;

        foreach (Transform child in go.transform)
        {
            SetLayerMaskRecursive(child.gameObject, layerMask);
        }
    }
}