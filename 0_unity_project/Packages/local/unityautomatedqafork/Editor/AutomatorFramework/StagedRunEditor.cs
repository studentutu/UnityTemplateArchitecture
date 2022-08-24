using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.AutomatedQA.Editor
{
    [CustomEditor(typeof(StagedRun))]
    public class StagedRunEditor : UnityEditor.Editor
    {
        private string AutomatorDirectoryName => $"{target.name}_Automators";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Generate Tests"))
            {
                // Generate AutomatedRuns then create tests from them
                foreach (string path in GenerateAutomatedRuns())
                {
                    AutomatedRunTestCreator.GenerateAutomatedRunTest(path);
                }
            }
        }

        /// <summary>
        /// Generate AutomatedRuns of all stage permutations 
        /// </summary>
        /// <returns>Paths of generated runs</returns>
        private List<string> GenerateAutomatedRuns()
        {
            StagedRun stagedRun = target as StagedRun;

            // Create directory if missing
            string directory = Path.Combine(Application.dataPath, "Resources", AutomatorDirectoryName);
            Directory.CreateDirectory(directory);

            List<string> generatedPaths = new List<string>();
            var runConfigs = stagedRun.CalculateStageSequences();
            foreach (var runConfig in runConfigs)
            {
                var path = GenerateAutomatedRun(runConfig);
                generatedPaths.Add(path);
            }

            return generatedPaths;
        }

        /// <summary>
        /// Create single AutomatedRun given sequence of AutomatorConfigs
        /// </summary>
        /// <param name="automators"></param>
        /// <returns>Path of output AutomatedRun</returns>
        private string GenerateAutomatedRun(List<AutomatorConfig> automators)
        {
            AutomatedRun runAsset = CreateInstance<AutomatedRun>();
            runAsset.config.automators = automators;

            // Get available path and save asset
            string autoRunName = $"{target.name}_AutomatedRun.asset";
            string tryPath = Path.Combine(Application.dataPath, "Resources", AutomatorDirectoryName, autoRunName);
            string availableName = GetAvailableBaseName(tryPath);
            string creationPath = Path.Combine("Assets", "Resources", AutomatorDirectoryName, availableName);

            AssetDatabase.CreateAsset(runAsset, creationPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created run at: " + creationPath);

            return creationPath;
        }

        /// <summary>
        /// Given base path, bump file name suffix if already exit
        /// </summary>
        /// <param name="basePath">System absolute path of file</param>
        /// <returns>Available file name</returns>
        private string GetAvailableBaseName(string basePath)
        {
            // Already available
            if (!File.Exists(basePath))
            {
                return Path.GetFileName(basePath);
            }

            // Trying basePath1.ext, basePath2.ext... until availability
            int i = 0;
            string parentDir = Path.GetDirectoryName(basePath);
            string fileName = Path.GetFileNameWithoutExtension(basePath);
            string extension = Path.GetExtension(basePath);
            string path;
            string suffixedName;
            do
            {
                i++;
                suffixedName = $"{fileName}{i}{extension}";
                path = Path.Combine(parentDir, suffixedName);
            } while (File.Exists(path));

            return suffixedName;
        }
    }

}