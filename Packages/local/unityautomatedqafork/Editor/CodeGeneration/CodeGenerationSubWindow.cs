using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.RecordedPlayback;
using Unity.RecordedPlayback.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.RecordingInputModule;

namespace Unity.AutomatedQA.Editor
{
	/// <summary>
	/// Editor window for generating AQA tests.
	/// </summary>
	public class CodeGenerationSubWindow : HubSubWindow
	{
		private static readonly string WINDOW_FILE_NAME = "code-generation";
		private static string classBasedOnCurrentEditorColorTheme;
		private static string resourcePath = "Packages/com.unity.automated-testing/Editor/CodeGeneration/";
		private static CodeGenerationSubWindow wnd;
		private static List<string> allRecordingFiles;
		private static List<(string recording, Toggle toggle)> recordingToggles;
		private static List<(string recording, Toggle toggle)> automatedRunToggles;
		private static List<(string recording, Toggle toggle)> overrideToggles;
		private static List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)> filesWithEdits;
		private static readonly string TOGGLE_ON_CHAR = "✓";
		private static readonly string TOGGLE_OFF_CHAR = "✗";
		private static bool editedContentShouldBeFullTest = true;
		private static VisualElement baseRoot;
		private static VisualElement root;
		private static VisualElement mainButtonRow;
		private static Label successLabel;
		private static Label errorLabel;
		Toggle useSimplifiedDriverCodeToggle;
		
		public override void Init()
		{
			if (!Directory.Exists(AutomatedQASettings.RecordingDataPath))
			{
				Directory.CreateDirectory(AutomatedQASettings.RecordingDataPath);
			}
			filesWithEdits = new List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)>();
		}

		public override void SetUpView(ref VisualElement br)
		{
			SetUpView( ref br,  false, false);
		}

		public override void OnGUI()
		{
		}

		public void SetUpView(ref VisualElement br, bool isSave, bool isError)
		{
			br.Clear();
			root = new ScrollView();
			baseRoot = br;
			baseRoot.Add(root);
			root.Add(HubWindow.Instance.AddHubBackButton());
			
			overrideToggles = new List<(string recording, Toggle toggle)>();
			recordingToggles = new List<(string recording, Toggle toggle)>();
			automatedRunToggles = new List<(string recording, Toggle toggle)>();
			classBasedOnCurrentEditorColorTheme = EditorGUIUtility.isProSkin ? "editor-is-dark-theme" : "editor-is-light-theme";

			var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(resourcePath + $"{WINDOW_FILE_NAME}.uxml");
			visualTree.CloneTree(root);
			root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(resourcePath + $"{WINDOW_FILE_NAME}.uss"));

			// Add primary Generate Full Tests and Generate Simple Tests buttons.
			mainButtonRow = new VisualElement()
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					flexShrink = 0f,
				}
			};
			mainButtonRow.AddToClassList("center-align");
			Label label = new Label() { text = "Generate From Recordings:" };
			label.AddToClassList("header");
			root.Add(label);

			Button fullTestButton = new Button() { text = "Full Tests", style = { flexGrow = 1, height = 20 } };
			fullTestButton.clickable.clicked += () => { GenerateTests(false); };
			fullTestButton.tooltip = "Generates a test with step-by-step code allowing for easy test customization.";
			fullTestButton.AddToClassList(classBasedOnCurrentEditorColorTheme);
			fullTestButton.AddToClassList("button");
			mainButtonRow.Add(fullTestButton);

			// If any step files exist in the list of items to overwrite, do not show the "Simple Tests" option.
			if (!filesWithEdits.FindAll(f => f.isStepFile).Any())
			{
				Button simpleTestsButton = new Button() { text = "Simple Tests", style = { flexGrow = 1, height = 20 } };
				simpleTestsButton.clickable.clicked += () => { GenerateTests(true); };
				simpleTestsButton.AddToClassList(classBasedOnCurrentEditorColorTheme);
				simpleTestsButton.tooltip = "Generates a test that simply points to a recording file. Offers minimal opportunity for customization within test code.";
				simpleTestsButton.AddToClassList("button");
				if (!isSave && !isError)
					mainButtonRow.AddToClassList("label-space-padding");
				mainButtonRow.Add(simpleTestsButton);
			}
			root.Add(mainButtonRow);

			// Add message labels for a successful code generation, or for encountered errors.
			if (isSave)
			{
				successLabel = new Label() { text = "Success!" };
				successLabel.AddToClassList("success-label");
				root.Add(successLabel);
				RemoveMessageLabel();
			}
			else if (isError)
			{
				errorLabel = new Label() { text = "Please select test(s) first." };
				errorLabel.AddToClassList("error-label");
				root.Add(errorLabel);
				RemoveMessageLabel();
			}
			VisualElement toggleAllRow = new VisualElement()
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					flexShrink = 0f
				}
			};
			toggleAllRow.AddToClassList("toggle-row-warning");
			Label toggleAllLabel = new Label();
			toggleAllLabel.text = "Toggle All";
			toggleAllLabel.AddToClassList("recording");
			toggleAllLabel.AddToClassList("toggle-all-label");
			toggleAllRow.Add(toggleAllLabel);
			Button toggleAll = new Button();
			toggleAll.text = TOGGLE_ON_CHAR;
			toggleAll.clickable.clicked += () => {
				bool isToggleOn = false;
				if (toggleAll.text == TOGGLE_ON_CHAR)
				{
					isToggleOn = true;
					toggleAll.text = TOGGLE_OFF_CHAR;
				}
				else
				{
					toggleAll.text = TOGGLE_ON_CHAR;
				}
				foreach ((string recording, Toggle toggle) toggle in recordingToggles)
				{
					toggle.toggle.value = isToggleOn;
				}
				foreach ((string recording, Toggle toggle) toggle in automatedRunToggles)
				{
					toggle.toggle.value = isToggleOn;
				}
				foreach ((string recording, Toggle toggle) toggle in overrideToggles)
				{
					toggle.toggle.value = isToggleOn;
				}
			};
			toggleAllRow.Add(toggleAll);
			Label useSimplifiedDriverCodeLabel = new Label()
			{
				style =
				{
					marginLeft = 50,
					marginTop = 5,
					marginRight = 5,
				}
			};
			useSimplifiedDriverCodeLabel.text = "Use Simplified Driver Code";
			string simplifiedCodeToolTip = "Generate a test that simplifies code by utilizing query strings in the Driver class.";
			useSimplifiedDriverCodeLabel.tooltip = simplifiedCodeToolTip;
			toggleAllRow.Add(useSimplifiedDriverCodeLabel);
			if (useSimplifiedDriverCodeToggle == null)
			{
				useSimplifiedDriverCodeToggle = new Toggle();
				useSimplifiedDriverCodeToggle.tooltip = simplifiedCodeToolTip;
				useSimplifiedDriverCodeToggle.value = true;
			}
			toggleAllRow.Add(useSimplifiedDriverCodeToggle);
			root.Add(toggleAllRow);

			// Show only files with edits that need to be overwritten or excluded.
			if (filesWithEdits.Any())
			{
				VisualElement regionBox = new VisualElement();
				regionBox.AddToClassList("box-region");
				regionBox.AddToClassList(classBasedOnCurrentEditorColorTheme);
				Label editedWarningLabel = new Label();
				editedWarningLabel.text = "Several of these recordings have existing tests generated for them, and these existing tests have either user-edited content, or their test-type format is being changed. Are you sure you want to overwrite these changes?";
				editedWarningLabel.AddToClassList("warning");
				regionBox.Add(editedWarningLabel);

				VisualElement confirmationButtonRow = new VisualElement()
				{
					style =
				{
					flexDirection = FlexDirection.Row,
					flexShrink = 0f,
				}
				};
				Button overwriteCheckedTests = new Button();
				overwriteCheckedTests.text = "Overwrite Checked";
				overwriteCheckedTests.AddToClassList("button-confirm");
				overwriteCheckedTests.AddToClassList("button-overwrite");
				overwriteCheckedTests.clickable.clicked += () => { GenerateTests(!editedContentShouldBeFullTest); };
				confirmationButtonRow.Add(overwriteCheckedTests);
				Button clear = new Button();
				clear.text = "Clear All";
				clear.AddToClassList("button-confirm");
				clear.clickable.clicked += () => { ClearAll(); };
				confirmationButtonRow.Add(clear);
				regionBox.Add(confirmationButtonRow);
				foreach ((string recordingFileName, string cSharpScriptFileName, bool isStepFile) edited in filesWithEdits)
				{
					VisualElement recordingRowConfirm = new VisualElement()
					{
						style =
					{
						flexDirection = FlexDirection.Row,
						flexShrink = 0f,
					}
					};
					recordingRowConfirm.AddToClassList("warning-recordings");
					Toggle toggle = new Toggle();
					overrideToggles.Add((edited.recordingFileName, toggle));
					recordingRowConfirm.Add(toggle);
					Label toggleLabel = new Label();
					toggleLabel.text = $"{edited.cSharpScriptFileName}{(edited.isStepFile ? " (Step File)" : string.Empty)}";
					recordingRowConfirm.Add(toggleLabel);
					regionBox.Add(recordingRowConfirm);
					root.Add(regionBox);
				}
			}
			// List all recordings that can be transformed into compiled code tests.
			else
			{
				// Ignore any segments that are part of a composite recording (but increment child count for parent segment), then display all non-segment recording files.
				allRecordingFiles = Directory.GetFiles(AutomatedQASettings.RecordingDataPath).ToList();
				List<(string fileName, InputModuleRecordingData data)> allRecordings = new List<(string fileName, InputModuleRecordingData data)>();
				foreach (string file in Directory.GetFiles(AutomatedQASettings.RecordingDataPath))
				{
					if (new FileInfo(file).Extension != ".json")
						continue;
					var json = File.ReadAllText(Path.Combine(Application.dataPath, AutomatedQASettings.RecordingFolderName, file));
					allRecordings.Add((new FileInfo(file).Name, JsonUtility.FromJson<InputModuleRecordingData>(json)));
				}
				Dictionary<string, int> finalRecordings = new Dictionary<string, int>();
				for (int x = 0; x < allRecordings.Count; x++)
				{
					List<(string fileName, InputModuleRecordingData data)> parents = allRecordings.FindAll(a => a.data.recordings.FindAll(r => r.filename == allRecordings[x].fileName).Any());
					for (int i = 0; i < parents.Count; i++)
					{
						if (!finalRecordings.ContainsKey(parents[i].fileName))
							finalRecordings.Add(parents[i].fileName, 0);
						finalRecordings[parents[i].fileName]++;
					}
					if (!parents.Any() && !finalRecordings.ContainsKey(allRecordings[x].fileName))
					{
						finalRecordings.Add(allRecordings[x].fileName, 0);
					}
				}

				Label recordingsHeader = new Label();
				recordingsHeader.text = "Recordings";
				recordingsHeader.AddToClassList("recording-list-header");
				root.Add(recordingsHeader);

				foreach (KeyValuePair<string, int> recording in finalRecordings)
				{
					VisualElement recordingRow = new VisualElement()
					{
						style =
						{
							flexDirection = FlexDirection.Row,
							flexShrink = 0f,
						}
					};
					recordingRow.AddToClassList("toggle-row-main");
					string recordingNameWithoutFileType = recording.Key.Replace(".json", string.Empty);
					Toggle toggle = new Toggle();
					toggle.AddToClassList("recording");
					recordingToggles.Add((recordingNameWithoutFileType, toggle));
					recordingRow.Add(toggle);
					Label toggleLabel = new Label();
					toggleLabel.AddToClassList("recording");
					toggleLabel.text = recordingNameWithoutFileType;
					recordingRow.Add(toggleLabel);
					if (recording.Value > 0)
					{
						Label childCountLabel = new Label();
						childCountLabel.AddToClassList("recording-segments-found");
						childCountLabel.text = $"({recording.Value})";
						childCountLabel.tooltip = "The number of segments that make up this composite recording.";
						recordingRow.Add(childCountLabel);
					}
					root.Add(recordingRow);
				}

				Label automatorsHeader = new Label();
				automatorsHeader.text = "Automated Runs";
				automatorsHeader.AddToClassList("recording-list-header");
				root.Add(automatorsHeader);

                List<(string path, AutomatedRun automatedRun)> automatedRuns = GetAutomatedRuns();
                foreach ((string path, AutomatedRun automatedRun) ar in automatedRuns)
                {
                    VisualElement recordingRow = new VisualElement()
                    {
                        style =
                    {
                        flexDirection = FlexDirection.Row,
                        flexShrink = 0f,
                    }
                    };
                    recordingRow.AddToClassList("toggle-row-main");
                    string recordingNameWithoutFileType = ar.automatedRun.name;
                    Toggle toggle = new Toggle();
                    toggle.AddToClassList("recording");
                    automatedRunToggles.Add((ar.path, toggle));
                    recordingRow.Add(toggle);
                    Label toggleLabel = new Label();
                    toggleLabel.AddToClassList("recording");
                    toggleLabel.text = recordingNameWithoutFileType;
                    recordingRow.Add(toggleLabel);
                    List<AutomatorConfig> recordedPlaybackAutomators = ar.automatedRun.config.automators.FindAll(x => x != null && x.GetType() == typeof(RecordedPlaybackAutomator)).ToList();
                    if (recordedPlaybackAutomators.Any())
                    {
                        Label childCountLabel = new Label();
                        childCountLabel.AddToClassList("recording-segments-found");
                        childCountLabel.text = $"({recordedPlaybackAutomators.Count})";
                        childCountLabel.tooltip = "The number of recordings included in this automated run.";
                        recordingRow.Add(childCountLabel);
                    }
                    root.Add(recordingRow);
                }
            }
		}

		public void GenerateTests(bool replaceWithSimpleTests)
		{
			if (AreNoRecordingCheckboxesSelected())
			{
				SetUpView(ref baseRoot, false, true);
				return;
			}

			if (overrideToggles.Any())
			{
				// Get a list of all step files that should be overwritten. Pass it in to the GenerateTest method, and if that test file generates these step files, allow overwrite. 
				List<string> stepFilesToOverwrite = new List<string>();
				foreach ((string recordingFileName, string cSharpScriptFileName, bool isStepFile) file in filesWithEdits.FindAll(f => f.isStepFile && overrideToggles.FindAll(ot => ot.recording == f.recordingFileName && ot.toggle.value).Any()))
				{
					stepFilesToOverwrite.Add(file.recordingFileName);
					if (!replaceWithSimpleTests)
					{
						CodeGenerator.GenerateTestFromRecording(string.Empty, true, false, useSimplifiedDriverCodeToggle.value, file.recordingFileName); // Handle step files.
					}
				}

				foreach ((string recording, Toggle toggle) file in overrideToggles)
				{
					// Ignore step files in the invocation of GenerateTest. Instead, pass them in as an argument to each invocation.
					if (stepFilesToOverwrite.Contains(file.recording))
						continue;

					string originalRecordingFileName = string.Empty;
					foreach (string originalFilePath in allRecordingFiles)
					{
						string originalFile = new FileInfo(originalFilePath).Name;
						if (originalFile == file.recording)
						{
							originalRecordingFileName = originalFile;
						}
					}

					// If the associated overwrite toggle was checked, overwrite the edited file.
					if (file.toggle.value)
					{
						file.toggle.value = false;
						if (IsAutomatedRun(file.recording))
							CodeGenerator.GenerateTestFromAutomatedRun(file.recording, true, replaceWithSimpleTests, useSimplifiedDriverCodeToggle.value);
						else
							CodeGenerator.GenerateTestFromRecording(originalRecordingFileName, true, replaceWithSimpleTests, useSimplifiedDriverCodeToggle.value);
					}
				}
			}

			editedContentShouldBeFullTest = !replaceWithSimpleTests;
			filesWithEdits = new List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)>();
			overrideToggles = new List<(string recording, Toggle toggle)>();

			// Test that the old file content is not identical to the newly-generated content, which indicates that a user edited the file directly, or edited the recording.
			foreach ((string recording, Toggle toggle) file in recordingToggles)
			{
				if (!file.toggle.value)
					continue;

				// Test that the old file content is different from a freshly-generated test.
				filesWithEdits.AddRange(CodeGenerator.GenerateTestFromRecording(file.recording, false, replaceWithSimpleTests, useSimplifiedDriverCodeToggle.value));
			}
            foreach ((string recording, Toggle toggle) file in automatedRunToggles)
            {
                if (!file.toggle.value)
                    continue;

                // Test that the old file content is different from a freshly-generated test.
                filesWithEdits.AddRange(CodeGenerator.GenerateTestFromAutomatedRun(file.recording, false, replaceWithSimpleTests, useSimplifiedDriverCodeToggle.value));
            }

            bool isSuccess = false;
			// Do not recompile if we have any edited files needing confirmation. If we do, the editor window will refresh and displayed tests will be removed.
			if (!filesWithEdits.Any())
			{
				isSuccess = true;
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
			SetUpView(ref baseRoot, isSuccess, false);
		}

		public void ClearAll()
		{
			filesWithEdits = new List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)>();
			overrideToggles = new List<(string recording, Toggle toggle)>();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			SetUpView(ref baseRoot);
		}

		async static void RemoveMessageLabel()
		{
			await Task.Delay(2500);
			if (successLabel != null && root.Contains(successLabel))
				root.Remove(successLabel);
			if (errorLabel != null && root.Contains(errorLabel))
				root.Remove(errorLabel);
			mainButtonRow.AddToClassList("label-space-padding");
			errorLabel = successLabel = null;
		}

		/// <summary>
		/// Determines if any checkboxes are selected on the main or overwrite views. If none are selected, no work can be done by code generation logic.
		/// </summary>
		/// <returns></returns>
		private bool AreNoRecordingCheckboxesSelected()
		{
			return !overrideToggles.Any() && !recordingToggles.FindAll(x => x.toggle.value).Any() && !automatedRunToggles.FindAll(x => x.toggle.value).Any() ||
				!recordingToggles.Any() && !automatedRunToggles.Any() && !overrideToggles.FindAll(x => x.toggle.value).Any();
		}

		/// <summary>
		/// Find all assets and filter out Automated Runs.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static List<(string path, AutomatedRun automatedRun)> GetAutomatedRuns()
		{
			List<(string path, AutomatedRun automatedRun)> allAutomators = new List<(string path, AutomatedRun automatedRun)>();
			var guids = AssetDatabase.FindAssets($"t:{typeof(AutomatedRun)}");
			foreach (var t in guids)
			{
				var assetPath = AssetDatabase.GUIDToAssetPath(t);
				var asset = AssetDatabase.LoadAssetAtPath<AutomatedRun>(assetPath);
				if (asset != null && !asset.config.automators.FindAll(x => x == null).Any())
				{
					allAutomators.Add((assetPath, asset));
				}
			}
			return allAutomators;
		}

		public void SelectRecording(string recordingFilePath)
		{
			var recordingName = Path.GetFileNameWithoutExtension(recordingFilePath);
			foreach ((string recording, Toggle toggle) toggle in recordingToggles)
			{
				toggle.toggle.value = recordingName == toggle.recording;
			}
		}

		private static bool IsAutomatedRun(string filename)
		{
			return filename.ToLower().EndsWith(".asset");
		}
	}
}