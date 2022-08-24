using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.EventSystems;
using Unity.AutomatedQA;
using Unity.AutomatedQA.Editor;

namespace Unity.RecordedPlayback.Editor
{
    public static class Extensions
    {
        public static void SubscribeOnItemChosen(this ListView listView, Action<object> callback)
        {
#if UNITY_2020_1_OR_NEWER
            listView.onItemsChosen += callback;
#else
            listView.onItemChosen += callback;
#endif
        }

        public static void SubscribeOnSelectionChanged(this ListView listView, Action<IEnumerable<object>> callback)
        {
#if UNITY_2020_1_OR_NEWER
            listView.onSelectionChange += callback;
#else
            listView.onSelectionChanged += callback;
#endif
        }


        public static void WrappedRebuild(this ListView listView)
        {
#if UNITY_2021_2_OR_NEWER
            listView.Rebuild();
#else
            listView.Refresh();
#endif
        }

    }

    public class CompositeRecordingsWindow : EditorWindow
    {
        private static DateTime lastRefresh = DateTime.Now;
        private string BasePath = "Packages/com.unity.automated-testing/Editor/Automators/RecordedPlayback/Windows/CompositeRecordings/";
        private string WINDOW_NAME = "CompositeRecordingsWindow";
        private string COMPOSITE_PANEL = "CompositePanel";
        private string RECORDING_PANEL = "RecordingPanel";
        private string COMPOSITE_RECORDING_BUTTON_TEXT = "CREATE COMPOSITE RECORDING";
        private string RENAME_DIALOG_TITLE = "Rename this recording file?";
        private string RENAME_DIALOG_MSG = "Are you sure you want to rename this recording file";
        private List<string> recordingPaths = new List<string>();
        private List<string> recordingsToCombine = new List<string>();
        // TODO: Add back the rename button
        // private List<string> recordingRowButtonNames = new List<string>{ "play", "edit", "find" };
        private List<string> recordingRowButtonNames = new List<string> { "play", "find" };
        private List<string> renameRecordingButtonNames = new List<string> { "save", "cancel" };

        private ListView RecordingListView;

        public enum EditorWindowState
        {
            Error = -1,
            Reset,
            NeedsSetUp,
            RecordPlayControls
        }

        private EditorWindowState state = EditorWindowState.Reset;
        private bool isPlayMode = false;
        private bool playModeStartedFromHere = false;
        private bool isRenaming = false;
        bool isCombinePanelOpen = true;
        bool renameDialogResponse = false;
        private Vector2 scrollPos = Vector2.zero;
        private Dictionary<string, string> fileRenames = new Dictionary<string, string>();

        //[MenuItem("Automated QA/Experimental/Composite Recordings...", priority = AutomatedQABuildConfig.MenuItems.CompositeRecordings)]
        public static void ShowWindow()
        {
            CompositeRecordingsWindow wnd = GetWindow<CompositeRecordingsWindow>();
            wnd.Init();
            wnd.Show();
        }
        private void Init()
        {
            titleContent = new GUIContent("Composite Recordings");
            state = EditorWindowState.Reset;
        }
        public void OnEnable()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;
            // VisualElements objects can contain other VisualElement following a tree hierarchy.
            var visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BasePath + WINDOW_NAME + ".uxml");
            visualTree.CloneTree(root);
            // A stylesheet can be added to a VisualElement and will be applied to all its children
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(BasePath + WINDOW_NAME + ".uss");
            root.styleSheets.Add(styleSheet);

            rootVisualElement.Q(COMPOSITE_PANEL).style.display = DisplayStyle.None;

            SetupControls();
            SetupViews();

            rootVisualElement.Q<VisualElement>("RecordingsList").Add(RecordingListView);
        }

        /* START Recording Management Buttons */
        private void SetupControls()
        {
            // Asset path reference in uxml
            Label assetPath = rootVisualElement.Q<Label>("AssetPath");
            assetPath.text = $"{AutomatedQASettings.RecordingFolderNameWithAssetPath}";
            assetPath.SetEnabled(false);

            // Button element references from ButtonHolder in uxml
            Button recordButton = rootVisualElement.Q<Button>("RecordButton");
            Button reportButton = rootVisualElement.Q<Button>("ReportButton");
            Button saveSegmentButton = rootVisualElement.Q<Button>("SaveButton");
            Button endButton = rootVisualElement.Q<Button>("EndButton");

            // Button element references from CompositeRecordingsPanel in uxml
            Button combineButton = rootVisualElement.Q<Button>("OpenCompositeButton");
            Button closeCombineButton = rootVisualElement.Q<Button>("CloseCompositeButton");
            Button saveCombineButton = rootVisualElement.Q<Button>("SaveCombineButton");
            Button playContinueButton = rootVisualElement.Q<Button>("PlayAndContinueButton");

            var recordingListContainer = rootVisualElement.Q("CompositeRecordingList");
            Button addButton = rootVisualElement.Q<Button>("AddCompositeButton");
            Button deleteButton = rootVisualElement.Q<Button>("DeleteCompositeButton");

            List<Button> recordingStateBtns = new List<Button>()
            {
                saveSegmentButton,
                endButton
            };

            // On window open, hide recording mode buttons
            if (EditorApplication.isPlaying && playModeStartedFromHere)
            {
                saveSegmentButton.clickable.clicked += () => { HandleSaveSegmentClick(); };
                endButton.clickable.clicked += () => { HandleEndClick(recordButton, reportButton, recordingStateBtns); };
                BulkButtonDisplayUpdate(recordingStateBtns, DisplayStyle.Flex);
                reportButton.style.display = recordButton.style.display = DisplayStyle.None;
            }
            else
            {
                recordButton.clickable.clicked += () => { HandleRecordClick(); };
                if (!ReportingManager.DoesReportExist(ReportingManager.ReportType.Html))
                {
                    reportButton.SetEnabled(false);
                }
                else
                {
                    reportButton.clickable.clicked += () => { HandleOpenHtmlReportClick(); };
                }
                BulkButtonDisplayUpdate(recordingStateBtns, DisplayStyle.None);

                closeCombineButton.clickable.clicked += () =>
                {
                    rootVisualElement.Q<IntegerField>("NumberCompositeRecordings").value = 0;
                    recordingsToCombine.Clear();
                    ClearHelpBox();
                    rootVisualElement.Q(COMPOSITE_PANEL).style.display = DisplayStyle.None;
                    rootVisualElement.Q(RECORDING_PANEL).style.display = DisplayStyle.Flex;
                    combineButton.text = COMPOSITE_RECORDING_BUTTON_TEXT;
                    combineButton.style.display = DisplayStyle.Flex;
                    isCombinePanelOpen = true;
                };

                combineButton.clickable.clicked += () => HandleCombineClickToggle();

                saveCombineButton.clickable.clicked += () => HandleSaveComposite();

                playContinueButton.clickable.clicked += () => HandlePlayContinue();

                addButton.clickable.clicked += () => HandleAddCompositeRow(recordingListContainer);

                deleteButton.clickable.clicked += () => HandleDeleteCompositeRow(recordingListContainer);
            }
        }
        private void HandleCombineClickToggle()
        {
            if (isCombinePanelOpen)
            {
                var combineButton = rootVisualElement.Q<Button>("OpenCompositeButton");
                rootVisualElement.Q(COMPOSITE_PANEL).style.display = DisplayStyle.Flex;
                rootVisualElement.Q(RECORDING_PANEL).style.display = DisplayStyle.None;
                combineButton.style.display = DisplayStyle.None;
                SetupComposite();
                rootVisualElement.Q<Button>("CloseCompositeButton").text = "BACK";
                isCombinePanelOpen = false;
            }
        }
        private void HandleRecordClick()
        {
            StartNewRecording();
        }
        private void HandleSaveSegmentClick()
        {
            RecordedPlaybackController.Instance.SaveRecordingSegment();
            SetupViews();
        }

        private void HandleEndClick(Button recordButton, Button reportButton, List<Button> recordingStateBtns)
        {
            reportButton.style.display = recordButton.style.display = DisplayStyle.Flex;
            recordButton.text = "RECORD";
            BulkButtonDisplayUpdate(recordingStateBtns, DisplayStyle.None);
            EditorApplication.ExitPlaymode();
            EditorApplication.isPlaying = false;
        }

        private void HandleOpenHtmlReportClick()
        {
            ReportingManager.OpenReportFile(ReportingManager.ReportType.Html);
        }
        /* END Recording Management Buttons */

        /* START Recording View */
        private void SetupViews()
        {
            recordingPaths = GetAllRecordingAssetPaths();

            if (RecordingListView != null)
            {
                RecordingListView.itemsSource = recordingPaths;
                RecordingListView.WrappedRebuild();
            }
            else
            {
                SetupRecordingsListView();
            }
        }



        private void SetupRecordingsListView()
        {
            // The ListView object will invoke the "makeItem" function for each items (specified as path[i])
            Func<VisualElement> makeItem = () => CreateRecordingListRow();

            // The ListView object will invoke the "bindItem" callback to associate
            // the element with the matching data item (specified as an index in the path)
            recordingPaths = GetAllRecordingAssetPaths();
            Action<VisualElement, int> bindItem = (e, i) =>
            {
                var path = recordingPaths[i];
                e.Q<Label>().text = path.Substring($"{AutomatedQASettings.RecordingFolderNameWithAssetPath}".Length + 1);
                if (!path.Contains("pending_segment_file_"))
                {
                    e.Q<Button>("PlayRecordingButton").clickable.clicked += () => HandleRecordingRowClick("play", path);
                    e.Q<Button>("FindRecordingButton").clickable.clicked += () => HandleRecordingRowClick("find", path);
                    //e.Q<Button>("EditRecordingButton").clickable.clicked += () => HandleRenameClick(path, e);
                }
            };

            // Provide the list view with an explict height for every row
            // so it can calculate how many items to actually display
            const int itemHeight = 25;

            RecordingListView = new ListView(recordingPaths, itemHeight, makeItem, bindItem);
            RecordingListView.name = "RecordingListView";
            RecordingListView.selectionType = SelectionType.Multiple;

            RecordingListView.SubscribeOnItemChosen(obj =>
            {
                var selectedFileName = obj.ToString().Replace($"{AutomatedQASettings.RecordingFolderNameWithAssetPath}/", "");
                UpdateRecordingsToCombine(selectedFileName);
                SetupViews();
            });

            RecordingListView.SubscribeOnSelectionChanged(objects =>
            {
                SetupViews();
                var prevCount = recordingsToCombine.Count;
                var newCount = 0;
                foreach (var o in objects)
                {
                    newCount++;
                }
                if (prevCount > newCount || prevCount < newCount || prevCount == newCount)
                {
                    recordingsToCombine.Clear();
                }

                foreach (var obj in objects)
                {
                    var selectedFileName = obj.ToString().Replace($"{AutomatedQASettings.RecordingFolderNameWithAssetPath}/", "");
                    UpdateRecordingsToCombine(selectedFileName);
                }
            });

            isCombinePanelOpen = true;
            RecordingListView.style.flexGrow = 1.0f;
        }
        private void UpdateRecordingsToCombine(string recordingPath)
        {
            if (!recordingsToCombine.Contains(recordingPath))
            {
                recordingsToCombine.Add(recordingPath);
            }
        }
        private VisualElement CreateRecordingListRow()
        {
            var listContainer = new VisualElement();
            listContainer.name = "RecordingRowContainer";
            listContainer.AddToClassList("recording-row-container");
            listContainer.AddToClassList("flex-end");

            var label = new Label();
            listContainer.Add(label);
            listContainer.Add(label);

            AddRecordingRowButtons(listContainer, recordingRowButtonNames);
            return listContainer;
        }
        private void AddRecordingRowButtons(VisualElement container, List<string> buttonNames)
        {
            var buttonContainer = new VisualElement();
            buttonContainer.AddToClassList("recording-row-button-container");
            foreach (var buttonName in buttonNames)
            {
                Button newButton = SetupRowButton(buttonName);
                buttonContainer.Add(newButton);
            }
            container.Add(buttonContainer);
        }
        private Button SetupRowButton(string name)
        {
            var isEdit = IsEditButton(name);
            var capitalizedName = StringExtensions.FirstCharToUpper(name);
            var toolTipText = isEdit ? "Rename" : capitalizedName;
            var rowButton = new Button();

            rowButton.name = $"{capitalizedName}RecordingButton";
            rowButton.text = isEdit ? "RENAME" : name.ToUpper();
            rowButton.tooltip = $"{toolTipText} Recording";
            rowButton.AddToClassList("recording-row-button");

            if (isEdit)
            {
                rowButton.AddToClassList("edit-button");
            }
            else if (IsFindButton(name))
            {
                rowButton.AddToClassList("find-button");
            }

            if (name != "cancel")
            {
                var buttonIcon = new Image();
                buttonIcon.AddToClassList($"{capitalizedName}Img");
                rowButton.Add(buttonIcon);
            }

            return rowButton;
        }
        private bool IsEditButton(string name) => name == "edit";
        private bool IsFindButton(string name) => name == "find";
        private void HandleRecordingRowClick(string buttonName, string recordingFilePath)
        {
            switch (buttonName)
            {
                case "play":
                    PlayRecording(recordingFilePath);
                    break;
                case "find":
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(recordingFilePath);
                    break;
                default:
                    Debug.LogWarning("Something went wrong.  Please contact us for further assistance.");
                    break;
            }
        }
        private void HandleRenameClick(string recordingFilePath, VisualElement e)
        {
            isRenaming = true;
            var filename = recordingFilePath.Substring($"{AutomatedQASettings.RecordingFolderNameWithAssetPath}".Length + 1);
            fileRenames.Add(recordingFilePath, filename);

            var renamedFile = filename;
            var rowContainer = e.Q<VisualElement>("RecordingRowContainer");
            var currentLabel = rowContainer.Q<Label>();

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Hide Read-Only Label and Show an editable TextField
                currentLabel.style.display = DisplayStyle.None;

                var textField = new TextField();
                textField.name = "RenameTextField";
                textField.value = filename;
                rowContainer.Add(textField);

                // Add edit buttons
                AddRecordingRowButtons(rowContainer, renameRecordingButtonNames);
                var saveRenameButton = rowContainer.Q<Button>("SaveRecordingButton");
                var cancelRenameButton = rowContainer.Q<Button>("CancelRecordingButton");

                // Show or Hide Row Buttons for Rename
                ToggleRenameButtons(rowContainer);

                // Add event handlers
                textField.RegisterValueChangedCallback(element =>
                {
                    renamedFile = element.newValue;
                });

                saveRenameButton.clickable.clicked += () =>
                {
                    var DIALOG_MSG = $"{RENAME_DIALOG_MSG} from {recordingFilePath} to {renamedFile}?";
                    renameDialogResponse = EditorUtility.DisplayDialog(RENAME_DIALOG_TITLE, DIALOG_MSG, "OK", "CANCEL");
                    if (renameDialogResponse)
                    {
                        currentLabel.text = Path.Combine("Assets", AutomatedQASettings.RecordingFolderName, fileRenames[recordingFilePath]);
                        currentLabel.style.display = DisplayStyle.Flex;
                        ConfirmSaveRenameFile(renamedFile, recordingFilePath);
                        rowContainer.Remove(rowContainer.Q<TextField>("RenameTextField"));

                        ToggleRenameButtons(rowContainer);
                        SetupViews();
                    }
                    else
                    {
                        CancelSaveRenameFile(recordingFilePath);
                        rowContainer.Remove(rowContainer.Q<TextField>("RenameTextField"));
                        currentLabel.style.display = DisplayStyle.Flex;

                        ToggleRenameButtons(rowContainer);
                        SetupViews();
                    }
                };

                cancelRenameButton.clickable.clicked += () =>
                {
                    CancelSaveRenameFile(recordingFilePath);
                    rowContainer.Remove(rowContainer.Q<TextField>("RenameTextField"));
                    currentLabel.style.display = DisplayStyle.Flex;

                    ToggleRenameButtons(rowContainer);
                    SetupViews();
                };
            }
        }
        private void ConfirmSaveRenameFile(string renamedFile, string recordingFilePath)
        {
            fileRenames[recordingFilePath] = renamedFile;
            var renamePath = Path.Combine("Assets", AutomatedQASettings.RecordingFolderName, fileRenames[recordingFilePath]);
            AssetDatabase.MoveAsset(recordingFilePath, renamePath);
            Debug.Log($"Renamed {recordingFilePath} to {renamePath}");
            fileRenames.Remove(recordingFilePath);
            isRenaming = false;
        }
        private void CancelSaveRenameFile(string recordingFilePath)
        {
            fileRenames.Remove(recordingFilePath);
            isRenaming = false;
        }
        private void ToggleRenameButtons(VisualElement rowContainer)
        {
            if (isRenaming)
            {
                UpdateRecordingButtonsDisplayStyle(rowContainer, DisplayStyle.None);
                UpdateRenameButtonsDisplayStyle(rowContainer, DisplayStyle.Flex);
            }
            else
            {
                UpdateRenameButtonsDisplayStyle(rowContainer, DisplayStyle.None);
                UpdateRecordingButtonsDisplayStyle(rowContainer, DisplayStyle.Flex);
            }
        }
        private void UpdateRecordingButtonsDisplayStyle(VisualElement rowContainer, DisplayStyle style)
        {
            var playButton = rowContainer.Q<Button>("PlayRecordingButton");
            var findButton = rowContainer.Q<Button>("FindRecordingButton");
            //            var renameButton = rowContainer.Q<Button>("EditRecordingButton");

            List<Button> buttons = new List<Button> { playButton, findButton };
            BulkButtonDisplayUpdate(buttons, style);
        }
        private void UpdateRenameButtonsDisplayStyle(VisualElement rowContainer, DisplayStyle style)
        {
            var saveRenameButton = rowContainer.Q<Button>("SaveRecordingButton");
            var cancelRenameButton = rowContainer.Q<Button>("CancelRecordingButton");

            List<Button> buttons = new List<Button> { cancelRenameButton, saveRenameButton };
            BulkButtonDisplayUpdate(buttons, style);
        }

        private List<string> GetTemporarySegmentFiles()
        {
            List<string> segments = new List<string>();
            string[] files = Directory.GetFiles(AutomatedQASettings.PersistentDataPath);
            int index = 0;
            foreach (string file in files)
            {
                string filename = file.Split(Path.DirectorySeparatorChar).Last();
                if (filename.StartsWith("recording_segment"))
                {
                    segments = segments.Prepend($"Assets/{ AutomatedQASettings.RecordingFolderName}/pending_segment_file_{++index}.json");
                }
            }
            return segments;
        }

        private List<string> GetAllRecordingAssetPaths()
        {
            AssetDatabase.Refresh();
            var assets = AssetDatabase.FindAssets("*", new[] { $"{AutomatedQASettings.RecordingFolderNameWithAssetPath}" }).ToList();
            var results = new List<string>();
            for (int i = 0; i < assets.Count; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assets[i]);
                if (assetPath.EndsWith(".json"))
                {
                    results.Add(assetPath);
                }
            }
            results.Sort((a, b) => Convert.ToInt32((File.GetCreationTime(b) - File.GetCreationTime(a)).TotalSeconds));
            // Add temporary segments to list as they are saved.
            if (Application.isPlaying || RecordedPlaybackPersistentData.GetRecordingMode() == RecordingMode.Record)
            {
                results = results.PrependRange(GetTemporarySegmentFiles());
            }
            return results;
        }
        /* END Recording View */

        /* START Composite View */
        private void SetupComposite()
        {
            var numberRecordingsToCombine = recordingsToCombine.Count;
            var Container = rootVisualElement.Q("CompositeRecordingList");
            Container.Clear();

            var numberCompositeRecordings = rootVisualElement.Q<IntegerField>("NumberCompositeRecordings");
            var addButton = rootVisualElement.Q<Button>("AddCompositeButton");
            var deleteButton = rootVisualElement.Q<Button>("DeleteCompositeButton");
            var compositePrePopulatedButtonList = new List<Button> { addButton, deleteButton };

            // Composite Rows
            CreatePrePopulatedCompositeRows(Container, recordingsToCombine);

            // Hide size input, Show Add/Remove Buttons
            numberCompositeRecordings.style.display = DisplayStyle.None;
            BulkButtonDisplayUpdate(compositePrePopulatedButtonList, DisplayStyle.Flex);

            if (numberRecordingsToCombine <= 1)
            {
                CreateEmptyCompositeRows(Container, 2 - numberRecordingsToCombine);
            }
        }
        private void RemoveFromRecordingsToCombine(string selectedRecording)
        {
            if (recordingsToCombine.Contains(selectedRecording))
            {
                recordingsToCombine.Remove(selectedRecording);
            }
        }
        private void CreatePrePopulatedCompositeRows(VisualElement Container, List<string> recordings)
        {
            var rowCount = Container.childCount;
            for (var i = 0; i < recordings.Count; i++)
            {
                var currRecording = recordings[i];
                var asset = AssetDatabase.LoadMainAssetAtPath($"{AutomatedQASettings.RecordingFolderNameWithAssetPath}/" + currRecording);
                ObjectField newCompositeField = new ObjectField()
                {
                    objectType = typeof(TextAsset),
                    value = asset
                };
                newCompositeField.RegisterValueChangedCallback(field =>
                {
                    var selectedElement = rootVisualElement.Q<ObjectField>(newCompositeField.name);
                    var oldValArr = RetrievePathFromElementName(newCompositeField.name);
                    var newVal = selectedElement.Q<Label>().text;
                    foreach (var name in oldValArr)
                    {
                        int index = recordingsToCombine.IndexOf(name.ToString());
                        if (index != -1)
                        {
                            recordingsToCombine[index] = newVal;
                            selectedElement.name = $"cr-{newVal}";
                        }
                    }
                });
                var id = $"cr-{currRecording}";
                newCompositeField.name = id;
                Container.Add(newCompositeField);
            }
        }

        private string[] RetrievePathFromElementName(string ElementName)
        {
            string[] defaultArr = { };
            if (!string.IsNullOrEmpty(ElementName))
            {
                string[] elementNameDelimiter = { "cr-" };
                return ElementName.Split(elementNameDelimiter, System.StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                return defaultArr;
            }
        }
        private void CreateEmptyCompositeRows(VisualElement Container, int num)
        {
            var rowCount = Container.childCount;
            for (var i = 0; i < num; i++)
            {
                ObjectField newCompositeField = new ObjectField() { objectType = typeof(TextAsset) };
                newCompositeField.RegisterValueChangedCallback(field =>
                {
                    var selectedElement = rootVisualElement.Q<ObjectField>(newCompositeField.name);
                    var selectedRecordingPath = selectedElement.Q<Label>().text;

                    int index = recordingsToCombine.IndexOf(selectedRecordingPath);
                    if (index == -1)
                    {
                        recordingsToCombine.Add(selectedRecordingPath);
                        selectedElement.name = $"cr-{selectedRecordingPath}";
                    }
                });
                var id = rowCount + 1;
                newCompositeField.name = "cr-" + id.ToString();
                Container.Add(newCompositeField);
            }
        }
        private void HandleSaveComposite()
        {
            if (recordingsToCombine.Count <= 1)
            {
                var alertContainer = rootVisualElement.Q<VisualElement>("HelpBox");
                var message = "You must add at least two recordings";
                CreateIMGUIHelpBox(alertContainer, message, MessageType.Error);
                Debug.LogWarning(message);
            }
            else
            {
                string date = DateTime.Now.ToString("yyyy-MM-dd-THH-mm-ss");
                string newFileName = $"composite-recording-{date}.json";
                var newFilePath = Path.Combine("Assets", AutomatedQASettings.RecordingFolderName, newFileName);
                CreateCompositeRecordingFile(newFilePath);
            }
        }
        private void HandleAddCompositeRow(VisualElement Container)
        {
            CreateEmptyCompositeRows(Container, 1);
        }
        private void HandleDeleteCompositeRow(VisualElement Container)
        {
            if (Container.childCount > 2)
            {
                var lastElement = Container.ElementAt(Container.childCount - 1);
                var oldVal = RetrievePathFromElementName(lastElement.name);
                foreach (var name in oldVal)
                {
                    RemoveFromRecordingsToCombine(name);
                }

                Container.Remove(lastElement);
            }
        }
        private void CreateCompositeRecordingFile(string newFilePath, bool copyToPDP = false)
        {
            RecordingInputModule.InputModuleRecordingData recordingDataInstance = new RecordingInputModule.InputModuleRecordingData();
            recordingDataInstance.recordingType = RecordingInputModule.InputModuleRecordingData.type.composite;

            foreach (var recording in recordingsToCombine)
            {
                var fileName = SanitizeRecordingName(recording);
                if (copyToPDP)
                {
                    // Copy segment recordings to persistentDataPath
                    var destPath = Path.Combine(AutomatedQASettings.RecordingFolderName, fileName);
                    var sourcePath = Path.Combine("Assets", AutomatedQASettings.RecordingFolderName, fileName);
                    File.Copy(sourcePath, destPath, true);
                }

                if (string.IsNullOrEmpty(recordingDataInstance.entryScene))
                {
                    var segmentPath = Path.Combine("Assets", AutomatedQASettings.RecordingFolderName, fileName);
                    var segment = RecordingInputModule.InputModuleRecordingData.FromFile(segmentPath);
                    recordingDataInstance.entryScene = segment.entryScene;
                }

                recordingDataInstance.AddRecording(fileName);
            }
            recordingDataInstance.AddPlaybackCompleteEvent();
            recordingDataInstance.SaveToFile(newFilePath);

            var alertContainer = rootVisualElement.Q<VisualElement>("HelpBox");
            var message = $"Composite recording successfully created at: {newFilePath}. Note: there may be a short delay (<1min) before your new file appears";
            CreateIMGUIHelpBox(alertContainer, message, MessageType.Info);
            Debug.LogWarning(message);
            AssetDatabase.Refresh();
        }
        private string SanitizeRecordingName(string recordingFilename)
        {
            // TODO: Determine where duplicate .json suffix is added
            return recordingFilename.Replace(".json", "") + ".json";
        }
        private void HandlePlayContinue()
        {
            if (recordingsToCombine.Count <= 0)
            {
                Debug.LogWarning("Missing recording from input field");
            }
            else
            {
                var newFilePath = RecordedPlaybackPersistentData.GetRecordingDataFilePath();
                CreateCompositeRecordingFile(newFilePath, true);
                ContinueCompositeRecording();
            }
        }
        /* END Composite View */

        /* START GUI & State Updates */
        private void Update()
        {
            switch (state)
            {
                case EditorWindowState.Reset:
                    UpdateStateReset();
                    break;
                case EditorWindowState.NeedsSetUp:
                    UpdateStateNeedsSetup();
                    break;
                case EditorWindowState.RecordPlayControls:
                    UpdateStateRecordPlayControls();
                    break;
                case EditorWindowState.Error:
                default:
                    UpdateStateError();
                    break;
            }
        }
        private void UpdateStateReset()
        {
            if (IsProjectSetUp())
            {
                state = EditorWindowState.RecordPlayControls;
            }
            else
            {
                state = EditorWindowState.NeedsSetUp;
            }
        }
        private void UpdateStateNeedsSetup()
        {
            // Empty
        }
        private void UpdateStateRecordPlayControls()
        {
            if (playModeStartedFromHere &&
                EditorApplication.isPlaying &&
                RecordedPlaybackController.IsPlaybackCompleted() &&
                RecordedPlaybackPersistentData.GetRecordingMode() == RecordingMode.Playback)
            {
                EditorApplication.isPlaying = false;
            }

            // poll for state change
            if (EditorApplication.isPlaying && !isPlayMode)
            {
                isPlayMode = true;
                OnEnterPlaymode();
            }
            else if (!EditorApplication.isPlaying && isPlayMode)
            {
                isPlayMode = false;
                OnExitPlaymode();
            }
        }
        private void UpdateStateError()
        {
            // Empty
        }
        private void OnGUI()
        {
            switch (state)
            {
                case EditorWindowState.NeedsSetUp:
                    GUIStateNeedsSetup();
                    break;
                case EditorWindowState.RecordPlayControls:
                    GUIStateRecordPlayControls();
                    break;
                default:
                    GUIStateError();
                    break;
            }

            if (RecordingListView != null && (DateTime.Now - lastRefresh).TotalSeconds >= 5)
            {
                var recordingAssets = GetAllRecordingAssetPaths();
                if (!recordingAssets.SequenceEqual(recordingPaths))
                {
                    SetupViews();
                }
                lastRefresh = DateTime.Now;
            }
        }
        private void GUIStateNeedsSetup()
        {
            EditorGUILayout.LabelField("Your project is not set up for Recorded Playback");
        }
        private void GUIStateError()
        {
            EditorGUILayout.LabelField("Error. See console output for details.");
        }
        private bool IsProjectSetUp()
        {
            // TODO
            return true;
        }
        private void GUIHeader(string text)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

        }
        /* END GUI & State Updates */

        /* START Recording Mode Controls */
        private void GUIStateRecordPlayControls()
        {
            // Check & update button state 
            var windowStyle = new GUIStyle(GUIStyle.none);
            windowStyle.margin = new RectOffset(5, 5, 5, 5);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, windowStyle);

            EditorGUILayout.EndScrollView();
        }
        private void PlayRecording(string recordingFilePath)
        {
            isPlayMode = false;
            playModeStartedFromHere = true;
            StartRecordedPlaybackFromEditor.StartPlayback(recordingFilePath);
        }
        void OnEnterPlaymode()
        {
            SetupControls();
        }
        void OnExitPlaymode()
        {
            Button recordButton = rootVisualElement.Q<Button>("RecordButton");
            Button reportButton = rootVisualElement.Q<Button>("ReportButton");
            Button saveSegmentButton = rootVisualElement.Q<Button>("SaveButton");
            Button endButton = rootVisualElement.Q<Button>("EndButton");
            List<Button> recordingStateBtns = new List<Button>()
            {
                saveSegmentButton,
                endButton
            };

            BulkButtonDisplayUpdate(recordingStateBtns, DisplayStyle.None);
            recordButton.style.display = DisplayStyle.Flex;
            if (ReportingManager.DoesReportExist(ReportingManager.ReportType.Html))
            {
                reportButton.style.display = DisplayStyle.Flex;
                reportButton.clickable.clicked += () => { HandleOpenHtmlReportClick(); };
                reportButton.SetEnabled(true);
            }

            if (playModeStartedFromHere)
            {
                if (RecordedPlaybackPersistentData.GetRecordingMode() == RecordingMode.Record ||
                    RecordedPlaybackPersistentData.GetRecordingMode() == RecordingMode.Extend)
                {
                    RecordedPlaybackEditorUtils.SaveCurrentRecordingDataAsProjectAsset();
                }

                RecordedPlaybackPersistentData.SetRecordingMode(RecordingMode.None);
            }

            playModeStartedFromHere = false;
            SetupViews();
        }
        private void StartNewRecording()
        {
            isPlayMode = false;
            playModeStartedFromHere = true;
            StartRecordedPlaybackFromEditor.StartRecording();
        }

        private void ContinueCompositeRecording()
        {
            isPlayMode = false;
            playModeStartedFromHere = true;
            StartRecordedPlaybackFromEditor.EnterExtendModeAndRecord();
        }
        /* END Recording Mode Controls */

        /* START UIElement Utility Methods */
        private void BulkButtonDisplayUpdate(List<Button> buttons, DisplayStyle style)
        {
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.style.display = style;
                }
            }
        }

        private void ClearHelpBox()
        {
            var HelpBox = rootVisualElement.Q<VisualElement>("HelpBox");
            HelpBox.Clear();
        }

        private void CreateIMGUIHelpBox(VisualElement container, string message, MessageType type = MessageType.Info)
        {
            // You can use the special IMGUIContainer element to embed IMGUI UI within a UIElements UI as just another element.
            // The IMGUIContainer takes a callback that serves as your OnGUI() loop, receiving all the events from outside as it normally would.
            if (container != null && !string.IsNullOrEmpty(message))
            {
                container.Clear();
                var newHelpBox = new IMGUIContainer(() => EditorGUILayout.HelpBox(message, type, true));
                newHelpBox.name = "HelpBox";
                container.Add(newHelpBox);
            }
        }
        public static class StringExtensions
        {
            public static string FirstCharToUpper(string input = "")
            {
                switch (input)
                {
                    case "":
                        throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                    default:
                        return char.ToUpper(input[0]) + input.Substring(1);
                }
            }
        }
        /* END  UIElement Utility Methods */
    }
}