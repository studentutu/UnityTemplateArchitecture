#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AutomatedQA;
using Unity.RecordedPlayback;
#if UNITY_EDITOR
using UnityEditor;
#else
using System.IO;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GeneratedAutomationTests
{
    public abstract class AutomatedQATestsBase : AutomatedTestSuiteBase
    {
        protected RunContext Test
        {
            get
            {
                if (_test == null)
                    _test = new RunContext();
                return _test;
            }
            set => _test = value;
        }
        private RunContext _test;
        protected AutomatedRun automatedRun;
        protected RecordingInputModule.InputModuleRecordingData recordingData { get; set; }

        protected virtual void SetUpTestClass() { }
        protected virtual void SetUpTestRun() { }

        [UnitySetUp]
        public IEnumerator SetUpSingleTest()
        {
#if UNITY_EDITOR
            if (!playModeStateChangeListenerSet)
            {
                playModeStateChangeListenerSet = true;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            }
#endif
            ReportingManager.IsPlaybackStartedFromEditorWindow = false;
            ReportingManager.IsTestWithoutRecordingFile = ReportingManager.IsUTRTest = true;
            
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            SetUpTestRun();
            recordingData = new RecordingInputModule.InputModuleRecordingData();
            recordingData.entryScene = Test.entryScene;
            recordingData.recordedAspectRatio = Test.recordedAspectRatio;
            recordingData.recordedResolution = Test.recordedResolution;
            recordingData.recordingType = RecordingInputModule.InputModuleRecordingData.type.single;
            ReportingManager.CurrentTestName = $"{TestContext.CurrentContext.Test.ClassName}.{TestContext.CurrentContext.Test.Name}";
            ReportingManager.InitializeReport();
            ReportingManager.InitializeDataForNewTest();

            // Set up recording data
            RecordedPlaybackPersistentData.SetRecordingData(recordingData);

            // Start playback
            if (CentralAutomationController.Exists())
            {
                CentralAutomationController.Instance.Reset();
            }
            if (RecordedPlaybackController.Exists())
            {
                RecordedPlaybackController.Instance.Reset();
            }
            CentralAutomationController.Instance.AddAutomator<RecordedPlaybackAutomator>(new RecordedPlaybackAutomatorConfig
            {
                loadEntryScene = true,
            });
            CentralAutomationController.Instance.Run();

            if (automatedRun == null)
            {
  //              RecordedPlaybackController.Instance.Begin();
            }

            // Wait for RecordingInputModule to initialize
            var startTime = DateTime.UtcNow;
            int timeoutSecs = 60;
            while (RecordingInputModule.Instance == null)
            {
                if (DateTime.UtcNow.Subtract(startTime).TotalSeconds >= timeoutSecs)
                {
                    Debug.LogError($"Timeout wile waiting for RecordingInputModule to initialize");
                    break;
                }
                yield return null;
            }
            RecordingInputModule.Instance.ClearTouchData();
            SetUpTestClass();

            if (!ReportingManager.IsAutomatorTest && string.IsNullOrEmpty(Test.entryScene))
                throw new UnityException("Current test's scene is not declared in the SetUpTestRun method. Make sure the scene is correct in the associated PageObject if using simplified test logic.");
            yield return null;
        }

        protected class RunContext
        {
            public string entryScene;
            public Vector2 recordedAspectRatio;
            public Vector2 recordedResolution;
        }

        [UnityTearDown]
        protected virtual IEnumerator UnityTearDown()
        {
            if (Driver.Steps.Any())
                yield return Driver.Perform.EmitTestComplete(); // Test complete.

            Driver.Reset();
            ReportingManager.IsTestWithoutRecordingFile = false;
            if (CentralAutomationController.Exists())
            {
                CentralAutomationController.Instance.Reset();
            }
            if (RecordedPlaybackController.Exists())
            {
                RecordedPlaybackController.Instance.Reset();
            }

            ReportingManager.CreateMonitoringService();

            int sceneCount = 0;
            string sceneName = string.Empty;
            while (true)
            {
                bool sceneExists = false;
                sceneName = "emptyscene" + sceneCount++;
                for (int x = 0; x < SceneManager.sceneCount; x++)
                {
                    if (SceneManager.GetSceneAt(x).name == sceneName)
                    {
                        sceneExists = true;
                    }
                }
                if (!sceneExists)
                    break;
            }

            var emptyScene = SceneManager.CreateScene(sceneName);
            SceneManager.SetActiveScene(emptyScene);
            yield return UnloadScenesExcept(emptyScene.name);
        }

        protected IEnumerator WaitFor(Func<bool> condition, float timeout = 30f)
        {
            float timer = 0;
            while (!condition.Invoke() && timer <= timeout)
            {

                float start = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup < start + 1f)
                {

                    yield return null;

                }
                timer++;

            }
            yield return null;
        }

        private IEnumerable UnloadScenesExcept(string sceneName)
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name != sceneName)
                {
                    var unloadSceneAsync = SceneManager.UnloadSceneAsync(scene);
                    while (!unloadSceneAsync.isDone)
                    {
                        yield return null;
                    }
                }
            }
        }

        protected IEnumerator RunAutomator(Type type, int expectedIndex = -1)
        {
            AutomatedRun modifiedAutomatedRun = ScriptableObject.CreateInstance<AutomatedRun>();
            modifiedAutomatedRun.config.automators = new List<AutomatorConfig>();
            CentralAutomationController.Instance.ResetAutomators();

            List<AutomatorConfig> automators = automatedRun.config.automators.FindAll(x => x.AutomatorType != typeof(RecordedPlaybackAutomator));
            if (automators.Count <= expectedIndex)
            {
                Debug.LogError($"The test \"{ReportingManager.CurrentTestName}\" references an automator of type \"{type}\" at index {expectedIndex}. There isn't that many automators associated with this Automated Run (RecordedPlaybackAutomator are ignored in this count, as they are split into individual steps during code generation). Check that the Automated Run was not updated, and update the current test to match.");
            }
            AutomatorConfig targetConfig = automatedRun.config.automators.FindAll(x => x.AutomatorType != typeof(RecordedPlaybackAutomator))[expectedIndex];
            if (targetConfig.AutomatorType != type)
            {
                Debug.LogError($"The test \"{ReportingManager.CurrentTestName}\" references an automator of type \"{type}\" at index {expectedIndex}. The automator at that index ({targetConfig.AutomatorType}) is not the expected type. Check that the Automated Run was not updated, and update the current test to match.");
            }

            modifiedAutomatedRun.config.automators.Add(targetConfig);
            CentralAutomationController.Instance.Run(modifiedAutomatedRun.config);
            List<Automator> test = CentralAutomationController.Instance.GetAllAutomators();
            while (CentralAutomationController.Instance.GetAllAutomators().First().state == Automator.State.IN_PROGRESS)
            {
                yield return null;
            }
        }

        protected AutomatedRun GetAutomatedRun(string assetPath, string resourceName)
        {
#if UNITY_EDITOR
                return AssetDatabase.LoadAssetAtPath<AutomatedRun>(assetPath);
#else
                return Resources.Load<AutomatedRun>(Path.Combine("AutomatedRuns", resourceName));
#endif
        }

#if UNITY_EDITOR
        // Guarantees that the report is finalized if we are launched tests from the UTR window in the editor.
        private static bool playModeStateChangeListenerSet = false;
        public void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                ReportingManager.FinalizeReport();
            }
        }
#endif
    }
}
#endif