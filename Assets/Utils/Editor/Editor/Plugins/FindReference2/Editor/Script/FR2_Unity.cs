#if UNITY_5_3_OR_NEWER
#define UNITY_4_3_OR_NEWER
#define UNITY_4_4_OR_NEWER
#define UNITY_4_5_OR_NEWER
#define UNITY_4_6_OR_NEWER
#define UNITY_4_7_OR_NEWER
#define UNITY_5_0_OR_NEWER
#define UNITY_5_1_OR_NEWER
#define UNITY_5_2_OR_NEWER
#else
    #if UNITY_5
	    #define UNITY_4_3_OR_NEWER
        #define UNITY_4_4_OR_NEWER
        #define UNITY_4_5_OR_NEWER
        #define UNITY_4_6_OR_NEWER
        #define UNITY_4_7_OR_NEWER
	
        #if UNITY_5_0 
            #define UNITY_5_0_OR_NEWER
	    #elif UNITY_5_1
		    #define UNITY_5_0_OR_NEWER
		    #define UNITY_5_1_OR_NEWER
	    #elif UNITY_5_2
		    #define UNITY_5_0_OR_NEWER
		    #define UNITY_5_1_OR_NEWER
		    #define UNITY_5_2_OR_NEWER
	    #endif
    #else
        #if UNITY_4_3
            #define UNITY_4_3_OR_NEWER
        #elif UNITY_4_4
            #define UNITY_4_3_OR_NEWER
            #define UNITY_4_4_OR_NEWER
        #elif UNITY_4_5    
		    #define UNITY_4_3_OR_NEWER
            #define UNITY_4_4_OR_NEWER
            #define UNITY_4_5_OR_NEWER
        #elif UNITY_4_6
		    #define UNITY_4_3_OR_NEWER
            #define UNITY_4_4_OR_NEWER
            #define UNITY_4_5_OR_NEWER
            #define UNITY_4_6_OR_NEWER
        #elif UNITY_4_7
		    #define UNITY_4_3_OR_NEWER
            #define UNITY_4_4_OR_NEWER
            #define UNITY_4_5_OR_NEWER
            #define UNITY_4_6_OR_NEWER
            #define UNITY_4_7_OR_NEWER
        #endif
    #endif
#endif


#if UNITY_5_3_OR_NEWER
#define UNITY_SCENE_MANAGER
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_SCENE_MANAGER
using UnityEngine.SceneManagement;

#endif
namespace vietlabs.fr2
{
	public class FR2_Unity
	{
		internal static readonly AssetType[] FILTERS =
		{
			new AssetType("Scene", ".unity"),
			new AssetType("Prefab", ".prefab"),
			new AssetType("Model", ".3df", ".3dm", ".3dmf", ".3dv", ".3dx", ".c5d", ".lwo", ".lws", ".ma", ".mb",
				".mesh", ".vrl", ".wrl", ".wrz", ".fbx", ".dae", ".3ds", ".dxf", ".obj", ".skp", ".max", ".blend"),
			new AssetType("Material", ".mat", ".cubemap", ".physicsmaterial"),
			new AssetType("Texture", ".ai", ".apng", ".png", ".bmp", ".cdr", ".dib", ".eps", ".exif", ".ico", ".icon",
				".j", ".j2c", ".j2k", ".jas", ".jiff", ".jng", ".jp2", ".jpc", ".jpe", ".jpeg", ".jpf", ".jpg", "jpw",
				"jpx", "jtf", ".mac", ".omf", ".qif", ".qti", "qtif", ".tex", ".tfw", ".tga", ".tif", ".tiff", ".wmf",
				".psd", ".exr", ".rendertexture"),
			new AssetType("Video", ".asf", ".asx", ".avi", ".dat", ".divx", ".dvx", ".mlv", ".m2l", ".m2t", ".m2ts",
				".m2v", ".m4e", ".m4v", "mjp", ".mov", ".movie", ".mp21", ".mp4", ".mpe", ".mpeg", ".mpg", ".mpv2",
				".ogm", ".qt", ".rm", ".rmvb", ".wmv", ".xvid", ".flv"),
			new AssetType("Audio", ".mp3", ".wav", ".ogg", ".aif", ".aiff", ".mod", ".it", ".s3m", ".xm"),
			new AssetType("Script", ".cs", ".js", ".boo"),
			new AssetType("Text", ".txt", ".json", ".xml", ".bytes", ".sql"),
			new AssetType("Shader", ".shader", ".cginc"),
			new AssetType("Animation", ".anim", ".controller", ".overridecontroller", ".mask"),
			new AssetType("Unity Asset", ".asset", ".guiskin", ".flare", ".fontsettings", ".prefs"),
			new AssetType("Others") //
		};

		public static string[] Selection_AssetGUIDs
		{
			get
			{
#if UNITY_5_0_OR_NEWER
				return Selection.assetGUIDs;
#else
			var mInfo =
 typeof(Selection).GetProperty("assetGUIDs", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (mInfo != null){
				return (string[]) mInfo.GetValue(null, null);
			}
			Debug.LogWarning("Unity changed ! Selection.assetGUIDs not found !");
		    return new string[0];
#endif
			}
		}

		//private static Texture2D _whiteTexture;
		//public static Texture2D whiteTexture {
		//	get {
		//		return EditorGUIUtility.whiteTexture;

		//		#if UNITY_5_0_OR_NEWER
		//		return EditorGUIUtility.whiteTexture;
		//		#else
		//		if (_whiteTexture != null) return _whiteTexture;
		//		_whiteTexture = new Texture2D(1,1, TextureFormat.RGBA32, false);
		//        _whiteTexture.SetPixel(0, 0, Color.white);
		//		_whiteTexture.hideFlags = HideFlags.DontSave;
		//		return _whiteTexture;
		//		#endif
		//	}
		//}

		public static T LoadAssetAtPath<T>(string path) where T : Object
		{
#if UNITY_5_1_OR_NEWER
			return AssetDatabase.LoadAssetAtPath<T>(path);
#else
			return (T)AssetDatabase.LoadAssetAtPath(path, typeof(T));
#endif
		}

		public static void SetWindowTitle(EditorWindow window, string title)
		{
#if UNITY_5_1_OR_NEWER
			window.titleContent = new GUIContent(title);
#else
	        window.title = title;
#endif
		}

		public static void GetCompilingPhase(string path, out bool isPlugin, out bool isEditor)
		{
#if (UNITY_5_2_0 || UNITY_5_2_1) && !UNITY_5_2_OR_NEWER
			bool oldSystem = true;
#else
			var oldSystem = false;
#endif

			// ---- Old system: Editor for the plugin should be Plugins/Editor
			if (oldSystem)
			{
				bool isPluginEditor = path.StartsWith("Assets/Plugins/Editor/", StringComparison.Ordinal)
				                      || path.StartsWith("Assets/Standard Assets/Editor/", StringComparison.Ordinal)
				                      || path.StartsWith("Assets/Pro Standard Assets/Editor/",
					                      StringComparison.Ordinal);

				if (isPluginEditor)
				{
					isPlugin = true;
					isEditor = true;
					return;
				}
			}

			isPlugin = path.StartsWith("Assets/Plugins/", StringComparison.Ordinal)
			           || path.StartsWith("Assets/Standard Assets/", StringComparison.Ordinal)
			           || path.StartsWith("Assets/Pro Standard Assets/", StringComparison.Ordinal);

			isEditor = oldSystem && isPlugin ? false : path.Contains("/Editor/");
		}

		public static T LoadAssetWithGUID<T>(string guid) where T : Object
		{
			if (string.IsNullOrEmpty(guid))
			{
				return null;
			}

			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(path))
			{
				return null;
			}

#if UNITY_5_1_OR_NEWER
			return AssetDatabase.LoadAssetAtPath<T>(path);
#else
			return (T)AssetDatabase.LoadAssetAtPath(path, typeof(T));
#endif
		}

		public static void UnloadUnusedAssets()
		{
#if UNITY_5_0_OR_NEWER
			EditorUtility.UnloadUnusedAssetsImmediate();
#else
			EditorUtility.UnloadUnusedAssets();
#endif
			Resources.UnloadUnusedAssets();
		}

		internal static int Epoch(DateTime time)
		{
			return (int) (time.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
		}

		internal static bool DrawToggle(ref bool v, string label)
		{
			bool v1 = GUILayout.Toggle(v, label);
			if (v1 != v)
			{
				v = v1;
				return true;
			}

			return false;
		}

		internal static bool DrawToggleToolbar(ref bool v, string label, float width)
		{
			bool v1 = GUILayout.Toggle(v, label, EditorStyles.toolbarButton, GUILayout.Width(width));
			if (v1 != v)
			{
				v = v1;
				return true;
			}

			return false;
		}

		internal static bool DrawToggleToolbar(ref bool v, GUIContent icon, float width)
		{
			bool v1 = GUILayout.Toggle(v, icon, EditorStyles.toolbarButton, GUILayout.Width(width));
			if (v1 != v)
			{
				v = v1;
				return true;
			}

			return false;
		}

		internal static bool DrawToggle(bool v, string label, Action<bool> setter)
		{
			bool v1 = GUILayout.Toggle(v, label);
			if (v1 != v)
			{
				if (setter != null)
				{
					setter(v1);
				}

				return true;
			}

			return false;
		}

		internal static EditorWindow FindEditor(string className)
		{
			EditorWindow[] list = Resources.FindObjectsOfTypeAll<EditorWindow>();
			foreach (EditorWindow item in list)
			{
				if (item.GetType().FullName == className)
				{
					return item;
				}
			}

			return null;
		}

		internal static void RepaintAllEditor(string className)
		{
			EditorWindow[] list = Resources.FindObjectsOfTypeAll<EditorWindow>();

			foreach (EditorWindow item in list)
			{
#if FR2_DEV
			Debug.Log(item.GetType().FullName);
#endif

				if (item.GetType().FullName != className)
				{
					continue;
				}

				item.Repaint();
			}
		}

		internal static void RepaintProjectWindows()
		{
			RepaintAllEditor("UnityEditor.ProjectBrowser");
		}

		internal static void RepaintFR2Windows()
		{
			RepaintAllEditor("vietlabs.fr2.FR2_Window");
		}

		internal static void ExportSelection()
		{
			Type packageExportT = null;

			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				packageExportT = assembly.GetType("UnityEditor.PackageExport");
				if (packageExportT != null)
				{
					break;
				}
			}

			if (packageExportT == null)
			{
				Debug.LogWarning("Export Package Error : UnityEditor.PackageExport not found !");
				return;
			}

			EditorWindow panel = EditorWindow.GetWindow(packageExportT, true, "Exporting package");
#if UNITY_5_2_OR_NEWER
			var prop = "m_IncludeDependencies";
#else
			var prop = "m_bIncludeDependencies";
#endif

			FieldInfo fieldInfo = packageExportT.GetField(prop, BindingFlags.NonPublic | BindingFlags.Instance);
			if (fieldInfo == null)
			{
				Debug.LogWarning("Export Package error : " + prop + " not found !");
				return;
			}

			MethodInfo methodInfo =
				packageExportT.GetMethod("BuildAssetList", BindingFlags.NonPublic | BindingFlags.Instance);
			if (methodInfo == null)
			{
				Debug.LogWarning("Export Package error : BuildAssetList method not found !");
				return;
			}

			fieldInfo.SetValue(panel, false);
			methodInfo.Invoke(panel, null);
			panel.Repaint();
		}


		public static Type GetType(string typeName)
		{
			Type type = Type.GetType(typeName);
			if (type != null)
			{
				return type;
			}

			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
			{
				type = a.GetType(typeName);
				if (type != null)
				{
					return type;
				}
			}

			return null;
		}

		public static IEnumerable<Transform> GetAllChild(Transform root)
		{
			yield return root;
			if (root.childCount <= 0)
			{
				yield break;
			}

			for (var i = 0; i < root.childCount; i++)
			{
				foreach (Transform item in GetAllChild(root.GetChild(i)))
				{
					yield return item;
				}
			}
		}

		public static IEnumerable<GameObject> getAllObjsInCurScene()
		{
#if UNITY_SCENE_MANAGER
			for (var j = 0; j < SceneManager.sceneCount; j++)
			{
				Scene scene = SceneManager.GetSceneAt(j);
				foreach (GameObject item in GetGameObjectsInScene(scene))
				{
					yield return item;
				}
			}

			if (EditorApplication.isPlaying)
			{
				//dont destroy scene
				GameObject temp = null;
				try
				{
					temp = new GameObject();
					Object.DontDestroyOnLoad(temp);
					Scene dontDestroyOnLoad = temp.scene;
					Object.DestroyImmediate(temp);
					temp = null;

					foreach (GameObject item in GetGameObjectsInScene(dontDestroyOnLoad))
					{
						yield return item;
					}
				}
				finally
				{
					if (temp != null)
					{
						Object.DestroyImmediate(temp);
					}
				}
			}
#else
			foreach (Transform obj in Resources.FindObjectsOfTypeAll(typeof(Transform)))
            {
				GameObject o = obj.gameObject;
               yield return o;
            }
#endif
		}
#if UNITY_SCENE_MANAGER
		private static IEnumerable<GameObject> GetGameObjectsInScene(Scene scene)
		{
			var rootObjects = new List<GameObject>();
			if (!scene.isLoaded)
			{
				yield break;
			}

			scene.GetRootGameObjects(rootObjects);

			// iterate root objects and do something
			for (var i = 0; i < rootObjects.Count; ++i)
			{
				GameObject gameObject = rootObjects[i];

				foreach (GameObject item in getAllChild(gameObject))
				{
					yield return item;
				}

				yield return gameObject;
			}
		}
#endif
		public static IEnumerable<GameObject> getAllChild(GameObject target, bool returnMe = false)
		{
			if (returnMe)
			{
				yield return target;
			}

			if (target.transform.childCount > 0)
			{
				for (var i = 0; i < target.transform.childCount; i++)
				{
					yield return target.transform.GetChild(i).gameObject;
					foreach (GameObject item in getAllChild(target.transform.GetChild(i).gameObject, false))
					{
						yield return item;
					}
				}
			}
		}

		public static IEnumerable<Object> GetAllRefObjects(GameObject obj)
		{
			Component[] components = obj.GetComponents<Component>();
			foreach (Component com in components)
			{
				if (com == null)
				{
					continue;
				}

				foreach (var anyObject in FR2_Helper.GetFromPlayableGraph(com))
				{
					yield return anyObject;
				}

				var serialized = new SerializedObject(com);
				SerializedProperty it = serialized.GetIterator().Copy();
				while (it.NextVisible(true))
				{
					if (it.propertyType != SerializedPropertyType.ObjectReference)
					{
						continue;
					}

					if (it.objectReferenceValue == null)
					{
						continue;
					}

					yield return it.objectReferenceValue;
				}
			}
		}

		public static int StringMatch(string pattern, string input)
		{
			if (input == pattern)
			{
				return int.MaxValue;
			}

			if (input.Contains(pattern))
			{
				return int.MaxValue - 1;
			}

			var pidx = 0;
			var score = 0;
			var tokenScore = 0;

			for (var i = 0; i < input.Length; i++)
			{
				char ch = input[i];
				if (ch == pattern[pidx])
				{
					tokenScore += tokenScore + 1; //increasing score for continuos token
					pidx++;
					if (pidx >= pattern.Length)
					{
						break;
					}
				}
				else
				{
					tokenScore = 0;
				}

				score += tokenScore;
			}

			return score;
		}

		public static int GetIndex(string ext)
		{
			for (var i = 0; i < FILTERS.Length - 1; i++)
			{
				if (FILTERS[i].extension.Contains(ext))
				{
					return i;
				}
			}

			return FILTERS.Length - 1; //Others
		}

		public static void GuiLine(int i_height = 1)

		{
			Rect rect = EditorGUILayout.GetControlRect(false, i_height);

			rect.height = i_height;

			EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
		}

		public static bool IsInAsset(GameObject obj)
		{
			//#if UNITY_5_3_OR_NEWER
			// this not working in new empty created scene
			//return string.IsNullOrEmpty(obj.scene.name);
			//#else
			return !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj));
			//#endif
		}

		public static string GetPrefabParent(Object obj)
		{
#if UNITY_2018_2_OR_NEWER
			string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
			return AssetDatabase.AssetPathToGUID(prefabPath);
#else
			var prefab = PrefabUtility.GetPrefabParent(obj);
			return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
#endif
		}

		public static string GetGameObjectPath(GameObject obj, bool includeMe = true)
		{
			if (obj == null)
			{
				return string.Empty;
			}

			string path = includeMe ? "/" + obj.name : "/";
			while (obj.transform.parent != null)
			{
				obj = obj.transform.parent.gameObject;
				path = "/" + obj.name + path;
			}

			path = path.TrimStart('/');
			return path;
		}

		public static bool CheckIsPrefab(GameObject obj)
		{
#if UNITY_2018_3_OR_NEWER
			// var t = PrefabUtility.GetPrefabAssetType(obj);
			// var isPrefab = (t != PrefabAssetType.NotAPrefab) && (t != PrefabAssetType.MissingAsset);
			return PrefabUtility.IsAnyPrefabInstanceRoot(obj);
#else
			return PrefabUtility.GetPrefabType(obj) != PrefabType.None;
#endif
		}

		public static TerrainTextureData[] GetTerrainTextureDatas(TerrainData data)
		{
#if UNITY_2018_3_OR_NEWER
			var arr = new TerrainTextureData[data.terrainLayers.Length];
			for (var i = 0; i < data.terrainLayers.Length; i++)
			{
				TerrainLayer layer = data.terrainLayers[i];
				arr[i] = new TerrainTextureData
				(
					layer.normalMapTexture,
					layer.maskMapTexture,
					layer.diffuseTexture
				);
			}

			return arr;
#else
			var arr = new TerrainTextureData[data.splatPrototypes.Length];
			for(int i = 0; i < data.splatPrototypes.Length; i++)
			{
				var layer = data.splatPrototypes[i];
				arr[i] = new TerrainTextureData
				(
					layer.normalMap,
					layer.texture
				);
			}
			return arr;
#endif
		}

		public static int ReplaceTerrainTextureDatas(TerrainData terrain, Texture2D fromObj, Texture2D toObj)
		{
			var found = 0;
#if UNITY_2018_3_OR_NEWER
			TerrainLayer[] arr3 = terrain.terrainLayers;
			for (var i = 0; i < arr3.Length; i++)
			{
				if (arr3[i].normalMapTexture == fromObj)
				{
					found++;
					arr3[i].normalMapTexture = toObj;
				}

				if (arr3[i].maskMapTexture == fromObj)
				{
					found++;
					arr3[i].maskMapTexture = toObj;
				}

				if (arr3[i].diffuseTexture == fromObj)
				{
					found++;
					arr3[i].diffuseTexture = toObj;
				}
			}

			terrain.terrainLayers = arr3;
#else
                    var arr3 = terrain.splatPrototypes;
                    for (var i = 0; i < arr3.Length; i++)
                    {
                        if (arr3[i].texture ==  fromObj)
                        {
                            found++;
                            arr3[i].texture = toObj;
                        }

                        if (arr3[i].normalMap ==  fromObj)
                        {
                            found++;
                            arr3[i].normalMap = toObj;
                        }
                    }

                    terrain.splatPrototypes = arr3;
#endif
			return found;
		}

		public class TerrainTextureData
		{
			public Texture2D[] textures;

			public TerrainTextureData(params Texture2D[] param)
			{
				var count = 0;
				if (param != null)
				{
					count = param.Length;
				}

				textures = new Texture2D[count];
				for (var i = 0; i < count; i++)
				{
					textures[i] = param[i];
				}
			}
		}
	}
}