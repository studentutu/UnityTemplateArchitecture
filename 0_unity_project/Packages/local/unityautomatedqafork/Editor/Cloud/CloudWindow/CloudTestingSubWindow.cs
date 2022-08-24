using System;
using System.Collections.Generic;
using TestPlatforms.Cloud;
using Unity.AutomatedQA;
using Unity.AutomatedQA.Editor;
using Unity.CloudTesting.Editor;
using Unity.RecordedPlayback.Editor;
using Unity.RecordedPlayback;
using UnityEditor;
using UnityEngine;
using UnityEngine.iOS;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Unity.RecordedTesting.Editor
{

    /// <summary>
    /// Editor Window for running AQA tests on cloud device farm
    /// </summary>
    public class CloudTestingSubWindow : HubSubWindow
    {
        private static readonly string WINDOW_FILE_NAME = "cloud-window";
        private static string resourcePath = "Packages/com.unity.automated-testing/Editor/Cloud/CloudWindow/";
        
        private static readonly string[] SupportedBuildTargets = { BuildTarget.Android.ToString(), BuildTarget.iOS.ToString() };
        private static ICloudTestClient Client = new CloudTestClient();

        private List<string> allCloudTests = new List<string>();
        private JobStatusResponse jobStatus = new JobStatusResponse();
        private UploadUrlResponse uploadInfo = new UploadUrlResponse();
        private BundleUpload _bundleUpload = new BundleUpload();
        private DateTime lastRefresh = DateTime.UtcNow;
        private int buildTargetIndex = 0;
        private BuildTarget originalBuildTarget;
        private BuildTarget buildTarget;
        private bool createBuild = false;
        CloudTestDeviceInput cloudTestDeviceInput;
        
        private ScrollView root;
        private VisualElement baseRoot;

        private int priorBuildIndex = 0;
        private Lazy<GetBuildsResponse> buildResponse = new Lazy<GetBuildsResponse>(() => Client.GetBuilds());
        
        private int jobIndex = 0;
        private Lazy<GetJobsResponse> jobResponse = new Lazy<GetJobsResponse>(() => Client.GetJobs());
        
        private List<string> deviceList = new List<string>();

        private GUIContent servicesNotEnabledContent = new GUIContent("To get started with cloud device testing, you must first link your project to a Unity Cloud Project ID. A Unity Cloud Project ID is an online identifier which is used across all Unity Services. These can be created within the Services window itself, or online on the Unity Services website. The simplest way is to use the Services window within Unity, as follows: \n\nTo open the Services Window, go to Window > General > Services.\n\nNote: using Unity cloud device testing does not require that you turn on any additional, individual cloud services like Analytics, Ads, Cloud Build, etc.");

        public override void Init()
        {
            RecordedPlaybackAnalytics.SendRecordingWindowInteraction("CloudTestingSubWindow", "Open");

            var editorBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            if (SupportedBuildTargets.Contains(editorBuildTarget))
            {
                buildTargetIndex = Array.IndexOf(SupportedBuildTargets, editorBuildTarget);
            }
            allCloudTests.Clear();
            var cloudDict = CloudTestPipeline.GetCloudTests();
            foreach (var testlist in cloudDict)
            {
                allCloudTests.AddRange(testlist.Value);
            }
        }

        public override void SetUpView(ref VisualElement br)
        {
            br.Clear();
            root = new ScrollView();
            baseRoot = br;
            baseRoot.Add(root);

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(resourcePath + $"{WINDOW_FILE_NAME}.uxml");
            visualTree.CloneTree(baseRoot);

            baseRoot.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(resourcePath + $"{WINDOW_FILE_NAME}.uss"));
            baseRoot.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(HubWindow.HUB_RESOURCES_PATH + $"{HubWindow.ALL_WINDOWS_USS_FILE_NAME}"));

            root.Add(HubWindow.Instance.AddHubBackButton());
            root.Add(new IMGUIContainer(() =>
            {
                UpdateIMGUI();
            }));

        }

        void UpdateIMGUI()
        {
            if (!AreServicesEnabled())
            {
                return;
            }

            GUITestList();
            GUILayout.FlexibleSpace();
            GUIEmailUs();
            GUIPlatformSelect();
            GUIBuild();
            GUIBuildSelect();

            GUIRunButton();
            GUILayout.FlexibleSpace();

            GUIResults();
            GUILayout.FlexibleSpace();
        }

        public override void OnGUI()
        {
            if (createBuild && !EditorApplication.isCompiling)
            {
                createBuild = false;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS));
                _bundleUpload.buildName = $"CloudBundle{CloudTestConfig.BuildFileExtension}";
                _bundleUpload.buildPath = CloudTestConfig.BuildPath;
                CloudTestBuilder.CreateBuild(buildTarget);
                CloudTestPipeline.testBuildFinished += OnBuildFinish;
            }
        }

        void GUITestList()
        {
            EditorGUILayout.LabelField("Tests", EditorStyles.boldLabel);
            EditorGUILayout.BeginScrollView(Vector2.zero);

            foreach (var test in allCloudTests)
            {
                EditorGUILayout.LabelField(test.ToString());
            }
            EditorGUILayout.EndScrollView();
        }

        void GUIEmailUs()
        {
            if (GUILayout.Button("Please email us at AutomatedQA@unity3d.com for information on increasing your usage limit.", EditorStyles.linkLabel))
            {
                Application.OpenURL("mailto:AutomatedQA@unity3d.com");
            }
        }

        void GUIPlatformSelect()
        {
            buildTargetIndex = EditorGUILayout.Popup("Target Platform", buildTargetIndex, SupportedBuildTargets);
        }

        void GUIBuildSelect()
        {

            var buildStrings = buildResponse.Value.builds.Select(x => $"{x.buildName}   {x.createdAt}");

            priorBuildIndex = EditorGUILayout.Popup("Available builds", priorBuildIndex, buildStrings.ToArray());
        }

        void GUIBuild()
        {
            var buildTargetStr = SupportedBuildTargets[buildTargetIndex];
            var msg = "Usage of the Unity editor will be blocked until the build process is complete.";
            if (buildTargetStr != EditorUserBuildSettings.activeBuildTarget.ToString())
            {
                msg += $"\n\nActive build target {EditorUserBuildSettings.activeBuildTarget} does not match the selected target {buildTargetStr} which will increase the compilation time.";
            }
            if (GUILayout.Button("Build & Upload") && EditorUtility.DisplayDialog("Confirm Build", msg,
                "Continue", "Cancel"))
            {
                buildTarget = (BuildTarget) Enum.Parse(typeof(BuildTarget), buildTargetStr);
                DoBuildAndUpload();
            }
        }

        void GUIRunButton()
        {

            cloudTestDeviceInput = (CloudTestDeviceInput) EditorGUILayout.ObjectField(cloudTestDeviceInput, typeof(CloudTestDeviceInput), false);

            if (GUILayout.Button("Run on Device Farm"))
            {
                RunOnDeviceFarm(buildResponse.Value.builds[priorBuildIndex].buildId, cloudTestDeviceInput);
            }
        }

        void GUIJobSelect()
        {
            var jobStrings = jobResponse.Value.jobs.Select(x => $"{x.buildName}   {x.createdAt}");

            EditorGUI.BeginChangeCheck();
            jobIndex = EditorGUILayout.Popup("Past Jobs", jobIndex, jobStrings.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                RefreshJobStatus();
            }
        }
        
        void GUIResults()
        {
            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);
            GUIJobSelect();
            GUIJobStatus();
            GUIGetResultsButton();
        }

        void GUIJobStatus()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("status", jobStatus.status);
            EditorGUI.BeginDisabledGroup((DateTime.UtcNow - lastRefresh).TotalSeconds < 1);
            if (GUILayout.Button("Refresh"))
            {
                RefreshJobStatus();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        void GUIGetResultsButton()
        {
            if (GUILayout.Button("Get Test Results"))
            {
                RefreshJobStatus();

                if (jobStatus.jobId == "")
                {
                    string jobId = jobResponse.Value.jobs[jobIndex].jobId;
                    Client.GetTestResults(jobId);
                }
                else
                {
                    Client.GetTestResults(jobStatus.jobId);
                }
                
            }
        }

        void RefreshJobStatus()
        {
            if ((DateTime.UtcNow - lastRefresh).TotalSeconds < 1)
            {
                return;
            }
            lastRefresh = DateTime.UtcNow;

            var jobId = string.IsNullOrEmpty(jobStatus.jobId.Trim())? jobResponse.Value.jobs[jobIndex].jobId : jobStatus.jobId.Trim();
            jobStatus = Client.GetJobStatus(jobId);
        }

        void DoBuildAndUpload()
        {
            RecordedPlaybackAnalytics.SendRecordingWindowInteraction("CloudTestingSubWindow", "DoBuildAndUpload");
            CloudTools.UploadAllRecordings(RecordingUploadWindow.SetAllTestsAndRecordings());
            Debug.Log("Uploaded all recordings.");
            originalBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (buildTarget != EditorUserBuildSettings.activeBuildTarget)
            {
                Debug.Log("switching build target");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(buildTarget), buildTarget);
            }
            createBuild = true;
        }

        private void RunOnDeviceFarm(string buildId, CloudTestDeviceInput cloudTestDevices)
        {
            RecordedPlaybackAnalytics.SendRecordingWindowInteraction("CloudTestingSubWindow", "RunOnDeviceFarm");
            // TODO use allCloudTests
            var runTests = new List<string>(new string[] { "DummyUTFTest" });

            jobStatus = Client.RunCloudTests(buildId, runTests, cloudTestDevices);
            jobResponse = new Lazy<GetJobsResponse>(() => Client.GetJobs());
        }

        private bool AreServicesEnabled()
        {
            if (string.IsNullOrEmpty(CloudProjectSettings.projectId) || string.IsNullOrEmpty(CloudProjectSettings.organizationId))
            {
                GUIStyle style = GUI.skin.label;
                style.wordWrap = true;
                EditorGUILayout.LabelField(servicesNotEnabledContent, style);
                return false;
            }
            return true;
        }

        private void OnBuildFinish()
        {
#if UNITY_IOS
            CloudTestPipeline.ArchiveIpa();
#endif
            CloudTestPipeline.testBuildFinished -= OnBuildFinish;

            Debug.Log($"Build successfully saved at - {_bundleUpload.buildPath}");
            if (originalBuildTarget != EditorUserBuildSettings.activeBuildTarget)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(originalBuildTarget), originalBuildTarget);
            }

            uploadInfo = Client.GetUploadURL();
            Client.UploadBuildToUrl(uploadInfo.upload_uri, _bundleUpload.buildPath);
            
            buildResponse = new Lazy<GetBuildsResponse>(() => Client.GetBuilds());

            // TODO Query upload status
            EditorUtility.DisplayProgressBar("Wait for upload status", "Wait for upload status", 0);
            EditorUtility.ClearProgressBar();
        }

        //[MenuItem("Automated QA/Dev/Run Debug Cloud Test")]
        static void RunDebugCloudTest()
        {
            Client.RunCloudTests("f3678dc4-2218-4320-a4c8-41ff5cdd7b22", 
                new List<string>(new string[] { "DummyUTFTest" }), new CloudTestDeviceInput());
        }

        //   [MenuItem("Automated QA/Dev/Log Project ID")]
        static void LogProjectID()
        {
            Debug.Log($"" +
                      $"Application.cloudProjectId: {Application.cloudProjectId}\n" +
                      $"CloudProjectSettings.accessToken: {CloudProjectSettings.accessToken}\n" +
                      $"");
        }

    }
}