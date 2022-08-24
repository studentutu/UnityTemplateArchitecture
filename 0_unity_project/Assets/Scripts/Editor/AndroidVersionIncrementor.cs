using App.Core.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class AndroidVersionIncrementor
{
    
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        BuildPreProcess.RemoveActions(ActionFactory);
        BuildPreProcess.AddAction(ActionFactory);
    }

    private static BuildPreProcess.BuildPreProcessAction ActionFactory()
    {
        return new BuildPreProcess.BuildPreProcessAction
        {
            CallbackOrder = -100,
            Action = SetFullBundleVersion
        };
    }
    
    private static void SetFullBundleVersion(BuildPlayerOptions buildPlayerOptions)
    {
        string[] lines = PlayerSettings.bundleVersion.Split('.');
        int MajorVersion = int.Parse(lines[0]);
        int MinorVersion = int.Parse(lines[1]);
        int Build =  PlayerSettings.Android.bundleVersionCode;
        
        PlayerSettings.bundleVersion = MajorVersion.ToString("0") + "." +
                                       MinorVersion.ToString("0") + "." +
                                       Build.ToString("0");
    }
    
    
    [PostProcessBuildAttribute(10000)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        Debug.Log("Build v" + PlayerSettings.bundleVersion + " (" + PlayerSettings.Android.bundleVersionCode + ")");
        // IncreaseBuild();
    }

    static void IncrementVersion(int majorIncr, int minorIncr, int buildIncr)
    {
        // string[] lines = PlayerSettings.bundleVersion.Split('.');
        // int MajorVersion = int.Parse(lines[0]) + majorIncr;
        // int MinorVersion = int.Parse(lines[1]) + minorIncr;
        // int Build = int.Parse(lines[2]) + buildIncr;
        //
        // PlayerSettings.bundleVersion = MajorVersion.ToString("0") + "." +
        //                                MinorVersion.ToString("0") + "." +
        //                                Build.ToString("0");
        PlayerSettings.Android.bundleVersionCode = PlayerSettings.Android.bundleVersionCode + buildIncr * 1;
    }

    [MenuItem("Build/Increase Minor Version")]
    private static void IncreaseMinor()
    {
        IncrementVersion(0, 1, 0);
    }

    [MenuItem("Build/Increase Major Version")]
    private static void IncreaseMajor()
    {
        IncrementVersion(1, 0, 0);
    }

    private static void IncreaseBuild()
    {
        IncrementVersion(0, 0, 1);
    }
}