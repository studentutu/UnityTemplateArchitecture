#if UNITY_INCLUDE_TESTS
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.AutomatedQA;
using Unity.RecordedTesting;
using Unity.RecordedPlayback;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GeneratedAutomationTests
{
    public abstract class AutomatedTestSuite : AutomatedTestSuiteBase
    {
        protected string testName;

        [UnitySetUp]
        public virtual IEnumerator Setup()
        {
#if UNITY_EDITOR
            if (!playModeStateChangeListenerSet)
            {
                playModeStateChangeListenerSet = true;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            }
#endif
            ReportingManager.IsPlaybackStartedFromEditorWindow = false;
            ReportingManager.IsUTRTest = true;
            testName = NUnit.Framework.TestContext.CurrentContext.Test.FullName;
            yield return null;
        }

        [UnityTearDown]
        public virtual IEnumerator UnityTearDown()
        {
            if (CentralAutomationController.Exists())
            {
                CentralAutomationController.Instance.Reset();
            }
            if (RecordedPlaybackController.Exists())
            {
                RecordedPlaybackController.Instance.Reset();
            }

            if (RecordedTesting.IsRecordedTest(testName))
            {
                ReportingManager.CreateMonitoringService();
            }

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

        private IEnumerable UnloadScenesExcept(string sceneName) {
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

        protected AutomatedRun GetAutomatedRun(string assetPath, string resourceName)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<AutomatedRun>(assetPath);
#else
            return UnityEngine.Resources.Load<AutomatedRun>(System.IO.Path.Combine("AutomatedRuns", resourceName));
#endif
        }
        
        protected IEnumerator LaunchAutomatedRun(AutomatedRun myRun)
        {
            ReportingManager.CurrentTestName = myRun.name;
            ReportingManager.IsAutomatorTest = true;
            // Run automation until complete
            CentralAutomationController controller = CentralAutomationController.Instance;
            controller.Run(myRun.config);
            while (!controller.IsAutomationComplete())
            {
                yield return null;
            }

        }

        protected void SetCustomReportData(string name, string value)
        {
            ReportingManager.SetCustomReportData(name, value);
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