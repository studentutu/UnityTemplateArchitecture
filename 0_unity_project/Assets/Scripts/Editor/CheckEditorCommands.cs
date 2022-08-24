using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class CheckEditorCommands
{
    static CheckEditorCommands()
    {
        UnityEditor.Lightmapping.ForceStop();
        if (Application.isBatchMode)
        {
            UnityEditor.AssetDatabase.DisallowAutoRefresh();
            UnityEditor.AssetDatabase.RefreshSettings();
            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.AssetDatabase.SaveAssets();
        }
    }

    [MenuItem("Tools/Check Editor Build Commands")]
    public static void CheckEditorBuildCommands()
    {
        BuildCommand.TryChangeUrlEnvironments();

        var buildTarget = BuildTarget.Android;
        var buildPath = BuildCommand.GetBuildPath();
        var buildName = BuildCommand.GetBuildName(buildTarget);
        Debug.Log("Build Name : " + buildName);
        var buildOptions = BuildCommand.GetBuildOptions();
        var fixedBuildPath = BuildCommand.GetFixedBuildPath(buildTarget, buildPath, buildName);
        Debug.Log("fixedBuildPath  : " + fixedBuildPath);
    }
}