using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Unity.AutomatedQA;
using UnityEditor;
using UnityEngine;

namespace Unity.AutomatedQA.Editor
{
    public static class AutomatedRunTestCreator
    {
        private static string TestType = "AutomatedRunTests";
        private static string GeneratedTestScriptTemplatePath =>
            $"{TestCreatorUtils.ScriptTemplatePath}C# Script-GeneratedAutomatedRunTests.cs.txt";

        public static void GenerateAutomatedRunTest(string runPath)
        {
            TestCreatorUtils.CreateTestAssemblyFolder();
            TestCreatorUtils.CreateTestAssembly();
            TestCreatorUtils.CreateTestScriptFolder(TestType);
            CreateTestScripts(runPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.ClearProgressBar();
        }

        private static void CreateTestScripts(string runPath)
        {
            string templateContent = File.ReadAllText(GeneratedTestScriptTemplatePath);
            string runName = Path.GetFileNameWithoutExtension(runPath);
            var testClassName = "AutomatedRunTest_" + runName;
            string content = templateContent
                .Replace("#CLASS_NAME#", testClassName)
                .Replace("#RUN_FILEPATH#", runPath.Replace("\\", "/"))
                .Replace("#RUN_NAME#", runName);

            var writePath = Path.Combine(Application.dataPath,
                TestCreatorUtils.AutomatedTestingFolderName,
                TestCreatorUtils.GeneratedTestsFolderName,
                TestType,
                $"{testClassName}.cs");
            EditorUtility.DisplayProgressBar("Generate Automated Run Test", $"Create Automated Run Test Scripts: {writePath}", 0);
            File.WriteAllText(writePath, content);
        }
    }
}