using System.IO;
using UnityEditor;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.AutomatedQA;
using Toggle = UnityEngine.UIElements.Toggle;
using Unity.AutomatedQA.Editor;

namespace Unity.RecordedPlayback.Editor
{
	public class SettingsSubWindow : HubSubWindow
	{
		private static readonly string WINDOW_FILE_NAME = "settings-window";
		private static string classBasedOnCurrentEditorColorTheme;
		private static string resourcePath = "Packages/com.unity.automated-testing/Editor/Settings/";

		// On window load, take two copies of the settings. One will update with user input. The other will be compared to the current set to determine if changes were made.
		private static List<(string Key, string Value)> settings = new List<(string Key, string Value)>();
		private static List<(string Key, string Value)> oldSettings = new List<(string Key, string Value)>();

		private static Label notification;
		private static VisualElement settingsContainer;
		private VisualElement baseRoot;
		private static ScrollView root;
		private static VisualElement newConfigRow;
		private static List<VisualElement> configRows;
		private static List<Toggle> toggles;
		private static List<TextField> textFields;
		private static TextField newConfigKey;
		private static TextField newConfigValue;
		private static HubWindow wnd;
		public  override void Init()
		{
		}

		public override void OnGUI()
		{
		}

		public override void SetUpView(ref VisualElement br)
		{
			br.Clear();
			root = new ScrollView();
			baseRoot = br;
			baseRoot.Add(root);
			root.Add(HubWindow.Instance.AddHubBackButton());
			
			toggles = new List<Toggle>();
			textFields = new List<TextField>();
			configRows = new List<VisualElement>();
			settings = new List<(string Key, string Value)>();
			oldSettings = new List<(string Key, string Value)>();
			newConfigRow = new VisualElement() { style = { marginTop = 15 } };
			newConfigRow.AddToClassList("settings-row");
			Label keyLabel = new Label();
			keyLabel.text = "Key";
			newConfigRow.Add(keyLabel);
			newConfigKey = new TextField();
			newConfigRow.Add(newConfigKey);
			Label valueLabel = new Label() { style = { marginTop = 5 } };
			valueLabel.text = "Value";
			newConfigRow.Add(valueLabel);
			newConfigValue = new TextField();
			newConfigRow.Add(newConfigValue);
			VisualElement buttonRow = new VisualElement()
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					flexShrink = 0f,
				}
			};

			Button saveButton = new Button() { text = "Add", style = { flexGrow = 1, height = 20 } };
			saveButton.clickable.clicked += () => { SaveNewConfig(); };
			buttonRow.Add(saveButton);
			Button removeButton = new Button() { text = "Delete", style = { flexGrow = 1, height = 20 } };
			removeButton.clickable.clicked += () => { HideNewConfig(); };
			buttonRow.Add(removeButton);
			newConfigRow.Add(buttonRow);

			wnd = EditorWindow.GetWindow<HubWindow>(); //Use to determine window size before ShowWindow is invoked.
			classBasedOnCurrentEditorColorTheme = EditorGUIUtility.isProSkin ? "editor-is-dark-theme" : "editor-is-light-theme";
			
			var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(resourcePath + $"{WINDOW_FILE_NAME}.uxml");
			visualTree.CloneTree(baseRoot);

			baseRoot.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(resourcePath + $"{WINDOW_FILE_NAME}.uss"));
			LoadConfigData();
			


			root = new ScrollView();
			baseRoot.Add(root);

			Label requiredHeader = new Label();
			requiredHeader.AddToClassList("section-header");
			requiredHeader.AddToClassList(classBasedOnCurrentEditorColorTheme);

			settingsContainer = new VisualElement();
			Label customHeader = new Label();
			customHeader.AddToClassList("section-header");
			customHeader.AddToClassList(classBasedOnCurrentEditorColorTheme);
			customHeader.text = "Automated QA Settings";
			root.Add(customHeader);
			notification = new Label();
			notification.visible = false;
			root.Add(notification);
			settingsContainer.name = "custom-settings-container";
			root.Add(settingsContainer);
			RenderSettings();

			RenderAddNewCustomSettings();
			oldSettings = new List<(string Key, string Value)>();
			oldSettings.AddRange(settings);
		}

		void RenderSettings()
		{
			foreach ((string Key, string Value) keyVal in settings)
			{
				AddConfigSet(keyVal.Key, keyVal.Value);
			}
		}

		void AddConfigSet(string keyText, string valueText)
		{
			VisualElement row = new VisualElement();
			Button deleteCustomConfigButton = new Button();
			deleteCustomConfigButton.text = "X";
			deleteCustomConfigButton.AddToClassList("delete-custom-settings-button");
			deleteCustomConfigButton.clickable.clicked += () => { DeleteConfigKey(keyText); };
			row.Add(deleteCustomConfigButton);
			row.name = keyText;

			Label key = new Label();
			key.text = AddSpacesBeforeUppercaseLettersAndInPlaceOfUnderscores(keyText);
			key.tooltip = GetTooltip(keyText);
			key.AddToClassList("settings-field-label");
			key.AddToClassList("delete-button-offset");
			row.Add(key);
			if (bool.TryParse(valueText, out bool val))
			{
				Toggle value = new Toggle();
				value.AddToClassList("settings-field-text-toggle");
				value.name = keyText;
				value.value = val;
				value.AddToClassList("delete-button-offset");
				value.RegisterValueChangedCallback(element =>
				{
					(string Key, string Value) set = settings.Find(s => s.Key == value.name);
					set.Value = value.value.ToString();
					int index = settings.FindIndex(s => s.Key == value.name);
					settings.RemoveAt(index);
					settings.Add(set);
					if (set.Value != oldSettings.Find(s => s.Key == value.name).Value)
					{
						key.AddToClassList("setting-dirty");
					}
					else
					{
						key.RemoveFromClassList("setting-dirty");
					}
				});
				value.AddToClassList(classBasedOnCurrentEditorColorTheme);
				toggles.Add(value);
				row.Add(value);
			}
			else
			{
				TextField value = new TextField();
				value.AddToClassList("settings-field-text-input");
				value.value = valueText;
				value.name = keyText;
				value.AddToClassList("delete-button-offset");
				value.RegisterValueChangedCallback(element =>
				{
					(string Key, string Value) set = settings.Find(s => s.Key == value.name);
					set.Value = value.text;
					int index = settings.FindIndex(s => s.Key == value.name);
					settings.RemoveAt(index);
					settings.Add(set);
					if (set.Value != oldSettings.Find(s => s.Key == value.name).Value)
					{
						key.AddToClassList("setting-dirty");
					}
					else
					{
						key.RemoveFromClassList("setting-dirty");
					}
				});
				value.AddToClassList(classBasedOnCurrentEditorColorTheme);
				textFields.Add(value);
				row.Add(value);
			}
			row.AddToClassList("settings-row");
			settingsContainer.Add(row);
			configRows.Add(row);
		}

		void RenderAddNewCustomSettings()
		{
			VisualElement buttonContainerInner = new VisualElement();
			buttonContainerInner.AddToClassList("button-container-inner");
			VisualElement buttonRow = new VisualElement()
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					flexShrink = 0f,
				}
			};

			Button addNewConfigButton = new Button() { text = "Add New", style = { flexGrow = 1, height = 40 } };
			if (wnd.position.width > 375)
			{
				addNewConfigButton.AddToClassList("button");
				addNewConfigButton.AddToClassList("button-left");
			}
			addNewConfigButton.clickable.clicked += () => { AddNewCustomConfig(); };
			buttonRow.Add(addNewConfigButton);
			Button saveButton = new Button() { text = "Save", style = { flexGrow = 1, height = 40 } };
			if (wnd.position.width > 375)
			{
				saveButton.AddToClassList("button");
			}
			saveButton.clickable.clicked += () => { SaveSettings(); };
			buttonRow.Add(saveButton);
			buttonContainerInner.Add(buttonRow);
			root.Add(buttonContainerInner);
		}

		void AddNewCustomConfig() {
			ClearErrors();
			settingsContainer.Add(newConfigRow);
		}

		void HideNewConfig() {
			ClearErrors();
			settingsContainer.Remove(newConfigRow);
		}

		void DeleteConfigKey(string key) {
			ClearErrors();
			AutomatedQASettings.AutomatedQASettingsData newConfig = new AutomatedQASettings.AutomatedQASettingsData();
			for (int i = 0; i < oldSettings.Count; i++)
			{
				string currentKey = settings[i].Key;
				string currentValue = settings[i].Value;
				if (currentKey == key)
					continue;
				newConfig.Configs.Add(new AutomatedQASettings.AutomationSet(currentKey, currentValue));
			}

			VisualElement row = configRows.Find(cr => cr.name == key);
			settingsContainer.Remove(row);
			configRows.Remove(row);

			settings.RemoveAll(s => s.Key == key);
			oldSettings.RemoveAll(os => os.Key == key);
			toggles.RemoveAll(to => to.name == key);
			textFields.RemoveAll(tf => tf.name == key);
			SortAlphabetically(newConfig.Configs);

			//Overwrite and save settings to file.
			File.WriteAllText(Path.Combine(Application.dataPath, AutomatedQASettings.AutomatedQASettingsResourcesPath, AutomatedQASettings.AutomatedQaSettingsFileName),
				JsonUtility.ToJson(newConfig));
			AutomatedQASettings.RefreshConfig();
		}

		void SaveNewConfig()
		{
			string newKey = newConfigKey.text.Trim();
			string newValue = newConfigValue.text.Trim();
			ClearErrors();
			(string Key, string Value) existingMatch =
				oldSettings.Find(x => x.Key.ToLowerInvariant().Replace("_", string.Empty) == newKey.ToLowerInvariant().Replace("_", string.Empty));
			bool errorDetected = existingMatch != default((string Key, string Value));
			string errorMessage = "A key already exists with an identical name (or with the only difference being casing or underscores). Please choose a different key.";
			if (!KeyContainsValidCharacters(newKey))
			{
				errorDetected = true;
				errorMessage = "Invalid characters in key. Key should be alpha-numeric, with underscores or camel casing used for multi-word keys.";
			}

			if (errorDetected)
			{
				notification.visible = true;
				notification.text = errorMessage;
				notification.AddToClassList("error-label");
				Toggle toggle = toggles.Find(to => to.name == existingMatch.Key);
				if (toggle != null)
					toggle.AddToClassList(".setting-error");
				TextField textField = textFields.Find(tf => tf.name == existingMatch.Key);
				if (textField != null)
					textField.AddToClassList(".setting-error");
				root.ScrollTo(notification);
				return;
			}
			AutomatedQASettings.AutomatedQASettingsData newConfig = new AutomatedQASettings.AutomatedQASettingsData();
			for (int i = 0; i < oldSettings.Count; i++)
			{
				string currentKey = settings[i].Key;
				string currentValue = settings[i].Value;
				newConfig.Configs.Add(new AutomatedQASettings.AutomationSet(currentKey, currentValue));
			}
			newConfig.Configs.Add(new AutomatedQASettings.AutomationSet(newKey, newValue));
			SortAlphabetically(newConfig.Configs);

			// Save old settings plus newly added setting to file.
			File.WriteAllText(Path.Combine(Application.dataPath, AutomatedQASettings.AutomatedQASettingsResourcesPath, AutomatedQASettings.AutomatedQaSettingsFileName),
				JsonUtility.ToJson(newConfig));
			AutomatedQASettings.RefreshConfig();

			settings.Add((newKey, newConfigValue.text));
			oldSettings.Add((newKey, newConfigValue.text));
			AddConfigSet(newKey, newConfigValue.text);
			settingsContainer.Remove(newConfigRow);
		}

		void LoadConfigData()
		{
			AutomatedQASettings.AutomatedQASettingsData newConfig = AutomatedQASettings.GetCustomSettingsData();

			for (int i = 0; i < newConfig.Configs.Count; i++)
			{
				AutomatedQASettings.AutomationSet set = newConfig.Configs[i];
				if (string.IsNullOrEmpty(set.Key))
				{
					continue;
				}

				// Order alphabetically.
				int newIndex = 0;
				for (int x = 0; x < settings.Count; x++) {
					int order = string.Compare(set.Key, settings[x].Key);
					if (order <= 0)
					{
						newIndex = x;
						break;
					}
					newIndex = x + 1;
				}
				if (newIndex == settings.Count)
					settings.Add((set.Key, set.Value));
				else
					settings = settings.AddAtAndReturnNewList(newIndex, (set.Key, set.Value));
			}

		}

		void SaveSettings()
		{
			AutomatedQASettings.AutomatedQASettingsData newConfig = new AutomatedQASettings.AutomatedQASettingsData();
			for (int i = 0; i < settings.Count; i++)
			{
				newConfig.Configs.Add(new AutomatedQASettings.AutomationSet(settings[i].Key, settings[i].Value));
			}
			SortAlphabetically(newConfig.Configs);

			//Overwrite and save settings to file.
			File.WriteAllText(Path.Combine(Application.dataPath, AutomatedQASettings.AutomatedQASettingsResourcesPath, AutomatedQASettings.AutomatedQaSettingsFileName),
				JsonUtility.ToJson(newConfig));
			AutomatedQASettings.RefreshConfig();
			SetUpView(ref baseRoot);
		}

		void ClearErrors()
		{
			notification.text = "";
			notification.RemoveFromClassList("error-label");
			notification.visible = false;
			foreach (Toggle to in toggles)
				to.RemoveFromClassList(".setting-error");
			foreach (TextField tf in textFields)
				tf.RemoveFromClassList(".setting-error");
		}

		List<AutomatedQASettings.AutomationSet> SortAlphabetically(List<AutomatedQASettings.AutomationSet> list)
		{
			List<string> comparable = new List<string>();
			foreach (AutomatedQASettings.AutomationSet item in list)
			{
				comparable.Add(item.Key);
			}
			comparable.Sort();

			List<AutomatedQASettings.AutomationSet> sortedList = new List<AutomatedQASettings.AutomationSet>();
			foreach (string item in comparable)
			{
				sortedList.Add(list.Find(x => x.Key == item));
			}
			return sortedList;
		}

		/// <summary>
		/// We don't want characters that will make our json config not well-formed. 
		/// Limit keys to alphanumerics, spaces, and underscores. 
		/// </summary>
		/// <param name="key">String key name.</param>
		bool KeyContainsValidCharacters(string key) 
		{
			bool invalidCharDetected = false;
			char[] charArray = key.ToCharArray();
			foreach (char c in charArray)
			{
				if (!char.IsLetterOrDigit(c) &&
					c != '_')
					invalidCharDetected = true;
			}
			return !invalidCharDetected;
		}

		private static string GetTooltip(string key)
		{
			if (AutomatedQASettings.Tooltips.TryGetValue(key, out string tooltip))
			{
				return tooltip;
			}

			return key;
		}
		
		private static string AddSpacesBeforeUppercaseLettersAndInPlaceOfUnderscores(string val)
		{
			StringBuilder returnVal = new StringBuilder();
			char[] charArray = val.ToCharArray();
			foreach (char c in charArray)
			{
				if (char.IsUpper(c))
					returnVal.Append(" ");
				returnVal.Append(c);
			}
			return returnVal.ToString().Replace("_", " ");
		}

		public static Texture2D MakeTextureFromColor(Color color)
		{
			Texture2D result = new Texture2D(1, 1);
			result.SetPixels(new Color[] { color });
			result.Apply();
			return result;
		}
	}
}