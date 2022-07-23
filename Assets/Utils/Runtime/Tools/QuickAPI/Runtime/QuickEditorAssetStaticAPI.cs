#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

public static class AssetDatabaseExtension
{
	public static List<T> FindAssetsOfType<T>(this UnityEditor.AssetDatabase any)
		where T : UnityEngine.Object
	{
		return QuickEditor.Core.QuickEditorAssetStaticAPI.LoadAssetsOfType<T>();
	}
	
	public static List<System.Object> FindAssetsOfType(this UnityEditor.AssetDatabase any, System.Type typeToLookFor)
	{
		return QuickEditor.Core.QuickEditorAssetStaticAPI.FindAssetOfType(typeToLookFor);
	}
}

namespace QuickEditor.Core
{

    public class QuickEditorAssetStaticAPI
    {
        public enum AssetPathMode
        {
            SelectionAssetPath,
            ScriptableObjectAssetPath,
        }

        public static T CreateAssetProjectWindow<T>(string filename) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            ProjectWindowUtil.CreateAsset(asset, filename + ".asset");
            return asset;
        }

        public static T CreateAsset<T>(AssetPathMode type = AssetPathMode.ScriptableObjectAssetPath) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            return CreateAsset(asset, (type == AssetPathMode.ScriptableObjectAssetPath ? asset.GetScriptableObjectPath() : QuickEditorPathStaticAPI.SelectionAssetPath)) as T;
        }

        public static T CreateAsset<T>(string targetPath) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();

            return CreateAsset(asset, targetPath) as T;
        }

        public static UnityEngine.Object CreateAsset(ScriptableObject asset, AssetPathMode type = AssetPathMode.ScriptableObjectAssetPath)
        {
            return CreateAsset(asset, (type == AssetPathMode.ScriptableObjectAssetPath ? asset.GetScriptableObjectPath() : QuickEditorPathStaticAPI.SelectionAssetPath));
        }

        public static UnityEngine.Object CreateAsset(UnityEngine.Object asset, string targetPath)
        {
            string fileName = AssetDatabase.GenerateUniqueAssetPath(targetPath + "/" + asset.GetType().Name + ".asset");

            BuildAsset(asset, fileName);
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            return asset;
        }

        public static void BuildAsset(UnityEngine.Object asset, string path)
        {
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        public static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var temp = LoadAsset<T>(path);
            return temp ?? CreateAsset<T>(path);
        }

        public static bool DeleteAsset(string path)
        {
            var file = new FileInfo(path);
            if (!file.Exists)
                return false;
            file.Delete();
            return true;
        }

        public static UnityEngine.Object ObjectFromGUID(string guid)
        {
            return AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(UnityEngine.Object)) as UnityEngine.Object;
        }

        public static T InstanciateScriptableObject<T>(string path) where T : ScriptableObject
        {
            return InstanciateScriptableObject<T>(path, typeof(T).Name);
        }

        public static T InstanciateScriptableObject<T>(string path, string fileName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            var uniquePath = AssetDatabase.GenerateUniqueAssetPath(path + fileName + ".asset");
            BuildAsset(asset, uniquePath);
            var final = AssetDatabase.LoadAssetAtPath(uniquePath, typeof(T)) as T;
            return final;
        }

        public static T LoadOrCreateAssetFromFindAssets<T>(bool mFocusProjectWindow = true) where T : ScriptableObject
        {
            T asset = default(T);
            asset = (T)AssetDatabase.LoadAssetAtPath(FindAssetPath<T>(), typeof(T));
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                string fileName = AssetDatabase.GenerateUniqueAssetPath(asset.GetScriptableObjectPath() + "/Resources/" + asset.GetType().Name + ".asset");
                BuildAsset(asset, fileName);
            }
            if (mFocusProjectWindow)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
            }
            return asset;
        }

        #region FindAssets
		
        /// <summary>
        /// Returns GUIDS
        /// </summary>
        /// <param name="searchInFolders">Relative to the Assets Assets/Module/...</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string[] FindAssets<T>(string[] searchInFolders = null) where T : UnityEngine.Object
        {
	        return FindAssets(string.Format("t:{0}", typeof(T).Name), searchInFolders);
        }
        
        /// <summary>
        /// Returns GUIDS
        /// </summary>
        /// <param name="searchInFolders">Relative to the Assets Assets/Module/...</param>
        public static string[] FindAssets(string filter, string[] searchInFolders = null)
        {
            return (searchInFolders == null || searchInFolders.Length < 1) ? AssetDatabase.FindAssets(filter) : AssetDatabase.FindAssets(filter, searchInFolders);
        }

        public static string FindAssetPath<T>() where T : UnityEngine.Object
        {
            string[] guids = FindAssets<T>();

            if (guids.Length > 0)
            {
                if (guids.Length > 1)
                {
                    Debug.LogWarning("More than one instance of " + typeof(T).Name + " exists! Using the first occurance.");
                }
                return AssetDatabase.GUIDToAssetPath(guids[0]);
            }
            Debug.LogError("File not found " + typeof(T).Name);
            return string.Empty;
        }
        
        public static List<System.Object> FindAssetOfType(Type typeToLookFor) 
        {
	        List<System.Object> assets = new List<System.Object>();
	        string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeToLookFor));
	        for( int i = 0; i < guids.Length; i++ )
	        {
		        string assetPath = AssetDatabase.GUIDToAssetPath( guids[i] );
		        System.Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeToLookFor);
		        if( asset != null )
		        {
			        assets.Add(asset);
		        }
	        }
	        return assets;
        }

        #endregion FindAssets

        /// <summary>
        /// Searches the whole project and attempts to load the first asset matching name (excluding extension).
        /// </summary>
        /// <param name="name">Name of the file without extension</param>
        public static T LoadAssetWithName<T>(string name) where T : UnityEngine.Object
        {
            T asset = null;

            try
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(name)[0]);
                asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("Could not load asset with name " + name + " | Error: " + ex.Message);
            }

            return asset;
        }

        /// <summary>
        /// Searches the whole projects for assets of type T and returns them as a list
        /// </summary>
        /// <param name="searchInFolders">Relative to the Assets Assets/Module/...</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> LoadAssetsOfType<T>(string[] searchInFolders = null) where T : UnityEngine.Object
        {
            List<T> assets = new List<T>();
            string filter = typeof(T).ToString().Replace("UnityEngine.", string.Empty);
            bool serachInFolder = !(searchInFolders == null || searchInFolders.Length < 1);
            string[] guids = serachInFolder? AssetDatabase.FindAssets(string.Format("t:{0}",filter),searchInFolders): AssetDatabase.FindAssets(string.Format("t:{0}",filter));

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
            return assets;
        }

        public static List<T> LoadAssetsOfType<T>(string folderToSearchIn) where T : UnityEngine.Object
        {
	        return LoadAssetsOfType<T>(new string[]{folderToSearchIn});
        }


    }
}
#endif