#if UNITY_EDITOR

namespace QuickEditor.Core
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public enum PathMode
    {
        /// <summary>
        /// 系统全路径
        /// </summary>
        Full,

        /// <summary>
        /// Application.dataPath路径
        /// </summary>
        Data,

        /// <summary>
        /// Application.persistentDataPath路径
        /// </summary>
        Persistent,

        /// <summary>
        /// Application.temporaryCachePath路径
        /// </summary>
        Temporary,

        /// <summary>
        /// Application.streamingAssetsPath路径
        /// </summary>
        Streaming,

        /// <summary>
        /// 压缩的资源路径，Resources目录
        /// </summary>
        Resources,
    }

    public static class QuickEditorPathStaticAPI
    {
        public static readonly string AssetPathNodeName = "Assets";

        public static string AssetsPath = string.Empty;
        public static string PersistentDataPath = string.Empty;
        public static string ProjectPath = string.Empty;
        public static string ResourcesPath = string.Empty;
        public static string StreamingAsstesPath = string.Empty;
        public static string TemporaryCachePath = string.Empty;

        static QuickEditorPathStaticAPI()
        {
            AssetsPath = Application.dataPath;
            PersistentDataPath = Application.persistentDataPath;
            ProjectPath = string.Format("{0}/", Path.GetDirectoryName(AssetsPath));
            ResourcesPath = AssetsPath + "/Resources";
            StreamingAsstesPath = Application.streamingAssetsPath;
            TemporaryCachePath = Application.temporaryCachePath;
        }

        public static string SelectionAssetPath
        {
            get
            {
                string selectionpath = "Assets";
                int length = Selection.assetGUIDs.Length;
                if (length >= 1)
                {
                    selectionpath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                }
                return selectionpath;
            }
        }

        /// <summary>
        /// 返回ScriptableObject所在路径
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public static string GetScriptableObjectPath(this ScriptableObject asset)
        {
            string path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(asset));
            return path.Substring(0, path.LastIndexOf("/"));
        }

        public static string GetProjectPath(this string srcName)
        {
            if (srcName.Equals(string.Empty))
            {
                return ProjectPath;
            }
            return Combine(ProjectPath, srcName);
        }

        public static string GetAssetPath(this string assetName)
        {
            if (assetName.Equals(string.Empty))
            {
                return "Assets/";
            }
            return Combine("Assets/", assetName);
        }

        /// <summary>
        /// Convert global path to relative
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GlobalPathToRelative(this string path)
        {
            if (path.Equals(string.Empty)) { return "Assets/"; }
            if (path.StartsWith(Application.dataPath))
                return "Assets" + path.Substring(Application.dataPath.Length);
            else
                throw new ArgumentException("Incorrect path. Path doed not contain Application.datapath");
        }

        /// <summary>
        /// Convert relative path to global
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string RelativePathToGlobal(this string path)
        {
            return Combine(Application.dataPath, path);
        }

        /// <summary>
        /// Convert path from unix style to windows
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string UnixToWindowsPath(this string path)
        {
            return path.Replace("/", "\\");
        }

        /// <summary>
        /// Convert path from windows style to unix
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string WindowsToUnixPath(this string path)
        {
            return path.Replace("\\", "/");
        }

        /// <summary>　　
        /// 格式化路径成Asset的标准格式　
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string FormatAssetPath(string filePath)
        {
            var newFilePath1 = filePath.Replace("\\", "/");
            var newFilePath2 = newFilePath1.Replace("//", "/").Trim();
            newFilePath2 = newFilePath2.Replace("///", "/").Trim();
            newFilePath2 = newFilePath2.Replace("\\\\", "/").Trim();
            return newFilePath2;
        }

        private static string FormatPath(string path)
        {
            return path.Substring(path.IndexOf("Assets", StringComparison.Ordinal));
        }

        #region C# API 重写

        public static string Combine(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }

        public static string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path);
        }

        public static string GetExtension(string path)
        {
            return Path.GetExtension(path);
        }

        public static string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }

        public static string GetFileNameWithoutExtension(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        public static string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }

        #endregion C# API 重写
    }
}
#endif