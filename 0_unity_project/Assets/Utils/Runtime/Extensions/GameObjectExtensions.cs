using System;
using UnityEngine;

public static class GameObjectExtensions
{
    public static T GetOrCreateComponent<T>(this GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    public static Tuple<T, GameObject> GetTupleComponentGoInChildren<T>(this Transform transform, bool searchInactive)
    {
        var tuple = GetTupleInternalComponentGoInChildren<T>(transform.gameObject, searchInactive);
        if (tuple.Item1 != null)
        {
            return tuple;
        }

        return null;
    }

    public static Tuple<T, GameObject> GetTupleComponentGoInChildren<T>(this GameObject go, bool searchInactive)
    {
        var tuple = GetTupleInternalComponentGoInChildren<T>(go, searchInactive);
        if (tuple.Item1 != null)
        {
            return tuple;
        }

        return null;
    }

    /// <summary>
    /// Check Tuple with both values!
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="searchInactive"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private static Tuple<T, GameObject> GetTupleInternalComponentGoInChildren<T>(GameObject gameObject,
        bool searchInactive)
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                if (gameObject.transform.GetChild(i).gameObject.activeInHierarchy || searchInactive)
                {
                    var tuple = GetTupleComponentGoInChildren<T>(gameObject.transform.GetChild(i).gameObject,
                        searchInactive);
                    if (tuple != null)
                    {
                        return tuple;
                    }
                }
            }
        }

        return new Tuple<T, GameObject>(component, gameObject);
    }

    /// <summary>
    /// Casting to UnityEngine.Object and Check with null (True Managed to unmanaged null check)
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool IsNull(this UnityEngine.Object obj)
    {
        return obj == null;
    }

    public static void DontDestroyOnLoad(this GameObject obj)
    {
        if (IsRuntimeMode())
        {
            UnityEngine.GameObject.DontDestroyOnLoad(obj);
        }
    }

    public static void DontDestroyOnLoad(this MonoBehaviour obj)
    {
        if (IsRuntimeMode())
        {
            UnityEngine.GameObject.DontDestroyOnLoad(obj);
        }
    }

    private static bool IsRuntimeMode()
    {
        bool result = true;
#if UNITY_EDITOR
        result = Application.isPlaying;
#endif
        return result;
    }
}