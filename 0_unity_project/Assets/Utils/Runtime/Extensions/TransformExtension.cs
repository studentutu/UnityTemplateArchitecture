using System.Runtime.CompilerServices;
using App.Core.Extensions;
using UnityEngine;

[System.Serializable]
public struct TransformLocalStruct
{
    public Transform parent;
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
    public Vector3 localScale;
    public bool active;
}

[System.Serializable]
public struct TransformGlobalStruct
{
    public Vector3 Position;
    public Vector3 EulerAngles;
    public Vector3 Scale;
    public bool active;
}

public static class TransformExtension
{
    private static Transform helperObject = null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T FindAncestorOfType<T>(this Transform t, int deep = 100) where T : Component
    {
        Transform temporal = t;
        T comp = temporal.GetComponent<T>();
        while (comp == null && temporal.parent != null && deep > 0)
        {
            temporal = temporal.parent;
            comp = temporal.GetComponent<T>();
            deep--;
        }

        return comp;
    }

    public static void InverseLookAt(this Transform current, Transform awayFrom)
    {
        current.rotation = Quaternion.LookRotation(current.position - awayFrom.position);
    }

    /// <summary>
    ///  Global scale is a lossy scale!
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetGlobalScale(this Transform transform, Vector3 globalScale)
    {
        if (helperObject == null)
        {
            helperObject = new GameObject(nameof(TransformExtension) + "Helper Transform").transform;
            helperObject.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        helperObject.localScale = globalScale;
        helperObject.parent = transform.parent;
        transform.localScale = helperObject.localScale;
        helperObject.parent = null;
    }

    public static Transform AddChild(this Transform transform, string name = "GameObject")
    {
        Transform child = new GameObject(name).transform;
        child.parent = transform;
        child.localEulerAngles = Vector3.zero;
        child.localPosition = Vector3.zero;
        child.localScale = new Vector3(1, 1, 1);
        child.position = new Vector3(0, 0, 0);
        return child;
    }

    public static Transform AddChild(this Transform transform, GameObject origin, string name = "GameObject")
    {
        Transform child = GameObject.Instantiate(origin, transform).transform;
        child.name = name;
        return child;
    }

    public static void RemoveAllChildren(this Transform transform)
    {
        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
            {
                GameObject.Destroy(child.gameObject);
            }
            else
            {
                GameObject.DestroyImmediate(child.gameObject);
            }
        }
    }

    public static TransformLocalStruct ToLocalStruct(this Transform transform)
    {
        return new TransformLocalStruct
        {
            localPosition = transform.localPosition,
            localEulerAngles = transform.localEulerAngles,
            localScale = transform.localScale,
            active = transform.gameObject.activeSelf,
            parent = transform.parent
        };
    }

    public static TransformGlobalStruct ToGlobalStruct(this Transform transform)
    {
        return new TransformGlobalStruct
        {
            Position = transform.localPosition,
            EulerAngles = transform.localEulerAngles,
            Scale = transform.localScale,
            active = transform.gameObject.activeSelf,
        };
    }

    public static void FromGlobalStruct(this Transform transform, TransformGlobalStruct data, bool ignoreActive = true)
    {
        // transform from Local to World
        transform.position = data.Position;
        transform.eulerAngles = data.EulerAngles;
        transform.SetGlobalScale(data.Scale);
        if (!ignoreActive)
        {
            transform.gameObject.SetActive(data.active);
        }
    }

    /// <summary> Convert struct to local position for transform </summary>
    public static void FromLocalStructToLocal(this Transform transform, TransformLocalStruct data,
        bool ignoreActive = true)
    {
        // transform from Local to World
        transform.localPosition = data.localPosition;
        transform.localEulerAngles = data.localEulerAngles;
        transform.localScale = data.localScale;
        if (!ignoreActive)
        {
            transform.gameObject.SetActive(data.active);
        }
    }

    public static void FromLocalStructToWorld(this Transform transform, TransformLocalStruct data,
        bool ignoreActive = true)
    {
        // transform from Local to World + parent Position!
        transform.position = (data.parent.localToWorldMatrix * data.localPosition);
        transform.position += data.parent.position;
        transform.localEulerAngles = data.localEulerAngles;
        transform.localScale = data.localScale;
        if (!ignoreActive)
        {
            transform.gameObject.SetActive(data.active);
        }
    }

    public static string WholePath(this Transform current)
    {
        string name = current.name;
        Transform parent = current.transform.parent;
        while (parent != null)
        {
            name = parent.name + "/" + name;
            parent = parent.parent;
        }
        return name;
    }

    public static Vector3 GetGlobalScale(this Transform tr)
    {
        return tr.lossyScale;
    }

    public static void SetTransform(this Transform current, Transform otherTransform, bool applyScale)
    {
        current.SetPositionAndRotation(otherTransform.position, otherTransform.rotation);
        if (applyScale)
        {
            current.SetGlobalScale(otherTransform.GetGlobalScale());
        }
    }

    public static Vector3 Down(this Transform self)
    {
        return Quaternion.Euler(180, 0, 0) * self.up;
    }

    public static Vector3 Left(this Transform self)
    {
        return Quaternion.Euler(0, 180, 0) * self.right;
    }

    public static Vector3 Backward(this Transform self)
    {
        return Quaternion.Euler(0, 0, 180) * self.forward;
    }
}