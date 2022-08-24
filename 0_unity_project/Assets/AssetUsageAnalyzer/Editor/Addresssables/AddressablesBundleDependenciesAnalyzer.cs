using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Neueec.AssetUsagesTool.Addresssables
{
	internal class AddressablesBundleDependenciesAnalyzer : CustomBundleRuleBase
	{
		public override bool CanFix => false;
		public override string ruleName => "Find bundles dependencies";
		private Dictionary<string, string> _bundleNames = new Dictionary<string, string>();
		private List<AnalyzeResult> _results = new List<AnalyzeResult>();

		public override List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
		{
			ClearAnalysis();

			if (!BuildUtility.CheckModifiedScenesAndAskToSave())
			{
				_results.Add(new AnalyzeResult {resultName = ruleName + "Cannot run Analyze with unsaved scenes"});
				return _results;
			}

			CalculateInputDefinitions(settings);
			var context = GetBuildContext(settings);
			RefreshBuild(context);
			ConvertBundleNamesToGroupNames(context);
			CollectBundleNames();
			CollectResults(settings);

			if (_results.Count == 0)
				_results.Add(new AnalyzeResult() {resultName = "Something went wrong"});

			_results = _results.OrderByDescending(x => x.severity).ToList();
			return _results;
		}

		private void CollectResults(AddressableAssetSettings settings)
		{
			List<AddressableAssetEntry> allEntries = new List<AddressableAssetEntry>();
			settings.GetAllAssets(allEntries, true);
			foreach (var entry in allEntries)
			{
				var deps = AssetDatabase.GetDependencies(entry.AssetPath);
				foreach (var dep in deps)
				{
					AddResultWithDependencies(entry, dep);
				}
			}
		}

		private void AddResultWithDependencies(AddressableAssetEntry entry, string dep)
		{
			_bundleNames.TryGetValue(entry.AssetPath, out var entryBundleName);
			_bundleNames.TryGetValue(dep, out var depBundleName);
			depBundleName ??= string.Empty;
			entryBundleName ??= entry.parentGroup.Name;

			if (entryBundleName == depBundleName)
			{
				return; // skip self
			}

			if (string.IsNullOrEmpty(depBundleName))
			{
				return; // skip dependencies outside bundles
			}

			bool allowableDep = depBundleName.StartsWith("commondata") || depBundleName.StartsWith("shareddata") ||
			                    depBundleName == string.Empty || (entryBundleName.StartsWith("map") &&
			                                                      depBundleName.StartsWith("mapfeature"));
			depBundleName = kDelimiter + depBundleName;

			var result = new AnalyzeResult()
			{
				resultName = entryBundleName + depBundleName + kDelimiter + dep,
				severity = allowableDep ? MessageType.None : MessageType.Warning
			};

			_results.Add(result);
		}

		private void CollectBundleNames()
		{
			_bundleNames = (from bundleBuild in m_AllBundleInputDefs
					let bundleName = bundleBuild.assetBundleName
					from asset in bundleBuild.assetNames
					select new KeyValuePair<string, string>(asset, bundleName))
				.ToDictionary(pair => pair.Key, v => v.Value);
		}

		public override void ClearAnalysis()
		{
			base.ClearAnalysis();
			_bundleNames.Clear();
			_results.Clear();
		}

		[InitializeOnLoad]
		private class RegisterMyRule
		{
			static RegisterMyRule()
			{
				AnalyzeWindow.RegisterNewRule<AddressablesBundleDependenciesAnalyzer>();
			}
		}
	}
}