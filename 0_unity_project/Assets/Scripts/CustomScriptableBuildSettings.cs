using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
[CreateAssetMenu(menuName = "Create BuildSettings", fileName = "BuildSettings", order = 0)]
public class CustomScriptableBuildSettings : ScriptableObject
{
    public int AndroidBuildNumber = 160;

    static CustomScriptableBuildSettings()
    {
#if UNITY_EDITOR
        EditorApplication.quitting -= CustomEditorQuitActions;
        EditorApplication.quitting += CustomEditorQuitActions;
#endif
    }

    private static void CustomEditorQuitActions()
    {
        OnEditorQuitUpdateBuildNumber();
    }

    private static void OnEditorQuitUpdateBuildNumber()
    {
#if UNITY_EDITOR
        bool changed = false;
        var findObject = ((AssetDatabase) null).FindAssetsOfType<CustomScriptableBuildSettings>();

        foreach (var item in findObject)
        {
            if (item.AndroidBuildNumber >= PlayerSettings.Android.bundleVersionCode)
            {
                continue;
            }

            changed = true;
            item.AndroidBuildNumber = PlayerSettings.Android.bundleVersionCode;
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssetIfDirty(item);
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif
    }
}