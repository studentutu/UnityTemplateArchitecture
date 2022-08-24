using System;
using System.Collections.Generic;
using System.IO;
using Unity.AutomatedQA;
using Unity.AutomatedQA.Editor;
using Unity.AutomatedQA.Listeners;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Unity.RecordedPlayback.Editor
{
	// State serialized as EditorPrefs for persisting data between playmode/editmode
	internal partial class AQAHubState
	{
		[SerializeField] internal bool renderStopButton;
		[SerializeField] internal bool isRecording;
		[SerializeField] internal bool startCrawl;
		[SerializeField] internal float startCrawlTime = float.MaxValue;
	}
	
	[Serializable]
	internal class RecordedPlaybackSubWindow : HubSubWindow
	{
		private static readonly string WINDOW_FILE_NAME = "recording-window";
		private static string resourcePath = "Packages/com.unity.automated-testing/Editor/RecordedPlayback/Windows/RecordedPlayback/";
		private ScrollView root;
		private VisualElement baseRoot;
		private VisualElement recordingContainer;
		private static string renameFile;
		private static Label stateLabel;
		private static bool WaitForModuleReady;
		private static bool firstRecordingListedIsBrandNew;
		private static List<string> recordingPaths;
		private static string searchFilterText;

		public override void Init()
		{
			RecordedPlaybackAnalytics.SendRecordingWindowInteraction("RecordedPlaybackSubWindow", "Open");

			recordingPaths = GetAllRecordingAssetPaths();
			recordingPaths.Sort();
			EditorApplication.playModeStateChanged -= StopRecording;
			EditorApplication.playModeStateChanged += StopRecording;
		}

		public override void OnGUI()
		{
			if (!WaitForModuleReady && State.renderStopButton && !RecordingInputModule.isWorkInProgress && ReportingManager.IsReportingFinished())
			{
				StopRecording();
			}
		}

		internal override void Update()
		{
			if (Time.time > State.startCrawlTime && State.startCrawl  && Application.isPlaying && RecordedPlaybackController.Instance != null)
			{
				State.startCrawl = false;
				RecordedPlaybackController.Instance.gameObject.AddComponent<GameListenerHandler>();
				GameCrawler gc = RecordedPlaybackController.Instance.gameObject.AddComponent<GameCrawler>();
				gc.Initialize();
			}
		}

        public override void SetUpView(ref VisualElement br)
		{
			br.Clear();
			root = new ScrollView();
			baseRoot = br;
			baseRoot.Add(root);

			var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(resourcePath + $"{WINDOW_FILE_NAME}.uxml");
			visualTree.CloneTree(baseRoot);

			baseRoot.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(resourcePath + $"{WINDOW_FILE_NAME}.uss"));
			baseRoot.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(HubWindow.HUB_RESOURCES_PATH + $"{HubWindow.ALL_WINDOWS_USS_FILE_NAME}"));

			RenderElements();
			RenderRecordings();
	//		HubWindow.Instance.DoTutorialStep();
		}

		void RenderElements()
		{
			root.Add(HubWindow.Instance.AddHubBackButton());

			VisualElement buttonsRow = new VisualElement()
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					flexShrink = 0f,
					alignContent = Align.Center
				}
			};
			buttonsRow.AddToClassList("buttons-row");

			if (!State.renderStopButton && !RecordingInputModule.isWorkInProgress)
			{
				Button recordButton = new Button() { text = "Record", style = { flexGrow = 1, height = 30 } };
				recordButton.clickable.clicked += () => { StartRecording(); };
				recordButton.AddToClassList("button");
				recordButton.name = "RecordButton";
				buttonsRow.Add(recordButton);

				Button crawlButton = new Button() { text = "Crawl", style = { flexGrow = 1, height = 30 } };
				crawlButton.clickable.clicked += () => { StartCrawl(); };
				crawlButton.AddToClassList("button");
				buttonsRow.Add(crawlButton);
			}
			else
			{
				stateLabel = new Label();
				stateLabel.text = "●";
				stateLabel.AddToClassList("state-label");
				stateLabel.AddToClassList("red");
				buttonsRow.Add(stateLabel);
				Button stopButton = new Button() { text = "Stop", style = { flexGrow = 1, height = 30 } };
				stopButton.clickable.clicked += () => { StopRecording(); };
				stopButton.AddToClassList("button");
				buttonsRow.Add(stopButton);
			}

			if (ReportingManager.DoesReportExist(ReportingManager.ReportType.Html))
			{
				Button reportButton = new Button() { text = "☰ Show Report", style = { flexGrow = 1, height = 30 } };
				reportButton.clickable.clicked += () => { ShowHtmlReport(); };
				reportButton.AddToClassList("button");
				buttonsRow.Add(reportButton);
			}

			root.Add(buttonsRow);

			if (HubWindow.RunTutorial)
			{
				root.Add(HubWindow.Instance.AddTutorialToolTip());
			}

			Label label = new Label();
			label.text = "- Recording asset path -";
			label.AddToClassList("center");
			root.Add(label);

			Label val = new Label()
			{
				style =
				{
					marginBottom = 10
				}
			}; 
			val.text = AutomatedQASettings.RecordingFolderNameWithAssetPath;
			val.AddToClassList("center");
			root.Add(val);

			VisualElement refreshRow = new VisualElement()
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					flexShrink = 0f,
					alignContent = Align.Center
				}
			};

			Label filterLabel = new Label();
			filterLabel.text = "Filter: ";
			filterLabel.AddToClassList("filter-label");
			refreshRow.Add(filterLabel);

			searchFilterText = string.Empty;
			TextField newName = new TextField();
			newName.AddToClassList("filter-field");
			newName.RegisterValueChangedCallback(x =>
			{
				searchFilterText = x.newValue;
				root.Remove(recordingContainer);
				RenderRecordings();
			});
			refreshRow.Add(newName);

			Button refreshListButton = new Button() { text = "↻", tooltip = "Refresh recordings list" };
			refreshListButton.clickable.clicked += () => 
			{
				newName.value = string.Empty;
				recordingPaths = GetAllRecordingAssetPaths();
				recordingPaths.Sort();
				SetUpView(ref baseRoot);
			};
			refreshListButton.AddToClassList("refresh-button");
			refreshRow.Add(refreshListButton);
			root.Add(refreshRow);
		}

		private void RenderRecordings()
		{
			if(!recordingPaths.Any())
				recordingPaths = GetAllRecordingAssetPaths();
			recordingPaths.RemoveAll(x => !x.EndsWith(".json"));
			recordingContainer = new VisualElement();
			for (int i = 0; i < recordingPaths.Count; i++)
			{
				string recordingFilePath = recordingPaths[i];
				string filename = recordingFilePath.Substring($"{AutomatedQASettings.RecordingFolderNameWithAssetPath}".Length + 1);

				if (!string.IsNullOrEmpty(searchFilterText))
				{
					if (!filename.Contains(searchFilterText))
						continue;
				}

				VisualElement row = new VisualElement();
				if (renameFile == recordingFilePath)
				{
					RecordingRenameView(recordingFilePath);
				}
				else
				{
					VisualElement recordingRow = new VisualElement();
					recordingRow.AddToClassList("item-row");
					if (i % 2 == 0)
					{
						recordingRow.AddToClassList("even");
					}
					if (firstRecordingListedIsBrandNew)
                    {
						firstRecordingListedIsBrandNew = false;
						recordingRow.AddToClassList("item-row-new");
					}

					Button playButton = new Button() { text = "▸" };
					playButton.clickable.clicked += () => {
						if (!RecordingInputModule.isWorkInProgress)
						{
							State.renderStopButton = true;
							PlayRecording(recordingFilePath);
							SetUpView(ref baseRoot);
							WaitForModuleToBeReady();
						}
					};
					playButton.tooltip = "Play recording";
					playButton.AddToClassList("small-button");
					playButton.AddToClassList("play-button");
					recordingRow.Add(playButton);

					var toolbar = new CustomToolbarMenu { text = "..." };
					toolbar.name = "MoreOptionsDropDown";
					toolbar.menu.AppendAction("Generate Test", a => {
						Debug.Log("Generate Test for " + recordingFilePath);
						HubWindow hubWindow = (HubWindow) EditorWindow.GetWindow(typeof(HubWindow));
						hubWindow.ShowPopup();
						CodeGenerationSubWindow codeGenSubWindow = hubWindow.SetSubWindow<CodeGenerationSubWindow>();
						codeGenSubWindow.SelectRecording(recordingFilePath);
					});
					toolbar.menu.AppendAction("Rename", a => {
						renameFile = recordingFilePath;
						SetUpView(ref baseRoot);
					});
					toolbar.menu.AppendAction("Find", a => {
						Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(recordingFilePath);
					});
					recordingRow.Add(toolbar);

					Label label = new Label();
					label.AddToClassList("recording-label");
					label.text = filename;
					recordingRow.Add(label);

					recordingContainer.Add(recordingRow);
				}
			}
			root.Add(recordingContainer);
		}

		private void RecordingRenameView(string recordingFilePath)
		{
			string fileName = recordingFilePath.Substring($"{AutomatedQASettings.RecordingFolderNameWithAssetPath}".Length + 1);

			VisualElement renameContainer = new VisualElement();
			renameContainer.AddToClassList("item-block");

			TextField newName = new TextField();
			newName.value = fileName.Replace(".json", string.Empty);
			newName.AddToClassList("edit-field");

			// Schedule focus event to highlight input's text (Required for successful focus).
			baseRoot.schedule.Execute(() => {
				newName.ElementAt(0).Focus();
			});

			Button saveButton = new Button() { text = "✓" };
			saveButton.clickable.clicked += () => {
				if (newName.value.Contains("_") && newName.value.Contains("-"))
				{
					throw new Exception("Recording file names cannot mix both underscores \"_\" and dashes \"-\".");
				} 
				var renamePath = Path.Combine("Assets", AutomatedQASettings.RecordingFolderName, $"{newName.value}.json");
				AssetDatabase.MoveAsset(recordingFilePath, renamePath);
				recordingPaths = GetAllRecordingAssetPaths();
				recordingPaths.Sort();
				SetUpView(ref baseRoot);
			};
			saveButton.AddToClassList("small-button");
			renameContainer.Add(saveButton);

			Button cancelButton = new Button() { text = "X" };
			cancelButton.clickable.clicked += () => {
				renameFile = string.Empty;
				SetUpView(ref baseRoot);
			};
			cancelButton.AddToClassList("small-button");
			renameContainer.Add(cancelButton);

			renameContainer.Add(newName);
			root.Add(renameContainer);
		}

		void StartRecording()
		{
			WaitForModuleToBeReady();
			State.renderStopButton = State.isRecording = true;
			StartRecordedPlaybackFromEditor.StartRecording();
			SetUpView(ref baseRoot);
		}

		void StopRecording(PlayModeStateChange state = PlayModeStateChange.ExitingPlayMode)
		{
			if (state == PlayModeStateChange.ExitingPlayMode)
			{
				if(RecordingInputModule.Instance != null)
					RecordingInputModule.Instance.EndRecording();
				if (State.isRecording || ReportingManager.IsCrawler)
				{
					string assetName = RecordedPlaybackEditorUtils.SaveCurrentRecordingDataAsProjectAsset();
					if (!string.IsNullOrEmpty(assetName))
					{
						recordingPaths = recordingPaths.AddAtAndReturnNewList(0, $"Assets/Recordings/{assetName}");
						firstRecordingListedIsBrandNew = true;
					}
					ReportingManager.FinalizeReport();
				}
				GameCrawler.Stop = true;
				ReportingManager.IsCrawler = State.renderStopButton = State.isRecording = false;
				SetUpView(ref baseRoot);
			}
		}

		private void PlayRecording(string recordingFilePath)
		{
			RecordedPlaybackAnalytics.SendRecordingWindowInteraction("RecordedPlaybackSubWindow", "PlayRecording");
			StartRecordedPlaybackFromEditor.StartPlayback(recordingFilePath);
		}

		private List<string> GetAllRecordingAssetPaths()
		{
			if (!Directory.Exists($"{AutomatedQASettings.RecordingFolderNameWithAssetPath}"))
			{
				return new List<string>();
			}

			var results = new List<string>(AssetDatabase.FindAssets("*", new[] { $"{AutomatedQASettings.RecordingFolderNameWithAssetPath}" }));
			for (int i = 0; i < results.Count; i++)
			{
				results[i] = AssetDatabase.GUIDToAssetPath(results[i]);
			}
			results.Sort((a, b) => Convert.ToInt32((File.GetCreationTime(b) - File.GetCreationTime(a)).TotalSeconds));

			return results;
		}

		void StartCrawl()
		{
			WaitForModuleToBeReady();
			State.renderStopButton = true;
			ReportingManager.IsCrawler = true;
			StartRecordedPlaybackFromEditor.StartRecording();
			ReportingManager.InitializeReport();
			State.startCrawlTime = Time.time + 2;
			State.startCrawl = true;
			SetUpView(ref baseRoot);
		}

		void WaitForModuleToBeReady()
		{
			WaitForModuleReady = true;
			baseRoot.schedule.Execute(() => {
				WaitForModuleReady = false;
			}).ExecuteLater(500);
		}

		private void ShowHtmlReport()
		{
			ReportingManager.OpenReportFile(ReportingManager.ReportType.Html);
		}

        internal class CustomToolbarMenu : ToolbarMenu
        {
            internal CustomToolbarMenu()
            {
                variant = Variant.Popup;
                RemoveFromClassList(ussClassName);
                AddToClassList("unity-button");
                AddToClassList("small-button");
                style.paddingLeft = 4;
                style.paddingRight = 4;
                VisualElement arrow = null;
                foreach (var element in Children())
                {
                    if (element.ClassListContains(ussClassName + "__arrow"))
                        arrow = element;
                }

                if (arrow != null)
                {
                    Remove(arrow);
                }
            }
        }
    }
}