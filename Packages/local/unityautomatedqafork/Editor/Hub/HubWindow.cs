using System;
using UnityEditor;
using System.Collections.Generic;
using Unity.AutomatedQA;
using Unity.AutomatedQA.Editor;
using Unity.RecordedTesting.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.RecordedPlayback.Editor
{
    // State serialized as EditorPrefs for persisting data between playmode/editmode
    [Serializable]
    internal partial class AQAHubState
    {
        [SerializeField] internal string currentWindowFocused = null;
    }
    internal class HubWindow : EditorWindow
    {
        internal static readonly string ALL_WINDOWS_USS_FILE_NAME = "hub-all-windows.uss";

        internal static readonly string HUB_RESOURCES_PATH = "Packages/com.unity.automated-testing/Editor/Hub/";
        internal static bool RunTutorial { get; set; }
        internal static int CurrentTutorialStep { get; set; }
        internal static HubWindow Instance { get; set; }

        private static readonly string WINDOW_FILE_NAME = "hub-window";
        private static string classBasedOnCurrentEditorColorTheme;
        private static VisualElement baseRoot;
        private static VisualElement root;
        private static VisualElement popup;
        private static VisualElement banner;
        private static HubSubWindow currentSubWindow = null;

        [SerializeField] internal AQAHubState State;

        private static class ButtonNames
        {
            internal static readonly string GoBackToHubButton = "GoBackToHubButton";
            internal static readonly string RecordAndPlaybackButton = "RecordAndPlaybackButton";
            internal static readonly string CreateAutomatedRunButton = "CreateAutomatedRunButton";
            internal static readonly string DocumentationButton = "DocumentationButton";
            internal static readonly string TutorialButton = "TutorialButton";
            internal static readonly string CloudTestingButton = "CloudTestingButton";
            internal static readonly string SettingsButton = "SettingsButton";
            internal static readonly string TestGenerationButton = "TestGenerationButton";
            internal static readonly string TestRunnerButton = "TestRunnerButton";
            internal static readonly string ToolsButton = "ToolsButton";
        }

        [MenuItem("Window/Automated QA Hub")]
        [MenuItem("Automated QA/Automated QA Hub", priority = int.MinValue)]
        internal static void ShowWindow()
        {
            Instance = GetWindow<HubWindow>(null, false);
            Instance.Show();
            Instance.Init();
        }

        private void Init()
        {
            Instance.titleContent = new GUIContent("Automated QA Hub");

            if (State == null)
            {
                State = new AQAHubState();
            }

            RenderMainView();

            if (currentSubWindow == null && EditorApplication.isPlayingOrWillChangePlaymode &&
                !string.IsNullOrEmpty(State.currentWindowFocused) && State.currentWindowFocused != GetType().ToString())
            {
                // Re-render window that was in focus before playmode change.
                SetSubWindow(State.currentWindowFocused);
            }
        }

        private void OnGUI()
        {
            if (Instance == null)
            {
                ShowWindow();
            }

            if (currentSubWindow != null)
            {
                currentSubWindow.OnGUI();
            }
        }

        private void Update()
        {
            if (currentSubWindow != null)
            {
                currentSubWindow.Update();
            }
        }

        internal static void GoBackToHub()
        {
            Instance.State.currentWindowFocused = string.Empty;
            Instance.Init();
            Instance.RenderMainView();
        }

        void RenderMainView()
        {
            classBasedOnCurrentEditorColorTheme =
                EditorGUIUtility.isProSkin ? "editor-is-dark-theme" : "editor-is-light-theme";
            rootVisualElement.Clear();

            var path = HUB_RESOURCES_PATH;

            var visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path + $"{WINDOW_FILE_NAME}.uxml");
            baseRoot = rootVisualElement;
            visualTree.CloneTree(baseRoot);

            baseRoot.styleSheets.Add(
                AssetDatabase.LoadAssetAtPath<StyleSheet>(path + $"{WINDOW_FILE_NAME}.uss"));
            baseRoot.styleSheets.Add(
                AssetDatabase.LoadAssetAtPath<StyleSheet>(path + $"{ALL_WINDOWS_USS_FILE_NAME}"));
            baseRoot.AddToClassList(classBasedOnCurrentEditorColorTheme);

            root = baseRoot.Q<ScrollView>("MainRegion");

            SetClickAction(ButtonNames.DocumentationButton,
                () => { Application.OpenURL("https://docs.unity3d.com/Packages/com.unity.automated-testing@latest"); });

            /*
            SetClickAction(ButtonNames.TutorialButton, () =>
            {
                if (RunTutorial)
                {
                    StopAndResetTutorial();
                    return;
                }
                Button tutorialButton = root.Q<Button>(ButtonNames.TutorialButton);
                tutorialButton.text = "Cancel Tutorial";
                PopMessage();
            });
            */

            SetClickAction(ButtonNames.CloudTestingButton, () => { SetSubWindow<CloudTestingSubWindow>(); });

            if (!AutomatedQASettings.EnableCloudTesting)
            {
                var button = root.Q<Button>(ButtonNames.CloudTestingButton);
                button.RemoveFromHierarchy();
            }

            SetClickAction(ButtonNames.ToolsButton, () => { SetSubWindow<ToolsSubWindow>(); });

            SetClickAction(ButtonNames.RecordAndPlaybackButton, () =>
            {
                // TODO: Take us to the R & P window with instructions on starting a new recording.
                SetSubWindow<RecordedPlaybackSubWindow>();
            });

            SetClickAction(ButtonNames.SettingsButton, () => { SetSubWindow<SettingsSubWindow>(); });

            SetClickAction(ButtonNames.TestGenerationButton, () => { SetSubWindow<CodeGenerationSubWindow>(); });
        }

        internal HubSubWindow SetSubWindow(string typeName)
        {
            Type type = Type.GetType(typeName);
            return SetSubWindow(type);
        }

        internal T SetSubWindow<T>() where T : HubSubWindow, new()
        {
            return (T) SetSubWindow(typeof(T));
        }

        internal HubSubWindow SetSubWindow(Type type)
        {
            currentSubWindow = (HubSubWindow) Activator.CreateInstance(type);
            currentSubWindow.Init();
            currentSubWindow.SetUpView(ref baseRoot);
            State.currentWindowFocused = type.ToString();
            return currentSubWindow;
        }

        private void SetClickAction(string buttonName, Action handler)
        {
            Button button = root.Q<Button>(buttonName);
            button.clickable.clicked += handler;
        }

        internal void PopMessage(string messageText = "")
        {
            popup = new VisualElement();
            popup.AddToClassList("tutorial-popup");

            VisualElement messageRegion = new VisualElement();
            messageRegion.AddToClassList("tutorial-message");

            Label message = new Label();
            message.text = string.IsNullOrEmpty(messageText) ? GetTutorialPopupMessage() : messageText;
            message.AddToClassList("tutorial-message-description");
            messageRegion.Add(message);

            VisualElement buttonGroup = new VisualElement();
            buttonGroup.AddToClassList("tutorial-message-buttons-container");

            Button cancelButton = new Button();
            cancelButton.AddToClassList("tutorial-message-button");
            cancelButton.text = "Cancel Tutorial";
            cancelButton.clickable.clicked += () => { StopAndResetTutorial(); };

            Button continueButton = new Button();
            continueButton.AddToClassList("tutorial-message-button");
            continueButton.text = "Continue";
            continueButton.clickable.clicked += () =>
            {
                if (!RunTutorial)
                    StartTutorial();
                DoTutorialStep();
                root.Remove(popup);
            };

            buttonGroup.Add(cancelButton);
            buttonGroup.Add(continueButton);
            messageRegion.Add(buttonGroup);
            popup.Add(messageRegion);
            root.Add(popup);
        }

        private string GetTutorialPopupMessage()
        {
            string message = string.Empty;
            // TODO: Add switch statement to determine text for each step of the tutorial.
            message =
                "Thank you for trying out the Unity Automated QA package. This tool will make testing your software faster, scalable, more efficient, and reliable. Anyone of any technical level can use this package to author automated UI tests. Let's get started by creating a simple recording of you doing just about anything in your application.";
            return message;
        }

        internal Button AddHubBackButton()
        {
            Button goBackToHubButton = new Button();
            goBackToHubButton.name = ButtonNames.GoBackToHubButton;
            goBackToHubButton.AddToClassList("go-back-to-hub-button");
            goBackToHubButton.clickable.clicked += () => { GoBackToHub(); };

            Label backButtonText = new Label();
            backButtonText.text = "↺  Go Back";
            backButtonText.AddToClassList("go-back-to-hub-button-text");

            goBackToHubButton.Add(backButtonText);
            return goBackToHubButton;
        }

        internal Label AddTutorialToolTip()
        {
            Label tutorialTooltip = new Label();
            tutorialTooltip.name = "TutorialInstructions";
            tutorialTooltip.AddToClassList("highlight");
            tutorialTooltip.AddToClassList("tutorial-instructions");
            tutorialTooltip.text = GetTutorialMessage();
            return tutorialTooltip;
        }

        private void StartTutorial()
        {
            RunTutorial = true;
            banner = new VisualElement();
            banner.AddToClassList("tutorial-indicator-background");
            Label tutorialModeLabel = new Label();
            tutorialModeLabel.text = "Tutorial Mode";
            tutorialModeLabel.AddToClassList("tutorial-indicator-text");
            banner.Add(tutorialModeLabel);
            root.Q<VisualElement>("BannerContainer").Add(banner);
        }

        internal void DoTutorialStep()
        {
            switch (++CurrentTutorialStep)
            {
                case 1:
                    Tutorial_Step1_SelectRecording();
                    break;
                case 2:
                    Tutorial_Step2_StartRecording();
                    break;
                default:
                    //StopAndResetTutorial();
                    break;
            }
        }

        private string GetTutorialMessage()
        {
            switch (CurrentTutorialStep + 1)
            {
                case 1:
                    return "";
                case 2:
                    return "Now select the Record button to start play mode and begin recording.";
                default:
                    return string.Empty;
            }
        }

        private void StopAndResetTutorial()
        {
            RunTutorial = false;
            CurrentTutorialStep = 0;
            root.Clear();
            RenderMainView();
        }

        internal void Tutorial_Step1_SelectRecording()
        {
            List<Button> buttons = root.Q<VisualElement>("PageButtonsContainer")
                .Query<Button>(className: "editor-window-button").ToList();
            foreach (Button button in buttons)
            {
                if (button.name != ButtonNames.RecordAndPlaybackButton)
                    button.SetEnabled(false);
            }

            Button b = root.Q<Button>(ButtonNames.RecordAndPlaybackButton);
            b.AddToClassList("highlight");
            b.Q<Label>(className: "button-description").AddToClassList("highlight");
            root.Q<Label>("TutorialInstructions").text =
                "First, select the Recorded Playback button highlighted below.";
        }

        internal void Tutorial_Step2_StartRecording()
        {
            List<Button> buttons = rootVisualElement.Query<Button>().ToList();
            foreach (Button button in buttons)
            {
                if (button.name != "RecordButton" && button.name != ButtonNames.GoBackToHubButton)
                    button.SetEnabled(false);
            }

            List<ToolbarMenu> moreOptionsDropDowns = rootVisualElement.Query<ToolbarMenu>().ToList();
            foreach (ToolbarMenu moreOptionsDropDown in moreOptionsDropDowns)
            {
                moreOptionsDropDown.SetEnabled(false);
            }

            rootVisualElement.Q<Button>("RecordButton").AddToClassList("highlight");
        }
    }
}