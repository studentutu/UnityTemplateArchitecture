﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace vietlabs.fr2
{
	public class FR2_Helper
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
		private static List<ObjectProviderFrom> typosOfProvidersForTracks = new List<ObjectProviderFrom>();

		public static IEnumerable<GameObject> getAllObjsInCurScene()
		{
			// foreach (GameObject obj in Object.FindObjectsOfType(typeof(GameObject)))
			// {
			//    yield return obj;
			// }


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
		}

		private static IEnumerable<GameObject> GetGameObjectsInScene(Scene scene)
		{
			var rootObjects = new List<GameObject>();
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

		public static IEnumerable<GameObject> getAllChild(GameObject target)
		{
			if (target.transform.childCount > 0)
			{
				for (var i = 0; i < target.transform.childCount; i++)
				{
					yield return target.transform.GetChild(i).gameObject;
					foreach (GameObject item in getAllChild(target.transform.GetChild(i).gameObject))
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

				foreach (var objToAddFromTimeline in GetFromPlayableGraph(com))
				{
					yield return objToAddFromTimeline;
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
		
		public static IEnumerable<Object> GetFromPlayableGraph(Component component)
		{
			var asTimeline = component as PlayableDirector;
			if (asTimeline != null)
			{
				UpdateAssemblyScriptUsage();
				var getTimelineAsset = asTimeline.playableAsset as TimelineAsset;
				foreach (var item in GetRefFromTimeline(getTimelineAsset))
				{
					yield return item;
				}
			}
		}

		private static IEnumerable<Object> GetRefFromTimeline(TimelineAsset timelineAsset)
		{
			if (timelineAsset == null)
			{
				yield break;
			}

			foreach (var track in timelineAsset.GetRootTracks())
			{
				foreach (var provider in typosOfProvidersForTracks)
				{
					if (provider != null)
					{
						foreach (var item in provider.GetObjectsFrom(track))
						{
							yield return item;
						}
					}
				}
			}
		}
		
		private static void UpdateAssemblyScriptUsage()
		{
			if (typosOfProvidersForTracks.Count == 0)
			{
				typosOfProvidersForTracks.Clear();
				foreach (Type type in 
					Assembly
						.GetAssembly(typeof(ObjectProviderFrom))
						.GetTypes()
						.Where(myType => myType.IsClass && 
						                 !myType.IsAbstract && 
						                 myType.IsSubclassOf(typeof(ObjectProviderFrom))))
				{
					typosOfProvidersForTracks
						.Add((ObjectProviderFrom)Activator.CreateInstance(type));
				}
			}
		}

		/// <summary>
		///  Generic Base CLass 
		/// </summary>
		public abstract class ObjectProviderFrom
		{
			public ObjectProviderFrom()
			{
			}

			public abstract IEnumerable<Object> GetObjectsFrom(TrackAsset track);
		}
		
		public class AudioClipFromTimeLineProvider : ObjectProviderFrom
		{
			public override IEnumerable<Object> GetObjectsFrom(TrackAsset track)
			{
				if (track is AudioTrack audioTrack)
				{
					foreach (var audioClip in audioTrack.GetClips())
					{
						var asAudioPlayableTrackCLip = audioClip.asset as AudioPlayableAsset;
						if (asAudioPlayableTrackCLip != null)
						{
							yield return asAudioPlayableTrackCLip.clip;
						}
					}
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

		public static string GetfileSizeString(long fileSize)
		{
			return fileSize <= 1024
				? fileSize + " B"
				: fileSize <= 1024 * 1024
					? Mathf.RoundToInt(fileSize / 1024f) + " KB"
					: Mathf.RoundToInt(fileSize / 1024f / 1024f) + " MB";
		}
	}
}