using System;
using System.Collections;
using Unity.AutomatedQA;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Unity.RecordedPlayback
{

    [Serializable]
    public class RecordedPlaybackAutomatorConfig : AutomatorConfig<RecordedPlaybackAutomator>
    {
        public TextAsset recordingFile = null;
        public bool loadEntryScene = true;
    }
    public class RecordedPlaybackAutomator : Automator<RecordedPlaybackAutomatorConfig>
    {
        public override void BeginAutomation()
        {
            base.BeginAutomation();

            string recordingFileName = "";
            if (config.recordingFile != null)
            {
                logger.Log($"Using recording asset - recordingFile: {config.recordingFile.name}");
                RecordedPlaybackPersistentData.SetRecordingData(config.recordingFile.text);
                recordingFileName = config.recordingFile.name;
            }
            else
            {
                logger.Log($"Using RecordedPlaybackPersistentData - kRecordedPlaybackFilename: {RecordedPlaybackPersistentData.kRecordedPlaybackFilename}");
            }

            StartCoroutine(PlayRecording(recordingFileName));
        }
 
        private IEnumerator PlayRecording(string recordingFileName)
        {
            // Load scene
            var recordingData = RecordedPlaybackPersistentData.GetRecordingData<RecordingInputModule.InputModuleRecordingData>();
            RecordedPlaybackPersistentData.RecordedResolution = recordingData.recordedResolution;
            RecordedPlaybackPersistentData.RecordedAspectRatio = recordingData.recordedAspectRatio;
            yield return LoadEntryScene(recordingData);

            if (RecordedPlaybackController.Exists())
            {
                // Reset controller if a previous recording just finished playing
                RecordedPlaybackController.Instance.Reset();
            }
            RecordedPlaybackPersistentData.SetRecordingMode(RecordingMode.Playback, recordingFileName);
            RecordedPlaybackController.Instance.Begin();

            while (!RecordedPlaybackController.IsPlaybackCompleted())
            {
                yield return null;
            }

            EndAutomation();
        }

        private IEnumerator LoadEntryScene(RecordingInputModule.InputModuleRecordingData recordingData)
        {
            if (config.loadEntryScene && !string.IsNullOrEmpty(recordingData.entryScene))
            {
                logger.Log($"Load Scene {recordingData.entryScene}");
                bool isFoundScene = false;
                float timer = AutomatedQASettings.DynamicLoadSceneTimeout;
                AsyncOperation loadSceneAsync = null;
                try
                {
                    loadSceneAsync = SceneManager.LoadSceneAsync(recordingData.entryScene);
                }
                catch (Exception e)
                {
                    isFoundScene = false;
                    logger.LogException(e); 
                }

                if (!isFoundScene)
                {
                    yield break;
                }
                
                while (!loadSceneAsync.isDone && timer > 0)
                {
                    yield return null;
                    timer -= Time.deltaTime;
                }
                if (!loadSceneAsync.isDone && timer <= 0)
                {
                    yield return null;
                }
            }
            yield return WaitForFirstActiveScene(recordingData, 60);
        }

        private IEnumerator WaitForFirstActiveScene(RecordingInputModule.InputModuleRecordingData recordingData, int timeoutSecs)
        {
            var touchData = recordingData.GetAllTouchData();
            if (touchData.Count > 0)
            {
                var startTime = DateTime.UtcNow;
                var firstActionScene = touchData[0].scene;
                if (!string.IsNullOrEmpty(firstActionScene) && SceneManager.GetActiveScene().name != firstActionScene)
                {
                    logger.Log($"Waiting for scene {firstActionScene} to load");
                }
                while (!string.IsNullOrEmpty(firstActionScene) && SceneManager.GetActiveScene().name != firstActionScene)
                {
                    var elapsed = DateTime.UtcNow.Subtract(startTime).TotalSeconds;
                    logger.Log(elapsed);
                    if (elapsed >= timeoutSecs)
                    {
                        logger.LogError($"Timeout wile waiting for scene {firstActionScene} to load");
                        break;
                    }
                    yield return new WaitForSeconds(1);
                }
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (RecordedPlaybackController.Exists())
            {
                RecordedPlaybackController.Instance.Reset();
            }
        }
    }
}