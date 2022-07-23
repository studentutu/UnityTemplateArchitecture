using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

namespace Unity.AutomatedQA
{
    /// <summary>
    ///   <para>Playmode settings for the Automated QA package.</para>
    /// </summary>
    public static class AutomatedQASettings
    {
        internal static class Keys
        {
            internal static readonly string LogLevel = "LogLevel";
            internal static readonly string EnableScreenshots = "EnableScreenshots";
            internal static readonly string PostActionScreenshotDelay = "PostActionScreenshotDelay";
            internal static readonly string RecordingFolderName = "RecordingFolderName";
            internal static readonly string ActivatePlaybackVisualFx = "ActivatePlaybackVisualFx";
            internal static readonly string ActivateClickFeedbackFx = "ActivateClickFeedbackFx";
            internal static readonly string ActivateDragFeedbackFx = "ActivateDragFeedbackFx";
            internal static readonly string ActivateHighlightFeedbackFx = "ActivateHighlightFeedbackFx";
            internal static readonly string UseDynamicWaits = "UseDynamicWaits";
            internal static readonly string DynamicWaitTimeout = "DynamicWaitTimeout";
            internal static readonly string DynamicLoadSceneTimeout = "DynamicLoadSceneTimeout";
            internal static readonly string RecordInputManager = "RecordInputManager";
            internal static readonly string FpsMinimumWarningThreshold = "FpsMinimumWarningThreshold";
            internal static readonly string HeapSizeMbMaximumWarningThreshold = "HeapSizeMbMaximumWarningThreshold";
            internal static readonly string PerformanceWarningsEnabled = "PerformanceWarningsEnabled";
            internal static readonly string EnableCloudTesting = "EnableCloudTesting";
            internal static readonly string ThrowGameObjectInvisibleToCamera = "ThrowGameObjectInvisibleToCamera";

        }

        public static readonly Dictionary<string, string> Tooltips = new Dictionary<string, string>()
        {
            {
                Keys.LogLevel,
                @"Set the maximum level of log messages
                0 = Logging disabled.
                1 = Errors only.
                2 = Errors and Warnings.
                3 = Errors, Warnings, Info.
                4 = Errors, Warnings, Info, Debug."
            },
            { Keys.EnableScreenshots, "Allows screenshots to be recorded during test run. These are used to show screenshots in reports."},
            { Keys.PostActionScreenshotDelay, "delay in seconds after an action to take a screenshot"},
            { Keys.RecordingFolderName, "Name of folder under Assets where we store recording files."},
            { Keys.ActivatePlaybackVisualFx, "Enable or disable visual Fx feedback for actions taken during playback of recordings. If true, check individual feedback booleans to see if a subset will be activated. "},
            { Keys.ActivateClickFeedbackFx,"Activates ripple effect on point of click during playback of recordings."},
            { Keys.ActivateDragFeedbackFx, "Activates drag effect between drag start and drag release during playback of recordings."},
            { Keys.ActivateHighlightFeedbackFx, "Activates highlight effect on point of click during playback of recordings."},
            { Keys.UseDynamicWaits, "Wait for elements to become interactable before trying to interact with them (as opposed to waiting for the original recorded timeDelta period before executing an action)."},
            { Keys.DynamicWaitTimeout, "Period of time in seconds to wait for a target GameObject to be ready while waiting to perform the next action in test playback."},
            { Keys.DynamicLoadSceneTimeout, "Period of time in seconds to wait for scene load while waiting to perform the next action in test playback."},
            { Keys.RecordInputManager, "Record input from the Input Manager (Input class). Playback requires usage replaced with the RecordableInput class."},
            { Keys.FpsMinimumWarningThreshold, "The FPS value designated as the minimum expected framerate before showing a warning."},
            { Keys.HeapSizeMbMaximumWarningThreshold, "The heap size in MB that is the maximum expected usage before showing a warning."},
            { Keys.PerformanceWarningsEnabled, "Activates/deactivates warnings related to performance and max/min thresholds being exceeded."},
            { Keys.EnableCloudTesting, "Enable Cloud Testing features."},
            { Keys.ThrowGameObjectInvisibleToCamera, "Throw errors if gameobject is not visible in camera view."},

        };

        public static readonly string DEVICE_TESTING_API_ENDPOINT = "https://device-testing.prd.gamesimulation.unity3d.com";
        private static AutomatedQASettingsData settings
        {
            get
            {
                if (_settings == null || _settings != default(AutomatedQASettingsData))
                {
                    _settings = GetCustomSettingsData();
                }
                return _settings;
            }
        }
        private static AutomatedQASettingsData _settings;

        static AutomatedQASettings()
        {
            Init();
        }

        private static void Init()
        {
            
#if UNITY_EDITOR
            try
            {
                if (!Directory.Exists(Path.Combine(Application.dataPath, AutomatedQASettingsResourcesPath)))
                {
                    Directory.CreateDirectory(Path.Combine(Application.dataPath, AutomatedQASettingsResourcesPath));
                }
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("get_dataPath is not allowed"))
                {
                    throw e;
                }
            }
#endif

            // Handle required configs. Re-add them if deleted from config settings file.
            List<AutomationSet> setsToReAddToConfigSettingsFile = new List<AutomationSet>();

            RecordingFolderName = InitString(setsToReAddToConfigSettingsFile, Keys.RecordingFolderName, RecordingFolderName);
            ActivatePlaybackVisualFx = InitBool(setsToReAddToConfigSettingsFile, Keys.ActivatePlaybackVisualFx, ActivatePlaybackVisualFx);
            ActivateClickFeedbackFx = InitBool(setsToReAddToConfigSettingsFile, Keys.ActivateClickFeedbackFx, ActivateClickFeedbackFx);
            ActivateDragFeedbackFx = InitBool(setsToReAddToConfigSettingsFile, Keys.ActivateDragFeedbackFx, ActivateDragFeedbackFx);
            ActivateHighlightFeedbackFx = InitBool(setsToReAddToConfigSettingsFile, Keys.ActivateHighlightFeedbackFx, ActivateHighlightFeedbackFx);
            EnableScreenshots = InitBool(setsToReAddToConfigSettingsFile, Keys.EnableScreenshots, EnableScreenshots);
            PostActionScreenshotDelay = InitFloat(setsToReAddToConfigSettingsFile, Keys.PostActionScreenshotDelay, PostActionScreenshotDelay);
            RecordInputManager = InitBool(setsToReAddToConfigSettingsFile, Keys.RecordInputManager, RecordInputManager);
            UseDynamicWaits = InitBool(setsToReAddToConfigSettingsFile, Keys.UseDynamicWaits, UseDynamicWaits);
            DynamicWaitTimeout = InitFloat(setsToReAddToConfigSettingsFile, Keys.DynamicWaitTimeout, DynamicWaitTimeout);
            DynamicLoadSceneTimeout = InitFloat(setsToReAddToConfigSettingsFile, Keys.DynamicLoadSceneTimeout, DynamicLoadSceneTimeout);
            LogLevel = (AQALogger.LogLevel)InitInt(setsToReAddToConfigSettingsFile, Keys.LogLevel, (int)LogLevel);
            FpsMinimumWarningThreshold = InitFloat(setsToReAddToConfigSettingsFile, Keys.FpsMinimumWarningThreshold, FpsMinimumWarningThreshold);
            HeapSizeMbMaximumWarningThreshold = InitFloat(setsToReAddToConfigSettingsFile, Keys.HeapSizeMbMaximumWarningThreshold, HeapSizeMbMaximumWarningThreshold);
            PerformanceWarningsEnabled = InitBool(setsToReAddToConfigSettingsFile, Keys.PerformanceWarningsEnabled, PerformanceWarningsEnabled);
            ThrowGameObjectInvisibleToCamera = InitBool(setsToReAddToConfigSettingsFile, Keys.ThrowGameObjectInvisibleToCamera,ThrowGameObjectInvisibleToCamera);
            // EnableCloudTesting = InitBool(setsToReAddToConfigSettingsFile, Keys.EnableCloudTesting, EnableCloudTesting);

            
#if UNITY_EDITOR
            // Add back any required configs that were deleted by the user.
            if (setsToReAddToConfigSettingsFile.Any())
            {
                AutomatedQASettingsData newConfig = new AutomatedQASettingsData();
                newConfig.Configs.AddRange(setsToReAddToConfigSettingsFile);
                newConfig.Configs.AddRange(settings.Configs);
                File.WriteAllText(Path.Combine(Application.dataPath, AutomatedQASettingsResourcesPath, AutomatedQaSettingsFileName), JsonUtility.ToJson(newConfig));
            }
#endif
        }

        private static AutomationSet FindOrAddAutomationSet(List<AutomationSet> setsToReAddToConfigSettingsFile, string settingKey, object defaultValue)
        {
            AutomationSet set = settings.Configs.Find(c => c.Key == settingKey);
            if (set == null || set == default(AutomationSet))
            {
                set = new AutomationSet(settingKey, defaultValue.ToString());
                setsToReAddToConfigSettingsFile.Add(set);
            }

            return set;
        }

        private static bool InitBool(List<AutomationSet> setsToReAddToConfigSettingsFile, string settingKey, bool defaultValue)
        {
            AutomationSet set = FindOrAddAutomationSet(setsToReAddToConfigSettingsFile, settingKey, defaultValue);
            return bool.Parse(set.Value);
        }

        private static string InitString(List<AutomationSet> setsToReAddToConfigSettingsFile, string settingKey, string defaultValue)
        {
            AutomationSet set = FindOrAddAutomationSet(setsToReAddToConfigSettingsFile, settingKey, defaultValue);
            return set.Value;
        }

        private static float InitFloat(List<AutomationSet> setsToReAddToConfigSettingsFile, string settingKey, float defaultValue)
        {
            AutomationSet set = FindOrAddAutomationSet(setsToReAddToConfigSettingsFile, settingKey, defaultValue);
            return float.Parse(set.Value);
        }

        private static int InitInt(List<AutomationSet> setsToReAddToConfigSettingsFile, string settingKey, int defaultValue)
        {
            AutomationSet set = FindOrAddAutomationSet(setsToReAddToConfigSettingsFile, settingKey, defaultValue);
            int result;
            if (int.TryParse(set.Value, out result))
            {
                return result;
            }

            new AQALogger().LogError($"Failed to parse {set.Value}");
            return defaultValue;
        }

        /// <summary>
        /// Folder on device where we store Automated QA temp and customization data.
        /// </summary>
        public static string PersistentDataPath
        {
            get
            {
                if (_persistentDataPath == null)
                {
                    _persistentDataPath = Path.Combine(Application.persistentDataPath, PackageAssetsFolderName);
                }
                return _persistentDataPath;
            }
            set
            {
                _persistentDataPath = value;
            }
        }
        private static string _persistentDataPath;

        /// <summary>
        /// Name of the Assets data path that our files are stored under.
        /// </summary>
        public static string PackageAssetsFolderPath
        {
            get
            {
                if (string.IsNullOrEmpty(_packageAssetsFolderPath))
                {
                    _packageAssetsFolderPath = Path.Combine(Application.dataPath, PackageAssetsFolderName);
                }
                return _packageAssetsFolderPath;
            }
            set
            {
                _packageAssetsFolderPath = value;
            }
        }
        private static string _packageAssetsFolderPath;

        /// <summary>
        /// Name of the Assets data path that our files are stored under.
        /// </summary>
        public static string PackageAssetsFolderName
        {
            get
            {
                if (string.IsNullOrEmpty(_packageAssetsFolderName))
                {
                    _packageAssetsFolderName = "AutomatedQA";
                }
                return _packageAssetsFolderName;
            }
            set
            {
                _packageAssetsFolderName = value;
            }
        }
        private static string _packageAssetsFolderName;

        /// <summary>
        /// Full path to folder where we store recording files.
        /// </summary>
        public static string RecordingDataPath
        {
            get
            {
                if (string.IsNullOrEmpty(_recordingDataPath))
                {
                    _recordingDataPath = Path.Combine(Application.dataPath, RecordingFolderName);
                }
                return _recordingDataPath;
            }
            set
            {
                _recordingDataPath = value;
            }
        }
        private static string _recordingDataPath;

        /// <summary>
        /// Name of folder under Assets, including Assets path, where we store recording files.
        /// </summary>
        public static string RecordingFolderNameWithAssetPath
        {
            get
            {
                return $"Assets/{RecordingFolderName}";
            }
        }

        /// <summary>
        /// Name of folder under Assets where we store recording files.
        /// </summary>
        public static string RecordingFolderName
        {
            get
            {
                if (string.IsNullOrEmpty(_recordingFolderName))
                {
                    _recordingFolderName = "Recordings";
                }
                return _recordingFolderName;
            }
            set
            {
                _recordingFolderName = value;
            }
        }
        private static string _recordingFolderName;

        /// <summary>
        /// Name of folder where we tests generated from recordings.
        /// </summary>
        public static string GeneratedTestsFolderName
        {
            get
            {
                if (string.IsNullOrEmpty(_generatedTestsFolderName))
                {
                    _generatedTestsFolderName = "GeneratedTests";
                }
                return _generatedTestsFolderName;
            }
            set
            {
                _generatedTestsFolderName = value;
            }
        }
        private static string _generatedTestsFolderName;

        /// <summary>
        /// Enable or disable visual Fx feedback for actions taken during playback of recordings.
        /// If true, check individual feedback booleans to see if a subset will be activated. 
        /// </summary>
        public static bool ActivatePlaybackVisualFx
        {
            get
            {
                return _activatePlaybackVisualFx;
            }
            set
            {
                _activatePlaybackVisualFx = value;
            }
        }
        private static bool _activatePlaybackVisualFx = true;

        /// <summary>
        /// Activates ripple effect on point of click during playback of recordings.
        /// </summary>
        public static bool ActivateClickFeedbackFx
        {
            get
            {
                return _activateClickFeedbackFx;
            }
            set
            {
                _activateClickFeedbackFx = value;
            }
        }
        private static bool _activateClickFeedbackFx = true;

        /// <summary>
        /// Activates drag effect between drag start and drag release during playback of recordings.
        /// </summary>
        public static bool ActivateDragFeedbackFx
        {
            get
            {
                return _activateDragFeedbackFx;
            }
            set
            {
                _activateDragFeedbackFx = value;
            }
        }
        private static bool _activateDragFeedbackFx = true;

        /// <summary>
        /// Activates highlight effect on point of click during playback of recordings.
        /// </summary>
        public static bool ActivateHighlightFeedbackFx
        {
            get
            {
                return _activateHighlightFeedbackFx;
            }
            set
            {
                _activateHighlightFeedbackFx = value;
            }
        }
        private static bool _activateHighlightFeedbackFx = true;

        /// <summary>
        /// Allows screenshots to be recorded during test run. These are used to show screenshots in reports.
        /// </summary>
        public static bool EnableScreenshots
        {
            get
            {
                return _enableScreenshots;
            }
            set
            {
                _enableScreenshots = value;
            }
        }
        private static bool _enableScreenshots = true;

        public static float PostActionScreenshotDelay
        {
            get
            {
                if (_postActionScreenshotDelay < 0f)
                {
                    _postActionScreenshotDelay = 0f;
                }
                return _postActionScreenshotDelay;
            }
            set
            {
                _postActionScreenshotDelay = value;
            }
        }
        private static float _postActionScreenshotDelay = 0.25f;

        public static bool PerformanceWarningsEnabled
        {
            get
            {
                return _performanceWarningsEnabled;
            }
            set
            {
                _performanceWarningsEnabled = value;
            }
        }
        private static bool _performanceWarningsEnabled;

        public static float FpsMinimumWarningThreshold
        {
            get
            {
                if (Mathf.Approximately(_fpsMinimumWarningThreshold, 0f))
                {
                    // Set 20 fps as a generous minimum low framerate to consider a default value.
                    _fpsMinimumWarningThreshold = 20f;
                }
                return _fpsMinimumWarningThreshold;
            }
            set
            {
                _fpsMinimumWarningThreshold = value;
            }
        }
        private static float _fpsMinimumWarningThreshold;

        public static float HeapSizeMbMaximumWarningThreshold
        {
            get
            {
                if (Mathf.Approximately(_heapSizeMbMaximumWarningThreshold, 0f))
                {
                    // Set a default of 512mb desktop or 100mb mobile as a reasonable max default limit.
                    _heapSizeMbMaximumWarningThreshold = Application.isMobilePlatform ? 100f : 512f;
                }
                return _heapSizeMbMaximumWarningThreshold;
            }
            set
            {
                _heapSizeMbMaximumWarningThreshold = value;
            }
        }
        private static float _heapSizeMbMaximumWarningThreshold;
        
        /// <summary>
        /// Allows screenshots to be recorded during test run. These are used to show screenshots in reports.
        /// </summary>
        public static bool ThrowGameObjectInvisibleToCamera
        {
            get
            {
                return _throwGameObjectInvisibleToCamera;
            }
            set
            {
                _throwGameObjectInvisibleToCamera = value;
            }
        }
        private static bool _throwGameObjectInvisibleToCamera = true;

        /// <summary>
        /// Set the maximum level of log messages
        /// 0 = Logging disabled.
        /// 1 = Errors only.
        /// 2 = Errors and Warnings.
        /// 3 = Errors, Warnings, Info.
        /// 4 = Errors, Warnings, Info, Debug.
        /// </summary>
        public static AQALogger.LogLevel LogLevel
        {
            get
            {
                return _logLevel;
            }
            set
            {
                _logLevel = value;
            }
        }
        private static AQALogger.LogLevel _logLevel = AQALogger.LogLevel.Info;

        public static BuildType buildType
        {
            get
            {
#if AQA_BUILD_TYPE_FULL
                return BuildType.FullBuild;
#else
                return BuildType.UnityTestRunner;
#endif
            }
        }

        public static HostPlatform hostPlatform
        {
            get
            {
#if AQA_PLATFORM_CLOUD
                return HostPlatform.Cloud;
#else
                return HostPlatform.Local;
#endif
            }
        }

        public static RecordingFileStorage recordingFileStorage
        {
            get
            {
                // TODO use a runtime config file instead of preprocessor defines?
#if AQA_RECORDING_STORAGE_CLOUD
                return RecordingFileStorage.Cloud;
#else
                return RecordingFileStorage.Local;
#endif
            }
        }

        /// <summary>
        /// Record input from the Input Manager (Input class). Playback requires usage replaced with the RecordableInput class.
        /// </summary>
        public static bool RecordInputManager
        {
            get
            {
                return _recordInputManager;
            }
            set
            {
                _recordInputManager = value;
            }
        }
        private static bool _recordInputManager = false;

        /// <summary>
        /// Wait for elements to become interactable before trying to interact with them (as opposed to waiting for the original recorded timeDelta period before executing an action).
        /// </summary>
        public static bool UseDynamicWaits
        {
            get
            {
                return _useDynamicWaits;
            }
            set
            {
                _useDynamicWaits = value;
            }
        }
        private static bool _useDynamicWaits = true;

        /// <summary>
        /// Period of time to wait for a target GameObject to be ready while waiting to perform the next action in test playback.
        /// </summary>
        public static float DynamicWaitTimeout
        {
            get
            {
                if (_dynamicWaitTimeout < 0.001f)
                {
                    _dynamicWaitTimeout = 10f;
                }
                return _dynamicWaitTimeout;
            }
            set
            {
                _dynamicWaitTimeout = value;
            }
        }
        private static float _dynamicWaitTimeout = 0f;

        /// <summary>
        /// Period of time to wait for scene load while waiting to perform the next action in test playback.
        /// </summary>
        public static float DynamicLoadSceneTimeout
        {
            get
            {
                if (_dynamicLoadSceneTimeout < 0.001f)
                {
                    _dynamicLoadSceneTimeout = 30f;
                }
                return _dynamicLoadSceneTimeout;
            }
            set
            {
                _dynamicLoadSceneTimeout = value;
            }
        }
        private static float _dynamicLoadSceneTimeout = 0f;
        
        
        /// <summary>
        /// Enable Cloud Testing features.
        /// </summary>
        public static bool EnableCloudTesting
        {
            get
            {
                return _enableCloudTesting;
            }
            set
            {
                _enableCloudTesting = value;
            }
        }
        private static bool _enableCloudTesting = false;

        private static TextAsset configTextAsset
        {
            get
            {
                if (_configTextAsset == null)
                    RefreshConfig();
                return _configTextAsset;
            }
        }
        private static TextAsset _configTextAsset;

        /// <summary>
        /// Resources are cached. Changes made to them in run time will not be seen until reload. Since edits to configs won't happen outside of editor, use Resources.Load outside of editor and File.ReadAllText in editor.
        /// </summary>
        public static void RefreshConfig()
        {
#if UNITY_EDITOR
            try
            {
                string path = Path.Combine(Application.dataPath, AutomatedQASettingsResourcesPath, AutomatedQaSettingsFileName);
                if (!File.Exists(path))
                {
                    _configTextAsset = null;
                    return;
                }
                _configTextAsset = new TextAsset(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("get_dataPath is not allowed"))
                {
                    throw e;
                }
            }
#else
            _configTextAsset = Resources.Load<TextAsset>(Path.GetFileNameWithoutExtension(AutomatedQaSettingsFileName));
#endif

            Init();
        }

        public static AutomatedQASettingsData GetCustomSettingsData()
        {
            if (configTextAsset == null)
            {
                AutomatedQASettingsData configCategories = new AutomatedQASettingsData();
                configCategories.Configs.Add(new AutomationSet(Keys.LogLevel, ((int)LogLevel).ToString()));
                configCategories.Configs.Add(new AutomationSet(Keys.EnableScreenshots, EnableScreenshots.ToString()));
                configCategories.Configs.Add(new AutomationSet(Keys.PostActionScreenshotDelay, PostActionScreenshotDelay.ToString()));
                configCategories.Configs.Add(new AutomationSet(Keys.RecordingFolderName, RecordingFolderName));
                configCategories.Configs.Add(new AutomationSet(Keys.ActivatePlaybackVisualFx, ActivatePlaybackVisualFx.ToString()));
                configCategories.Configs.Add(new AutomationSet(Keys.ActivateClickFeedbackFx, ActivateClickFeedbackFx.ToString()));
                configCategories.Configs.Add(new AutomationSet(Keys.ActivateDragFeedbackFx, ActivateDragFeedbackFx.ToString()));
                configCategories.Configs.Add(new AutomationSet(Keys.ActivateHighlightFeedbackFx, ActivateHighlightFeedbackFx.ToString()));
                configCategories.Configs.Add(new AutomationSet(Keys.UseDynamicWaits, UseDynamicWaits.ToString()));
                configCategories.Configs.Add(new AutomationSet(Keys.DynamicWaitTimeout, DynamicWaitTimeout.ToString()));
                configCategories.Configs.Add(new AutomationSet(Keys.DynamicLoadSceneTimeout, DynamicLoadSceneTimeout.ToString()));
                configCategories.Configs.Add(new AutomationSet(Keys.RecordInputManager, RecordInputManager.ToString()));
                configCategories.Configs.Add(new AutomationSet(Keys.PerformanceWarningsEnabled, PerformanceWarningsEnabled.ToString()));
                configCategories.Configs.Add(new AutomationSet(Keys.HeapSizeMbMaximumWarningThreshold, HeapSizeMbMaximumWarningThreshold.ToString()));
                configCategories.Configs.Add(new AutomationSet(Keys.FpsMinimumWarningThreshold, FpsMinimumWarningThreshold.ToString()));
                configCategories.Configs.Add(new AutomationSet(Keys.ThrowGameObjectInvisibleToCamera, ThrowGameObjectInvisibleToCamera.ToString()));
                // configCategories.Configs.Add(new AutomationSet(Keys.EnableCloudTesting, EnableCloudTesting.ToString()));

#if UNITY_EDITOR
                try
                {
                    File.WriteAllText(Path.Combine(Application.dataPath, AutomatedQASettingsResourcesPath, AutomatedQaSettingsFileName), JsonUtility.ToJson(configCategories));
                }
                catch (Exception e)
                {
                    if (!e.Message.Contains("get_dataPath is not allowed"))
                    {
                        throw e;
                    }
                }
#endif
                return configCategories;
            }
            return JsonUtility.FromJson<AutomatedQASettingsData>(configTextAsset.text);
        }

        public static string GetStringFromCustomSettings(string key)
        {
            AQALogger logger = new AQALogger();

            AutomationSet keyVal = settings.Configs.Find(x => x.Key == key);
            if (keyVal == default(AutomationSet) || string.IsNullOrEmpty(keyVal.Key))
            {
                logger.LogError($"Key requested ({key}) which is not defined in the settings file or is invalid.{(hostPlatform == HostPlatform.Cloud ? " Make sure you are supplying the expected settings config file name to DeviceFarmConfig." : string.Empty)}");
            }
            return keyVal.Value;
        }

        public static int GetIntFromCustomSettings(string key)
        {
            AQALogger logger = new AQALogger();

            AutomationSet keyVal = settings.Configs.Find(x => x.Key == key);
            int val = 0;
            bool isInt = keyVal == default(AutomationSet) ? false : int.TryParse(keyVal.Value, out val);
            if (!isInt)
            {
                logger.LogError($"Key requested ({key}) which is not defined in the settings file or is invalid.{(hostPlatform == HostPlatform.Cloud ? " Make sure you are supplying the expected settings config file name to DeviceFarmConfig." : string.Empty)}");
            }
            return val;
        }

        public static float GetFloatFromCustomSettings(string key)
        {
            AQALogger logger = new AQALogger();

            AutomationSet keyVal = settings.Configs.Find(x => x.Key == key);
            float val = 0;
            bool isFloat = keyVal == default(AutomationSet) ? false : float.TryParse(keyVal.Value, out val);
            if (!isFloat)
            {
                logger.LogError($"Key requested ({key}) which is not defined in the settings file or is invalid.{(hostPlatform == HostPlatform.Cloud ? " Make sure you are supplying the expected settings config file name to DeviceFarmConfig." : string.Empty)}");
            }
            return val;
        }

        public static bool GetBooleanFromCustomSettings(string key)
        {
            AQALogger logger = new AQALogger();

            AutomationSet keyVal = settings.Configs.Find(x => x.Key == key);
            bool returnVal = false;
            if (keyVal == default(AutomationSet) || !bool.TryParse(keyVal.Value, out returnVal))
            {
                logger.LogError($"Key requested ({key}) which is not defined in the settings file or is invalid.{(hostPlatform == HostPlatform.Cloud ? " Make sure you are supplying the expected settings config file name to DeviceFarmConfig." : string.Empty)}");
            }
            return returnVal;
        }

        public static string AutomatedQaSettingsFileName
        {
            get
            {
                if (string.IsNullOrEmpty(_automatedQaSettingsFileName))
                {
                    _automatedQaSettingsFileName = "AutomatedQASettings.json";
                }
                return _automatedQaSettingsFileName;
            }
            set
            {
                _automatedQaSettingsFileName = value;
            }
        }
        private static string _automatedQaSettingsFileName;

        /// <summary>
        /// Name of the relative data path that our files are stored under.
        /// </summary>
        public static string AutomatedQASettingsResourcesPath
        {
            get
            {
                if (string.IsNullOrEmpty(_automatedQASettingsResourcesPath))
                {
                    _automatedQASettingsResourcesPath = Path.Combine(PackageAssetsFolderName, "Resources");
                }
                return _automatedQASettingsResourcesPath;
            }
            set
            {
                _automatedQASettingsResourcesPath = value;
            }
        }
        private static string _automatedQASettingsResourcesPath;

        [System.Serializable]
        public class AutomatedQASettingsData
        {
            public AutomatedQASettingsData()
            {
                Configs = new List<AutomationSet>();
            }
            public List<AutomationSet> Configs;
        }

        [System.Serializable]
        public class AutomationSet
        {
            public AutomationSet(string Key, string Value)
            {
                this.Key = Key;
                this.Value = Value;
            }
            public string Key;
            public string Value;
        }
    }
}