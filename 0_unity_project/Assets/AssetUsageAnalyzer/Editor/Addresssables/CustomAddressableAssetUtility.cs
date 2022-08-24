using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Utilities;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.VersionControl;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neueec.AssetUsagesTool.Addresssables
{
	/// <summary>
	/// This class is Copy of UnityEditor.AddressableAssets.Settings.CustomAddressableAssetUtility
	/// </summary>
	internal static class CustomAddressableAssetUtility
	{
		internal static bool IsInResources(string path)
		{
			return path.Replace('\\', '/').ToLower().Contains("/resources/");
		}

		internal static bool TryGetPathAndGUIDFromTarget(Object target, out string path, out string guid)
		{
			guid = string.Empty;
			path = string.Empty;
			if (target == null)
				return false;
			path = AssetDatabase.GetAssetOrScenePath(target);
			if (!IsPathValidForEntry(path))
				return false;
			if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out guid, out long _))
				return false;
			return true;
		}

		static HashSet<string> excludedExtensions =
			new HashSet<string>(new string[] {".cs", ".js", ".boo", ".exe", ".dll", ".meta"});

		internal static bool IsPathValidForEntry(string path)
		{
			if (string.IsNullOrEmpty(path))
				return false;
			if (!path.StartsWith("assets", StringComparison.OrdinalIgnoreCase) && !IsPathValidPackageAsset(path))
				return false;
			if (path == CommonStrings.UnityEditorResourcePath ||
			    path == CommonStrings.UnityDefaultResourcePath ||
			    path == CommonStrings.UnityBuiltInExtraPath)
				return false;
			if (path.EndsWith("/Editor") || path.Contains("/Editor/"))
				return false;
			if (path == "Assets")
				return false;
			var settings = AddressableAssetSettingsDefaultObject.SettingsExists
				? AddressableAssetSettingsDefaultObject.Settings
				: null;
			if (settings != null && path.StartsWith(settings.ConfigFolder) ||
			    path.StartsWith(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder))
				return false;
			return !excludedExtensions.Contains(Path.GetExtension(path));
		}

		internal static bool IsPathValidPackageAsset(string path)
		{
			string convertPath = path.ToLower().Replace("\\", "/");
			string[] splitPath = convertPath.Split('/');

			if (splitPath.Length < 3)
				return false;
			if (splitPath[0] != "packages")
				return false;
			if (splitPath[2] == "package.json")
				return false;
			return true;
		}

		static HashSet<Type> validTypes = new HashSet<Type>();

		private static Type MapEditorTypeToRuntimeTypeInternal(Type t)
		{
			if (t == typeof(UnityEditor.Animations.AnimatorController))
				return typeof(RuntimeAnimatorController);
			if (t == typeof(UnityEditor.SceneAsset))
				return typeof(UnityEngine.ResourceManagement.ResourceProviders.SceneInstance);
			if (t.FullName == "UnityEditor.Audio.AudioMixerController")
				return typeof(UnityEngine.Audio.AudioMixer);
			if (t.FullName == "UnityEditor.Audio.AudioMixerGroupController")
				return typeof(UnityEngine.Audio.AudioMixerGroup);
			return null;
		}

		internal static void ConvertAssetBundlesToAddressables()
		{
			AssetDatabase.RemoveUnusedAssetBundleNames();
			var bundleList = AssetDatabase.GetAllAssetBundleNames();

			float fullCount = bundleList.Length;
			int currCount = 0;

			var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
			foreach (var bundle in bundleList)
			{
				if (EditorUtility.DisplayCancelableProgressBar("Converting Legacy Asset Bundles", bundle,
					currCount / fullCount))
					break;

				currCount++;
				var group = settings.CreateGroup(bundle, false, false, false, null);
				var schema = group.AddSchema<BundledAssetGroupSchema>();
				schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
				schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
				schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
				group.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;

				var assetList = AssetDatabase.GetAssetPathsFromAssetBundle(bundle);

				foreach (var asset in assetList)
				{
					var guid = AssetDatabase.AssetPathToGUID(asset);
					settings.CreateOrMoveEntry(guid, group, false, false);
					var imp = AssetImporter.GetAtPath(asset);
					if (imp != null)
						imp.SetAssetBundleNameAndVariant(string.Empty, string.Empty);
				}
			}

			if (fullCount > 0)
				settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
			EditorUtility.ClearProgressBar();
			AssetDatabase.RemoveUnusedAssetBundleNames();
		}

		/// <summary>
		/// Get all types that can be assigned to type T
		/// </summary>
		/// <typeparam name="T">The class type to use as the base class or interface for all found types.</typeparam>
		/// <returns>A list of types that are assignable to type T.  The results are cached.</returns>
		public static List<Type> GetTypes<T>()
		{
			return TypeManager<T>.Types;
		}

		/// <summary>
		/// Get all types that can be assigned to type rootType.
		/// </summary>
		/// <param name="rootType">The class type to use as the base class or interface for all found types.</param>
		/// <returns>A list of types that are assignable to type T.  The results are not cached.</returns>
		public static List<Type> GetTypes(Type rootType)
		{
			return TypeManager.GetManagerTypes(rootType);
		}

		private class TypeManager
		{
			public static List<Type> GetManagerTypes(Type rootType)
			{
				var types = new List<Type>();
				try
				{
					foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
					{
						if (a.IsDynamic)
							continue;
						foreach (var t in a.ExportedTypes)
						{
							if (t != rootType && rootType.IsAssignableFrom(t) && !t.IsAbstract)
								types.Add(t);
						}
					}
				}
				catch (Exception)
				{
					// ignored
				}

				return types;
			}
		}

		private class TypeManager<T> : TypeManager
		{
			// ReSharper disable once StaticMemberInGenericType
			static List<Type> s_Types;

			public static List<Type> Types
			{
				get
				{
					if (s_Types == null)
						s_Types = GetManagerTypes(typeof(T));

					return s_Types;
				}
			}
		}

		static Dictionary<Type, string> s_CachedDisplayNames = new Dictionary<Type, string>();

		internal static string GetCachedTypeDisplayName(Type type)
		{
			string result = "<none>";
			if (type != null)
			{
				if (!s_CachedDisplayNames.TryGetValue(type, out result))
				{
					var displayNameAtr = type.GetCustomAttribute<DisplayNameAttribute>();
					if (displayNameAtr != null)
					{
						result = (string) displayNameAtr.DisplayName;
					}
					else
						result = type.Name;

					s_CachedDisplayNames.Add(type, result);
				}
			}

			return result;
		}

		internal static bool IsUsingVCIntegration()
		{
			return Provider.isActive && Provider.enabled;
		}


		private static bool MakeAssetEditable(Asset asset)
		{
			if (!AssetDatabase.IsOpenForEdit(asset.path))
				return AssetDatabase.MakeEditable(asset.path);
			return false;
		}


		internal static ListRequest RequestPackageListAsync()
		{
#if !UNITY_2021_1_OR_NEWER
			return Client.List(true);
#endif
			return null;
		}

		internal static void RemoveCCDPackage()
		{
			var confirm = EditorUtility.DisplayDialog("Remove CCD Management SDK Package",
				"Are you sure you want to remove the CCD Management SDK package?", "Yes", "No");
			if (confirm)
			{
#if (ENABLE_CCD && UNITY_2019_4_OR_NEWER)
                Client.Remove(AddressableAssetSettings.kCCDPackageName);
                AddressableAssetSettingsDefaultObject.Settings.CCDEnabled = false;
#endif
			}
		}

		internal static string GetMd5Hash(string path)
		{
			string hashString;
			using (var md5 = MD5.Create())
			{
				using (var stream = File.OpenRead(path))
				{
					var hash = md5.ComputeHash(stream);
					hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
				}
			}

			return hashString;
		}


		internal static System.Threading.Tasks.Task ParallelForEachAsync<T>(this IEnumerable<T> source, int dop,
			Func<T, System.Threading.Tasks.Task> body)
		{
			async System.Threading.Tasks.Task AwaitPartition(IEnumerator<T> partition)
			{
				using (partition)
				{
					while (partition.MoveNext())
					{
						await body(partition.Current);
					}
				}
			}

			return System.Threading.Tasks.Task.WhenAll(
				Partitioner
					.Create(source)
					.GetPartitions(dop)
					.AsParallel()
					.Select(p => AwaitPartition(p)));
		}
	}
}