using System.IO;
using System.Threading.Tasks;
using Unity.RecordedPlayback;
using Unity.RecordedPlayback.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.AutomatedQA.Editor
{
    public class BuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder { get; }

        private static string resourcesPath = "Assets/AutomatedQA/Temp/Resources";
        
        public void OnPreprocessBuild(BuildReport report)
        {
            if ((report.summary.options & BuildOptions.IncludeTestAssemblies) != 0)
            {
                RecordedPlaybackEditorUtils.CreateDirectoryIfNotExists(resourcesPath);

                foreach (var testdata in RecordedTesting.RecordedTesting.GetAllRecordedTests())
                {
                    string sourceFromEditor = Path.Combine(Application.dataPath, testdata.recording);
                    string destInResources = Path.Combine(resourcesPath, testdata.recording);

                    if (File.Exists(sourceFromEditor))
                    {
                        RecordedPlaybackEditorUtils.CreateDirectoryIfNotExists(Path.GetDirectoryName(destInResources));
                        RecordedPlaybackEditorUtils.MarkFilesAsCreated(RecordedPlaybackPersistentData.CopyRecordingFile(sourceFromEditor, destInResources));
                    }
                    else
                    {
                        Debug.LogError($"file {sourceFromEditor} doesn't exist");
                    }
                }
                
                var runs = AssetDatabase.FindAssets("t:AutomatedRun");
                foreach (var runGuid in runs)
                {
                    string runPath = AssetDatabase.GUIDToAssetPath(runGuid);
                    string sourceFromEditor = Path.Combine(runPath);
                    string destInResources = Path.Combine(resourcesPath, "AutomatedRuns", Path.GetFileName(runPath));

                    if (File.Exists(sourceFromEditor))
                    {
                        RecordedPlaybackEditorUtils.CreateDirectoryIfNotExists(Path.GetDirectoryName(destInResources));
                        File.Copy(sourceFromEditor, destInResources);
                        RecordedPlaybackEditorUtils.MarkFileAsCreated(destInResources);
                        Debug.Log($"Copied AutomatedRun file from {sourceFromEditor} to {destInResources}");

                    }
                    else
                    {
                        Debug.LogError($"file {sourceFromEditor} doesn't exist");
                    }
                }
            }
            ClearFilesOnBuildCompletion(report);
        }

        static async void ClearFilesOnBuildCompletion(BuildReport report)
        {
            while (report.summary.result == BuildResult.Unknown)
            {
                await Task.Delay(1000);
            }

            RecordedPlaybackEditorUtils.ClearCreatedPaths();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if ((report.summary.options & BuildOptions.IncludeTestAssemblies) != 0)
            {
                RecordedPlaybackEditorUtils.ClearCreatedPaths();
            }
        }
    }
}
