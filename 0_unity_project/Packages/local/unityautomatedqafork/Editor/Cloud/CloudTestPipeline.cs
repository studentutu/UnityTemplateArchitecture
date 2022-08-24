using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using TestPlatforms.Cloud;
using Unity.AutomatedQA;
using Unity.AutomatedQA.Editor;
using Unity.CloudTesting.Editor;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
[assembly:PostBuildCleanup(typeof(CloudTestPipeline))]

namespace Unity.CloudTesting.Editor
{
    public class CloudTestPipeline: IPostBuildCleanup
    {
        private static readonly TestRunnerApi TestRunnerInstance = ScriptableObject.CreateInstance<TestRunnerApi>();
        public static event Action testBuildFinished;

        public static bool IsRunningOnCloud()
        {
            return AutomatedQABuildConfig.hostPlatform == HostPlatform.Cloud;
        }

        public static void SetTestRunOnCloud(bool enabled)
        {
            AutomatedQABuildConfig.hostPlatform = enabled ? HostPlatform.Cloud : HostPlatform.Local;
        }

        public void Cleanup()
        {
            if (IsRunningOnCloud())
            {
                AutomatedQABuildConfig.ClearBuildFlags(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));

                if (testBuildFinished != null)
                {
                    testBuildFinished.Invoke();
                }
                SetTestRunOnCloud(false);
            }
        }

        public static void MakeBuild(string[] testToExecute)
        {
            MakeBuild(testToExecute, EditorUserBuildSettings.activeBuildTarget);
        }

        public static void MakeBuild(string[] testToExecute, BuildTarget platform)
        {
            var filter = new Filter()
            {
                testMode = TestMode.PlayMode,
                targetPlatform = platform
            };
            if (testToExecute.Length > 0)
                filter.testNames = testToExecute;

            var settings = new ExecutionSettings(filter);
            TestRunnerInstance.Execute(settings);
        }

        public static Dictionary<string,List<string>> GetCloudTests()
        {
            var testFilterDictionary = new Dictionary<string, List<string>>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            //Change this to use fancy Select feature.
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    var methods = type.GetTypeInfo().DeclaredMethods;
                    foreach (var method in methods)
                    {
                        if (method.GetCustomAttributes(typeof(CloudTestAttribute), false).Length > 0)
                        {
                            var assemblyName = assembly.GetName().Name;
                            if (!testFilterDictionary.ContainsKey(assemblyName))
                                testFilterDictionary.Add(assemblyName, new List<string>());

                            if (method.DeclaringType != null)
                            {
                                var methodNamespace = method.DeclaringType.Namespace;
                                var testEntry = methodNamespace == null
                                    ? method.DeclaringType.Name + "." + method.Name
                                    : method.DeclaringType.Namespace + "." + method.DeclaringType.Name + "." +
                                      method.Name;
                                testFilterDictionary[assemblyName]
                                    .Add(testEntry);
                                //   Debug.Log("Adding : " + method.DeclaringType.Name + "." + method.Name);
                            }
                        }
                    }
                }
            }

            return testFilterDictionary;
        }

        public static void ArchiveIpa()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "/usr/bin/xcodebuild";
            psi.Arguments += "-allowProvisioningUpdates -project " + $"\"{CloudTestConfig.IOSBuildDir}/Unity-iPhone.xcodeproj\"" +
                             " -scheme 'Unity-iPhone' -archivePath " +
                             $"\"{CloudTestConfig.IOSBuildDir}/utf.xcarchive\" archive";
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            var proc = Process.Start(psi);
            //TODO: Add progress bar
            while (!proc.StandardOutput.EndOfStream)
            {
                Debug.Log (proc.StandardOutput.ReadLine ());
            }

            psi.Arguments = "-exportArchive -archivePath " +
                            $"\"{CloudTestConfig.IOSBuildDir}/utf.xcarchive\" -exportPath " +
                            $"\"{CloudTestConfig.BuildFolder}\" -exportOptionsPlist " +
                            $"\"{CloudTestConfig.IOSBuildDir}/Info.plist\"";


            proc = Process.Start(psi);
            while (!proc.StandardOutput.EndOfStream) {
                Debug.Log (proc.StandardOutput.ReadLine ());
            }
            proc.WaitForExit();
            if (File.Exists(CloudTestConfig.BuildPath))
            {
                Debug.Log($"Generated ipa file at {CloudTestConfig.BuildPath}");
            }
            else
            {
                var msg = $"Error generating ipa file {CloudTestConfig.BuildPath}";
                if (Application.isBatchMode)
                {
                    throw new Exception(msg);
                }
                Debug.LogError(msg);
            }
        }
    }
}