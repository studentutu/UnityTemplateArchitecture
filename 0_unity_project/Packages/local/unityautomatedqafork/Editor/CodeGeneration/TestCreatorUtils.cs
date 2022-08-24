using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.AutomatedQA.Editor
{
    public static class TestCreatorUtils
    {
        public static string AutomatedTestingFolderName => "AutomatedQA";
        public static string GeneratedTestsFolderName => "GeneratedTests";
        public static string ScriptTemplatePath = "Packages/com.unity.automated-testing/Editor/ScriptTemplates/";
        private static string GeneratedTestsAssemblyName => "GeneratedTests.asmdef";
        private static string GeneratedTestAssemblyTemplatePath =>
            $"{ScriptTemplatePath}Assembly Definition-GeneratedTests.asmdef.txt";

        public static void CreateTestAssemblyFolder()
        {
            EditorUtility.DisplayProgressBar("Generate Tests", "Create Test Assembly Folder", 0);
            Directory.CreateDirectory(Path.Combine(Application.dataPath, AutomatedTestingFolderName, GeneratedTestsFolderName));
        }

        public static void CreateTestAssembly()
        {
            EditorUtility.DisplayProgressBar("Generate Tests", "Create Test Assembly", 0);

            var template = File.ReadAllText(GeneratedTestAssemblyTemplatePath);
            var content = template.Replace("#SCRIPTNAME#", Path.GetFileNameWithoutExtension(GeneratedTestsAssemblyName));

            File.WriteAllText(
                Path.Combine(Application.dataPath, AutomatedTestingFolderName, GeneratedTestsFolderName,
                    GeneratedTestsAssemblyName),
                content);
        }

        public static void CreateTestScriptFolder(string testType)
        {
            EditorUtility.DisplayProgressBar("Generate Tests", "Create Test Script Folder", 0);
            var path = Path.Combine(Application.dataPath, AutomatedTestingFolderName, GeneratedTestsFolderName, testType);
            Directory.CreateDirectory(path);
        }
    }
}