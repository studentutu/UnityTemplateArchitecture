#if UNITY_INCLUDE_TESTS
using System.Collections;
using Unity.AutomatedQA;
using Unity.RecordedPlayback;
using Unity.RecordedTesting;
using UnityEngine;
using UnityEngine.TestTools;

namespace GeneratedAutomationTests
{
    public abstract class RecordedTestSuite : AutomatedTestSuite
    {
        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return base.Setup();

            ReportingManager.IsPlaybackStartedFromEditorWindow = false;
            ReportingManager.IsUTRTest = true;

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            ReportingManager.InitializeReport();

            if (AutomatedQASettings.hostPlatform == HostPlatform.Cloud &&
                AutomatedQASettings.buildType == BuildType.UnityTestRunner)
            {
                RecordedTesting.SetupCloudUTFTests(testName);
            }
            else
            {
                RecordedTesting.SetupRecordedTest(testName);
            }

            // Start playback
            CentralAutomationController.Instance.Reset();
            CentralAutomationController.Instance.AddAutomator<RecordedPlaybackAutomator>(new RecordedPlaybackAutomatorConfig
            {
                loadEntryScene = true,
            });
            CentralAutomationController.Instance.Run();

            // wait for playback to start
            while (!RecordedPlaybackController.Exists() || !RecordedPlaybackController.Instance.IsInitialized())
            {
                yield return null;
            }
        }     
    }
}
#endif