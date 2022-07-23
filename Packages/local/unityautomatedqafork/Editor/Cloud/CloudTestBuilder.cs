using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TestPlatforms.Cloud;
using Unity.AutomatedQA;
using Unity.AutomatedQA.Editor;
using UnityEditor;
using UnityEngine;

#if UNITY_IOS
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
#endif

namespace Unity.CloudTesting.Editor
{
    public class CloudTestBuilder
    {
        public static ICloudTestClient Client = new CloudTestClient();

#if UNITY_IOS
        [PostProcessBuild]
        public static void ChangeXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
        {
            // Adds UIFileSharingEnabled to Info.plist
            if (buildTarget == BuildTarget.iOS && CloudTestPipeline.IsRunningOnCloud())
            {
                // Get plist
                string plistPath = pathToBuiltProject + "/Info.plist";
                PlistDocument plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));

                // Get root
                PlistElementDict rootDict = plist.root;
                rootDict.SetBoolean("UIFileSharingEnabled", true);
                // rootDict.SetString("CFBundleDisplayName", "CloudBundle");
                // Write to Info.plist
                Debug.Log("Updating Info.plist");
                File.WriteAllText(plistPath, plist.WriteToString());
            }
        }
#endif

        /// <summary>
        /// Generates a build that can be used for cloud device testing for a desired build target.
        /// </summary>
        public static void CreateBuild()
        {
            var args = ParseCommandLineArgs();
            CreateBuild(args.TargetPlatform);
        }

        /// <inheritdoc cref="CreateBuild()"/>
        public static void CreateBuild(BuildTarget targetPlatform)
        {
            Debug.Log("Creating Build for platform " + targetPlatform);
            if (File.Exists(CloudTestConfig.BuildPath))
            {
                File.Delete(CloudTestConfig.BuildPath);
            }

#if UNITY_IOS
            if (Directory.Exists(CloudTestConfig.IOSBuildDir))
            {
                Directory.Delete(CloudTestConfig.IOSBuildDir, true);
            }
#endif

            // Editor code flag for utf cloud workflows
            CloudTestPipeline.SetTestRunOnCloud(true);

            // Setting scripting defines
            AutomatedQABuildConfig.ApplyBuildFlags(BuildPipeline.GetBuildTargetGroup(targetPlatform));

            var filter = GetTestFilter();

            CloudTestPipeline.MakeBuild(filter.ToArray(), targetPlatform);

            if (Application.isBatchMode)
            {
                CloudTestPipeline.testBuildFinished += () => Debug.Log($"Build {CloudTestConfig.BuildPath} complete");
                // Since MakeBuild executes on Update we need to manually invoke it to prevent batch mode exiting early
                EditorApplication.update.Invoke();
            }
        }

        private static UploadUrlResponse UploadBuild(string buildPath, string accessToken, string projectId)
        {
            var uploadUrlResponse = Client.UploadBuild(buildPath, accessToken, projectId);
            Debug.Log($"Uploaded build with id {uploadUrlResponse.id}");
            return uploadUrlResponse;
        }

        private static void RunTests(string buildId, string accessToken, string projectId)
        {
            var cloudTests = new List<string>(new[] { "DummyUTFTest" });
            Debug.Log($"Running Cloud Tests: {string.Join(",", cloudTests)}");
            var cloudTestSubmission = new CloudTestDeviceInput();
            var jobStatusResponse = Client.RunCloudTests(buildId, cloudTests, cloudTestSubmission, accessToken, projectId);

            AwaitTestResults(jobStatusResponse.jobId, accessToken, projectId);
        }

        /// <summary>
        /// Helper method that can be used from the command line which will create a new cloud testing build,
        /// upload it, and await test completion. If called using batch mode a non-zero exit code will be returned
        /// on test failure.
        /// </summary>
        public static void BuildAndRunTests()
        {
            var args = ParseCommandLineArgs();
            BuildAndRunTests(args.TargetPlatform, args.AccessToken, args.ProjectId);
        }

        /// <inheritdoc cref="BuildAndRunTests()"/>
        public static void BuildAndRunTests(BuildTarget targetPlatform, string accessToken, string projectId)
        {
            CreateBuild(targetPlatform);
            UploadAndRunTests(CloudTestConfig.BuildPath, accessToken, projectId);
        }

        /// <summary>
        /// Helper method that can be used from the command line which will upload a provided build file then
        /// upload it to the cloud testing service and await test completion. If called using batch mode a non-zero
        /// exit code will be returned on test failure.
        /// </summary>
        public static void UploadAndRunTests()
        {
            var args = ParseCommandLineArgs();
            UploadAndRunTests(args.UploadFile, args.AccessToken, args.ProjectId );
        }

        /// <inheritdoc cref="UploadAndRunTests()"/>
        public static void UploadAndRunTests(string uploadFile, string accessToken, string projectId)
        {
            UploadUrlResponse uploadUrlResponse = UploadBuild(uploadFile, accessToken, projectId);
            Thread.Sleep(TimeSpan.FromSeconds(30f)); // wait before triggering tests to avoid failure
            RunTests(uploadUrlResponse.id, accessToken, projectId);
        }

        internal static TestResultsResponse AwaitTestResults(string jobId, string accessToken, string projectId)
        {
            var jobStatusResponse = Client.GetJobStatus(jobId, accessToken, projectId);
            while (jobStatusResponse.IsInProgress())
            {
                Thread.Sleep(TimeSpan.FromSeconds(60f));
                jobStatusResponse = Client.GetJobStatus(jobStatusResponse.jobId, accessToken, projectId);
            }

            var testResults = Client.GetTestResults(jobStatusResponse.jobId, accessToken, projectId);
            if (!testResults.allPass)
            {
                throw new Exception("Job has completed but there are failing tests");
            }

            Debug.Log("All tests passed");
            return testResults;
        }

        private static CommandLineArgs ParseCommandLineArgs()
        {
            return ParseCommandLineArgs(System.Environment.GetCommandLineArgs());
        }

        internal static CommandLineArgs ParseCommandLineArgs(string[] args)
        {
            var commandLineArgs = new CommandLineArgs();
            var accessToken = GetArgValue("token", args);
            if (!string.IsNullOrEmpty(accessToken))
            {
                commandLineArgs.AccessToken = accessToken;
            }

            string testPlatformStr = GetArgValue("testPlatform", args);
            if (!string.IsNullOrEmpty(testPlatformStr))
            {
                if (!Enum.TryParse(testPlatformStr, true, out BuildTarget testPlatform))
                {
                    throw new Exception($"Invalid testPlatform {testPlatformStr}, please use a valid BuildTarget");
                }
                commandLineArgs.TargetPlatform = testPlatform;
            }

            var projectId = GetArgValue("projectId", args);
            if (!string.IsNullOrEmpty(projectId))
            {
                commandLineArgs.ProjectId = projectId;
            }

            var outputDir = GetArgValue("outputDir", args);
            if (!string.IsNullOrEmpty(outputDir))
            {
                CloudTestConfig.BuildFolder = outputDir;
            }

            var uploadFile = GetArgValue("uploadFile", args);
            if (!string.IsNullOrEmpty(uploadFile))
            {
                commandLineArgs.UploadFile = uploadFile;
            }
            
            EditorUserBuildSettings.exportAsGoogleAndroidProject = IsArgFlagSet("exportAsGoogleAndroidProject", args);

            return commandLineArgs;
        }

        private static bool IsArgFlagSet(string name, string[] args)
        {
            return args.Contains($"-{name}");
        }
        

        private static string GetArgValue(string name, string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == $"-{name}" && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
                if (args[i].StartsWith($"-{name}="))
                {
                    return args[i].Split('=')[1];
                }
            }

            return null;
        }

        private static List<string> GetTestFilter()
        {
            var cloudTests = CloudTestPipeline.GetCloudTests();
            var filter = new List<string>();
            foreach (var tests in cloudTests.Values)
            {
                foreach (var test in tests)
                {
                    Debug.Log("Adding Test: " + test);
                    filter.Add(test);
                }
            }

            return filter;
        }

        internal class CommandLineArgs
        {
            private string _accessToken;
            public string AccessToken
            {
                get => string.IsNullOrEmpty(_accessToken) ? CloudProjectSettings.accessToken : _accessToken;
                set => _accessToken = value;
            }

            private BuildTarget? targetPlatform;
            public BuildTarget TargetPlatform
            {
                get => targetPlatform?? EditorUserBuildSettings.activeBuildTarget;
                set => targetPlatform = value;
            }
            
            private string _uploadfile;
            public string UploadFile
            {
                get => string.IsNullOrEmpty(_uploadfile) ? CloudTestConfig.BuildPath : _uploadfile;
                set => _uploadfile = value;
            }

            private string _projectId;
            public string ProjectId
            {
                get => _projectId?? Application.cloudProjectId;
                set => _projectId = value;
            }
        }
    }
}