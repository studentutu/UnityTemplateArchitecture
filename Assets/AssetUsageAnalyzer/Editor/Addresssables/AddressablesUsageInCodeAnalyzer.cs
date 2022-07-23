using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace Neueec.AssetUsagesTool.Addresssables
{
	public class AddressablesUsageInCodeAnalyzer : AnalyzeRule
	{
		public override string ruleName => "Code Usage";
		public override bool CanFix => false;
		internal readonly List<AddressableAssetEntry> AssetEntries = new List<AddressableAssetEntry>();

		public override List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
		{
			CalculateInputDefinitions(settings);
			var codeUsage = AddressablesKeysFromCode();

			var output = new List<AnalyzeResult>(AssetEntries.Count);

			FindUnusedAssetsAndUsing(codeUsage, output);

			FindMissingAssets(codeUsage, output);

			return output;
		}

		private void FindUnusedAssetsAndUsing(List<UsageData> codeUsage, List<AnalyzeResult> output)
		{
			foreach (var assetEntry in AssetEntries)
			{
				var usage = codeUsage.Where(
						data =>
						{
							CutSpriteAtlasSuffix(data);

							var pattern = GetValidationCodeMatchPattern(data.AddressablesKey);

							return Regex.IsMatch(assetEntry.address, pattern, RegexOptions.Singleline);
						})
					.ToArray();

				if (usage.Length > 0)
				{
					foreach (var data in usage)
					{
						if (data.AddressablesKey.Contains("{0}"))
						{
							output.Add(new AnalyzeResult() { resultName = data.AddressablesKey + ": " + assetEntry.address + "  " + data });
						}
						else
						{
							output.Add(new AnalyzeResult() { resultName = assetEntry.address + ": " + data });
						}
					}
				}
				else
				{
					output.Add(new AnalyzeResult() { resultName = $"Not found usage: {assetEntry.address}", severity = MessageType.Warning });
				}
			}
		}

		private void FindMissingAssets(List<UsageData> codeUsage, List<AnalyzeResult> output)
		{
			foreach (var usageData in codeUsage)
			{
				CutSpriteAtlasSuffix(usageData);

				var pattern = GetValidationCodeMatchPattern(usageData.AddressablesKey);
				var addressableAssetEntry = AssetEntries.FirstOrDefault(entry => Regex.IsMatch(entry.address, pattern, RegexOptions.Singleline));
				var addressableAssetLabelEntry = AssetEntries.SelectMany(entry => entry.labels).FirstOrDefault(label => Regex.IsMatch(label, pattern, RegexOptions.Singleline));

				if (addressableAssetEntry == null && addressableAssetLabelEntry == null)
				{
					output.Add(new AnalyzeResult() { resultName = $"Missing asset or label: {usageData}", severity = MessageType.Error });
				}
			}
		}

		private string GetValidationCodeMatchPattern(string entry)
		{
			for (var i = 0; i < 5; i++)
			{
				var from = "{" + i + "}";
				entry = entry.Replace(from, ".*");
			}

			return entry;
		}

		private static void CutSpriteAtlasSuffix(UsageData data)
		{
			if (data.AddressablesKey.Contains('['))
			{
				data.AddressablesKey = data.AddressablesKey.Substring(0, data.AddressablesKey.IndexOf('['));
			}
		}

		private List<UsageData> AddressablesKeysFromCode()
		{
			var result = new List<UsageData>();

			result = AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetConstValueOfAttributeValues).ToList();

			return result;
		}

		private static IEnumerable<UsageData> GetConstValueOfAttributeValues(Assembly assembly)
		{
			foreach (var type in assembly.GetTypes())
			{
				var usages = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Static)
					.Where(info => info.GetCustomAttributes(typeof(AddressablesKeyAttribute), true).Length > 0)
					.Select(info => new UsageData((string)info.GetRawConstantValue(), false, type, info, info.GetCustomAttribute<AddressablesKeyAttribute>()));

				foreach (var usageData in usages)
				{
					yield return usageData;
				}
			}
		}

		internal void CalculateInputDefinitions(AddressableAssetSettings settings)
		{
			foreach (var group in settings.groups)
			{
				if (group == null)
					continue;

				if (group.HasSchema<BundledAssetGroupSchema>() && !group.Name.StartsWith("Localization", StringComparison.Ordinal)) //ignore generated and localization
				{
					var schema = group.GetSchema<BundledAssetGroupSchema>();
					var bundleInputDefinitions = new List<AssetBundleBuild>();
					var entryResult = PrepGroupBundlePacking(group, bundleInputDefinitions, schema)
						.Where(IsNotImplicitDependency); //Ignoring implicit dependencies that are added to the bundle for explicit layout.
					AssetEntries.AddRange(entryResult);
				}
			}
		}

		internal bool IsNotImplicitDependency(AddressableAssetEntry assetEntry)
		{
			const string implicitDependencyPattern = @"^Assets\/";

			return !Regex.IsMatch(assetEntry.address, implicitDependencyPattern);
		}

		internal static List<AddressableAssetEntry> PrepGroupBundlePacking(
			AddressableAssetGroup assetGroup, List<AssetBundleBuild> bundleInputDefs, BundledAssetGroupSchema schema, Func<AddressableAssetEntry, bool> entryFilter = null)
		{
			var combinedEntries = new List<AddressableAssetEntry>();
			var packingMode = schema.BundleMode;

			switch (packingMode)
			{
				case BundledAssetGroupSchema.BundlePackingMode.PackTogether:
					{
						var allEntries = new List<AddressableAssetEntry>();

						foreach (var a in assetGroup.entries)
						{
							if (entryFilter != null && !entryFilter(a))
								continue;

							a.GatherAllAssets(allEntries, true, true, false, entryFilter);
						}

						combinedEntries.AddRange(allEntries);
					}

					break;
				case BundledAssetGroupSchema.BundlePackingMode.PackSeparately:
					{
						foreach (var a in assetGroup.entries)
						{
							if (entryFilter != null && !entryFilter(a))
								continue;

							var allEntries = new List<AddressableAssetEntry>();
							a.GatherAllAssets(allEntries, true, true, false, entryFilter);
							combinedEntries.AddRange(allEntries);
						}
					}

					break;
				case BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel:
					{
						var labelTable = new Dictionary<string, List<AddressableAssetEntry>>();

						foreach (var a in assetGroup.entries)
						{
							if (entryFilter != null && !entryFilter(a))
								continue;

							var sb = new StringBuilder();
							foreach (var l in a.labels)
								sb.Append(l);
							var key = sb.ToString();
							List<AddressableAssetEntry> entries;
							if (!labelTable.TryGetValue(key, out entries))
								labelTable.Add(key, entries = new List<AddressableAssetEntry>());
							entries.Add(a);
						}

						foreach (var entryGroup in labelTable)
						{
							var allEntries = new List<AddressableAssetEntry>();

							foreach (var a in entryGroup.Value)
							{
								if (entryFilter != null && !entryFilter(a))
									continue;

								a.GatherAllAssets(allEntries, true, true, false, entryFilter);
							}

							combinedEntries.AddRange(allEntries);
						}
					}

					break;
				default:
					throw new Exception("Unknown Packing Mode");
			}

			return combinedEntries;
		}
	}

	[InitializeOnLoad]
	internal class RegisterCodeUsage
	{
		static RegisterCodeUsage()
		{
			AnalyzeSystem.RegisterNewRule<AddressablesUsageInCodeAnalyzer>();
		}
	}
}