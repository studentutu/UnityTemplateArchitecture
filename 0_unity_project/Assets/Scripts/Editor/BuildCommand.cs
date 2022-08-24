#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Core.Editor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Scripting;

/// <summary>
/// DO NOT MOVE FROM CURRENT DIRECTORY. Required for Gitlab CI Pipeline.
/// All Addressables assets (bin) should be build before the push.
/// </summary>
static class BuildCommand
{
    private const string BUILD_OPTIONS_ENV_VAR = "BuildOptions";

    /// <summary>
    /// In bash CI all custom environments should be used in CAPS LOCK
    /// </summary>
    private const string ANDROID_BUNDLE_VERSION_CODE = "BUNDLE_VERSION_CODE";

    private const string ANDROID_APP_BUNDLE = "BUILD_APP_BUNDLE";
    private const string SCRIPTING_BACKEND_ENV_VAR = "SCRIPTING_BACKEND";
    private const string BUILD_ADDRESSABLES = "BUILD_ADDRESSABLES";
    private const string BACK_END_BASE_URI = "BACK_END_BASE_URI";
    private const string BACK_END_AUTH_URI = "BACK_END_AUTH_URI";
    private const string BACK_END_AUTH_AUDIENCE_URI = "BACK_END_AUTH_AUDIENCE_URI";
    private const string BACK_END_AUTH_CLIENT_ID = "BACK_END_AUTH_CLIENT_ID";
    private const string BACK_END_AUTH_CLIENT_SECRET = "BACK_END_AUTH_CLIENT_SECRET";
    private const string DB_CONNECTION_TO_BACK_END = "DB_CONNECTION_TO_BACK_END";
    private const string BACK_END_AUTH_GRANT_TYPE = "BACK_END_AUTH_GRANT_TYPE";


    static string GetArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Contains(name))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    static string[] GetEnabledScenes()
    {
        return (
            from scene in EditorBuildSettings.scenes
            where scene.enabled
            where !string.IsNullOrEmpty(scene.path)
            select scene.path
        ).ToArray();
    }

    static BuildTarget GetBuildTarget()
    {
        string buildTargetName = GetArgument("customBuildTarget");
        Console.WriteLine(":: Received customBuildTarget " + buildTargetName);

        if (buildTargetName.ToLower() == "android")
        {
#if !UNITY_5_6_OR_NEWER
			// https://issuetracker.unity3d.com/issues/buildoptions-dot-acceptexternalmodificationstoplayer-causes-unityexception-unknown-project-type-0
			// Fixed in Unity 5.6.0
			// side effect to fix android build system:
			EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Internal;
#endif
        }

        if (buildTargetName.TryConvertToEnum(out BuildTarget target))
            return target;

        Console.WriteLine(
            $":: {nameof(buildTargetName)} \"{buildTargetName}\" not defined on enum {nameof(BuildTarget)}, using {nameof(BuildTarget.NoTarget)} enum to build");

        return BuildTarget.NoTarget;
    }

    public static string GetBuildPath()
    {
        string buildPath = GetArgument("customBuildPath");
        Console.WriteLine(":: Received customBuildPath " + buildPath);
        if (buildPath == "")
        {
            throw new Exception("customBuildPath argument is missing");
        }

        return buildPath;
    }

    public static string GetBuildName(BuildTarget target)
    {
        string buildName = GetArgument("customBuildName");
        Console.WriteLine(":: Received customBuildName " + buildName);
        if (buildName == "")
        {
            throw new Exception("customBuildName argument is missing");
        }

        if (target == BuildTarget.Android)
        {
            buildName = buildName + "_" + GetAndroidCurrentVersion();
        }

        return buildName;
    }

    private static string GetAndroidBundleVersion()
    {
        var findObject = ((AssetDatabase) null).FindAssetsOfType<CustomScriptableBuildSettings>();

        foreach (var item in findObject)
        {
            PlayerSettings.Android.bundleVersionCode = item.AndroidBuildNumber;
            string[] lines = PlayerSettings.bundleVersion.Split('.');
            int MajorVersion = int.Parse(lines[0]);
            int MinorVersion = int.Parse(lines[1]);
            int Build = item.AndroidBuildNumber;

            PlayerSettings.bundleVersion = MajorVersion.ToString("0") + "." +
                                           MinorVersion.ToString("0") + "." +
                                           Build.ToString("0");
            AssetDatabase.SaveAssets();
            return item.AndroidBuildNumber.ToString();
        }

        return PlayerSettings.Android.bundleVersionCode.ToString();
    }

    private static string GetAndroidCurrentVersion()
    {
        string any = GetAndroidBundleVersion();
        string buildNumber =
            string.Format($"{UnityEngine.Application.version}");
        return buildNumber;
    }

    public static string GetFixedBuildPath(BuildTarget buildTarget, string buildPath, string buildName)
    {
        if (buildTarget.ToString().ToLower().Contains("windows"))
        {
            buildName += ".exe";
        }
        else if (buildTarget == BuildTarget.Android)
        {
#if UNITY_2018_3_OR_NEWER
            buildName += EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk";
#else
            buildName += ".apk";
#endif
        }

        return buildPath + buildName;
    }

    public static BuildOptions GetBuildOptions()
    {
        if (TryGetEnv(BUILD_OPTIONS_ENV_VAR, out string envVar))
        {
            string[] allOptionVars = envVar.Split(',');
            BuildOptions allOptions = BuildOptions.None;
            BuildOptions option;
            string optionVar;
            int length = allOptionVars.Length;

            Console.WriteLine($":: Detecting {BUILD_OPTIONS_ENV_VAR} env var with {length} elements ({envVar})");

            for (int i = 0; i < length; i++)
            {
                optionVar = allOptionVars[i];

                if (optionVar.TryConvertToEnum(out option))
                {
                    allOptions |= option;
                }
                else
                {
                    Console.WriteLine($":: Cannot convert {optionVar} to {nameof(BuildOptions)} enum, skipping it.");
                }
            }

            return allOptions;
        }

        return BuildOptions.AllowDebugging | BuildOptions.StrictMode;
    }

    // https://stackoverflow.com/questions/1082532/how-to-tryparse-for-enum-value
    static bool TryConvertToEnum<TEnum>(this string strEnumValue, out TEnum value)
    {
        if (!Enum.IsDefined(typeof(TEnum), strEnumValue))
        {
            value = default;
            return false;
        }

        value = (TEnum) Enum.Parse(typeof(TEnum), strEnumValue);
        return true;
    }

    static bool TryGetEnv(string key, out string value)
    {
        value = Environment.GetEnvironmentVariable(key);
        return !string.IsNullOrEmpty(value);
    }

    static void SetScriptingBackendFromEnv(BuildTarget platform)
    {
        var targetGroup = BuildPipeline.GetBuildTargetGroup(platform);
        if (TryGetEnv(SCRIPTING_BACKEND_ENV_VAR, out string scriptingBackend))
        {
            if (scriptingBackend.TryConvertToEnum(out ScriptingImplementation backend))
            {
                Console.WriteLine($":: Setting ScriptingBackend to {backend}");
                PlayerSettings.SetScriptingBackend(targetGroup, backend);
                if (targetGroup == BuildTargetGroup.Android)
                {
                    if (backend == ScriptingImplementation.Mono2x)
                    {
                        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
                    }

                    if (backend == ScriptingImplementation.IL2CPP &&
                        PlayerSettings.Android.targetArchitectures == AndroidArchitecture.None)
                    {
                        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                    }
                }
            }
            else
            {
                string possibleValues = string.Join(", ",
                    Enum.GetValues(typeof(ScriptingImplementation)).Cast<ScriptingImplementation>());
                throw new Exception(
                    $"Could not find '{scriptingBackend}' in ScriptingImplementation enum. Possible values are: {possibleValues}");
            }
        }
        else
        {
            var defaultBackend = PlayerSettings.GetDefaultScriptingBackend(targetGroup);
            Console.WriteLine(
                $":: Using project's configured ScriptingBackend (should be {defaultBackend} for tagetGroup {targetGroup}");
        }
    }

    public static BuildTargetGroup ToBuildTargetGroup(BuildTarget tg)
    {
        //Debug.Log(tg.ToString());
        return (BuildTargetGroup) Enum.Parse(typeof(BuildTargetGroup), tg.ToString());
    }

    public static void TryChangeUrlEnvironments()
    {
        const string ENVIRONMENT_FileName = "ENVIRONMENT";
        var findObject = QuickEditor.Core.QuickEditorAssetStaticAPI.FindAssets("t: TextAsset " + ENVIRONMENT_FileName);
        if (findObject == null || findObject.Length == 0)
        {
            Debug.LogError("Can't find TextAsset ENVIRONMENT.json");
            return;
        }

        string finalAssetPath = null;
        for (int i = 0; i < findObject.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(findObject[i]);
            if (assetPath.Contains(ENVIRONMENT_FileName))
            {
                finalAssetPath = assetPath;
                break;
            }
        }

        if (string.IsNullOrEmpty(finalAssetPath))
        {
            Debug.LogError("Can't find TextAsset ENVIRONMENT.json");
            return;
        }

        TextAsset asset = AssetDatabase.LoadAssetAtPath(finalAssetPath, typeof(TextAsset)) as TextAsset;
        if (asset == null)
        {
            Debug.LogError("Can't load TextAsset ENVIRONMENT.json");
            return;
        }

        var asEnviromental = JsonUtility.FromJson<EnvironmentJsonService>(asset.text);
        if (asEnviromental == null || string.IsNullOrEmpty(asEnviromental.base_url_auth) ||
            string.IsNullOrEmpty(asEnviromental.base_url_backend))
        {
            Debug.LogError("Current TextAsset ENVIRONMENT.json is null or has empty fields");
            return;
        }

        bool requiredOverwrite = false;
        if (TryGetEnv(BACK_END_BASE_URI, out var baseBackEndUrlString))
        {
            if (!string.IsNullOrEmpty(baseBackEndUrlString))
            {
                requiredOverwrite = true;
                asEnviromental.base_url_backend = baseBackEndUrlString;
            }
        }

        if (TryGetEnv(BACK_END_AUTH_URI, out var baseBackEndAuthUrlString))
        {
            if (!string.IsNullOrEmpty(baseBackEndAuthUrlString))
            {
                requiredOverwrite = true;
                asEnviromental.base_url_auth = baseBackEndAuthUrlString;
            }
        }

        if (TryGetEnv(BACK_END_AUTH_AUDIENCE_URI, out var baseAudienceUriString))
        {
            if (!string.IsNullOrEmpty(baseAudienceUriString))
            {
                requiredOverwrite = true;
                asEnviromental.audience = baseAudienceUriString;
            }
        }

        if (TryGetEnv(BACK_END_AUTH_CLIENT_ID, out var authClientId))
        {
            if (!string.IsNullOrEmpty(authClientId))
            {
                requiredOverwrite = true;
                asEnviromental.client_id = authClientId;
            }
        }

        if (TryGetEnv(BACK_END_AUTH_CLIENT_SECRET, out var authClientSecret))
        {
            if (!string.IsNullOrEmpty(authClientSecret))
            {
                requiredOverwrite = true;
                asEnviromental.client_secret = authClientSecret;
            }
        }
        
        if (TryGetEnv(DB_CONNECTION_TO_BACK_END, out var dbConnectionString))
        {
            if (!string.IsNullOrEmpty(dbConnectionString))
            {
                requiredOverwrite = true;
                asEnviromental.dbConnection = dbConnectionString;
            }
        }
        
        if (TryGetEnv(BACK_END_AUTH_GRANT_TYPE, out var grantTypeString))
        {
            if (!string.IsNullOrEmpty(grantTypeString))
            {
                requiredOverwrite = true;
                asEnviromental.grant_type = grantTypeString;
            }
        }

        if (requiredOverwrite)
        {
            var stringToWrite = JsonUtility.ToJson(asEnviromental);
            QuickEditor.Core.QuickEditorFileStaticAPI.WriteFile(finalAssetPath, stringToWrite);
            EditorUtility.SetDirty(asset);
            asset = null;
            AssetDatabase.SaveAssets();
            AssetDatabase.RefreshSettings();
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// Main Build Command
    /// </summary>
    /// <exception cref="Exception"></exception>
    [Preserve]
    public static async void PerformBuild()
    {
        Console.WriteLine(":: Performing build");
        AssetDatabase.SaveAssets();
        AssetDatabase.RefreshSettings();
        AssetDatabase.Refresh();

        var requiredTime = 5000; // 5 seconds
        var timer = 3 * 60 * 1000; // 3 minutes
        while (requiredTime > 0 ||
               EditorApplication.isCompiling ||
               EditorApplication.isUpdating ||
               AssetDatabase.IsAssetImportWorkerProcess())
        {
            requiredTime -= 1000;
            if (timer <= 0)
            {
                break;
            }

            timer -= 1000;
            await Task.Delay(1000).ConfigureAwait(true);
        }

        TryChangeUrlEnvironments();
        requiredTime = 5000; // 5 seconds
        timer = 3 * 60 * 1000; // 3 minutes
        while (requiredTime > 0 ||
               EditorApplication.isCompiling ||
               EditorApplication.isUpdating ||
               AssetDatabase.IsAssetImportWorkerProcess())
        {
            requiredTime -= 1000;
            if (timer <= 0)
            {
                break;
            }

            timer -= 1000;
            await Task.Delay(1000).ConfigureAwait(true);
        }

        var buildTarget = GetBuildTarget();

        if (buildTarget == BuildTarget.Android)
        {
            HandleAndroidAppBundle();
            HandleAndroidKeystore();
        }

        var buildPath = GetBuildPath();
        var buildName = GetBuildName(buildTarget);
        var buildOptions = GetBuildOptions();
        var fixedBuildPath = GetFixedBuildPath(buildTarget, buildPath, buildName);

        SetScriptingBackendFromEnv(buildTarget);

        AssetDatabase.SaveAssets();
        AssetDatabase.RefreshSettings();
        AssetDatabase.Refresh();

        requiredTime = 5000; // 5 seconds
        timer = 3 * 60 * 1000; // 3 minutes
        while (requiredTime > 0 ||
               EditorApplication.isCompiling ||
               EditorApplication.isUpdating ||
               AssetDatabase.IsAssetImportWorkerProcess())
        {
            requiredTime -= 1000;
            if (timer <= 0)
            {
                break;
            }

            timer -= 1000;
            await Task.Delay(1000).ConfigureAwait(true);
        }

        if (TryGetEnv(BUILD_ADDRESSABLES, out string buildAddressables) && buildAddressables.ToUpper().Equals("YES"))
        {
            Console.WriteLine(" BuildAddressablesProcessor.PreExport start");
            // FOR CI: it cleans and rebuilds a full addressables bin - cleaning is too much. Make sure to clean and rebuild bin  after each push
            AddressableAssetSettings.CleanPlayerContent(
                AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
            AssetDatabase.SaveAssets();
            AssetDatabase.RefreshSettings();
            AssetDatabase.Refresh();

            requiredTime = 5000; // 5 seconds
            timer = 3 * 60 * 1000; // 3 minutes
            while (requiredTime > 0 ||
                   EditorApplication.isCompiling ||
                   EditorApplication.isUpdating ||
                   AssetDatabase.IsAssetImportWorkerProcess())
            {
                requiredTime -= 1000;
                if (timer <= 0)
                {
                    break;
                }

                timer -= 1000;
                await Task.Delay(1000).ConfigureAwait(true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.RefreshSettings();
            AssetDatabase.Refresh();
            requiredTime = 5000; // 5 seconds
            timer = 3 * 60 * 1000; // 3 minutes
            while (requiredTime > 0 ||
                   EditorApplication.isCompiling ||
                   EditorApplication.isUpdating ||
                   AssetDatabase.IsAssetImportWorkerProcess())
            {
                requiredTime -= 1000;
                if (timer <= 0)
                {
                    break;
                }

                timer -= 1000;
                await Task.Delay(1000).ConfigureAwait(true);
            }

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception($" Addressables Build ended with error : {result.Error}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.RefreshSettings();
            AssetDatabase.Refresh();
            requiredTime = 5000; // 5 seconds
            timer = 3 * 60 * 1000; // 3 minutes
            while (requiredTime > 0 ||
                   EditorApplication.isCompiling ||
                   EditorApplication.isUpdating ||
                   AssetDatabase.IsAssetImportWorkerProcess())
            {
                requiredTime -= 1000;
                if (timer <= 0)
                {
                    break;
                }

                timer -= 1000;
                await Task.Delay(1000).ConfigureAwait(true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.RefreshSettings();
            AssetDatabase.Refresh();
            requiredTime = 5000; // 5 seconds
            timer = 3 * 60 * 1000; // 3 minutes
            while (requiredTime > 0 ||
                   EditorApplication.isCompiling ||
                   EditorApplication.isUpdating ||
                   AssetDatabase.IsAssetImportWorkerProcess())
            {
                requiredTime -= 1000;
                if (timer <= 0)
                {
                    break;
                }

                timer -= 1000;
                await Task.Delay(1000).ConfigureAwait(true);
            }

            Console.WriteLine(" BuildAddressablesProcessor.PreExport done");
        }
        else
        {
            Console.WriteLine(" Not using BuildAddressablesProcessor.PreExport");
        }

        var buildReport = BuildPipeline.BuildPlayer(GetEnabledScenes(), fixedBuildPath, buildTarget, buildOptions);

        if (buildReport.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new Exception($"Build ended with {buildReport.summary.result} status");
        }

        Console.WriteLine(":: Done with build");
    }

    private static void AddToDefines(string define)
    {
        var getBuildTarget = ToBuildTargetGroup(GetBuildTarget());
        var platformSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(getBuildTarget);
        var listToPreserve = new List<string>();
        foreach (var symbol in platformSymbols.Split(';'))
        {
            if (symbol.Equals(define))
            {
                continue;
            }

            listToPreserve.Add(symbol);
        }

        listToPreserve.Add(define);
        string symbols = String.Join(";", listToPreserve.ToArray());
        PlayerSettings.SetScriptingDefineSymbolsForGroup(getBuildTarget, symbols);
    }

    private static void RemoveFromDefines(string define)
    {
        var getBuildTarget = ToBuildTargetGroup(GetBuildTarget());
        var platformSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(getBuildTarget);
        var listToPreserve = new List<string>();
        foreach (var symbol in platformSymbols.Split(';'))
        {
            if (symbol.Equals(define))
            {
                continue;
            }

            listToPreserve.Add(symbol);
        }

        string symbols = String.Join(";", listToPreserve.ToArray());
        PlayerSettings.SetScriptingDefineSymbolsForGroup(getBuildTarget, symbols);
    }

    private static void HandleAndroidAppBundle()
    {
        if (TryGetEnv(ANDROID_APP_BUNDLE, out string value))
        {
#if UNITY_2018_3_OR_NEWER
            if (bool.TryParse(value, out bool buildAppBundle))
            {
                EditorUserBuildSettings.buildAppBundle = buildAppBundle;
                Console.WriteLine($":: {ANDROID_APP_BUNDLE} env var detected, set buildAppBundle to {value}.");
            }
            else
            {
                Console.WriteLine(
                    $":: {ANDROID_APP_BUNDLE} env var detected but the value \"{value}\" is not a boolean.");
            }
#else
            Console.WriteLine($":: {ANDROID_APP_BUNDLE} env var detected but does not work with lower Unity version than 2018.3");
#endif
        }
    }

    private static void HandleAndroidKeystore()
    {
        AndroidSettings.LoadAndUseKeyStore();
    }
}
#endif