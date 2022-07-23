using System.IO;
using Unity.AutomatedQA;
using UnityEditor;
using UnityEngine;

namespace TestPlatforms.Cloud
{
    public class CloudTestConfig
    {
        public static string BuildPath => Path.Combine(BuildFolder, BuildName);

        private static string _buildFolder;
        public static string BuildFolder
        {
            get => _buildFolder?? AutomatedQASettings.PersistentDataPath;
            set => _buildFolder = value;
        }

        public static string BuildName => $"{Application.productName.Replace(" ", "")}{BuildFileExtension}";

        public static string BuildFileExtension
        {
            get {
#if UNITY_IOS
            return ".ipa";
#else
                return  EditorUserBuildSettings.exportAsGoogleAndroidProject ? string.Empty : ".apk";
#endif
            }
        }

        public static string IOSBuildDir => Path.Combine(BuildFolder, Application.productName);
        
    }
}