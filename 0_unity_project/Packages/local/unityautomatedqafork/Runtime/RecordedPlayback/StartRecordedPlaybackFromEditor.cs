#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEditor;
using Unity.AutomatedQA.Listeners;
using Unity.AutomatedQA;

namespace Unity.RecordedPlayback.Editor
{
    [ExecuteInEditMode]
    public class StartRecordedPlaybackFromEditor : MonoBehaviour
    {
        public static void StartRecording()
        {
            RecordedPlaybackPersistentData.SetRecordingMode(RecordingMode.Record);
            RecordedPlaybackPersistentData.CleanRecordingData();
            CreateInitializer();
            EditorApplication.isPlaying = true;
        }

        public static void StartPlayback(string recordingFilePath)
        {
            ReportingManager.IsPlaybackStartedFromEditorWindow = true;
            RecordedPlaybackPersistentData.SetRecordingMode(RecordingMode.Playback, recordingFilePath);
            RecordedPlaybackPersistentData.SetRecordingDataFromFile(recordingFilePath);
            CreateInitializer();
            EditorApplication.isPlaying = true;
        }

        public static void EnterExtendModeAndRecord()
        {
            RecordedPlaybackPersistentData.SetRecordingMode(RecordingMode.Extend);
            CreateInitializer();
            EditorApplication.isPlaying = true;
        }

        private static void CreateInitializer()
        {
            DestroyExisting();
            new GameObject("StartRecordedPlaybackFromEditor").AddComponent<StartRecordedPlaybackFromEditor>();
        }

        internal static void DestroyExisting()
        {
            GameObject existing = GameObject.Find("StartRecordedPlaybackFromEditor");
            if (existing != null)
                DestroyImmediate(existing);
        }

        private IEnumerator Start()
        {
            // Wait for 1 frame to avoid initializing too early
            yield return null;

            if (!RecordedPlaybackController.Initialized && Application.isPlaying && RecordedPlaybackPersistentData.GetRecordingMode() != RecordingMode.None)
            {
                ReportingManager.IsAutomatorTest = false;
                RecordedPlaybackController.Instance.Reset();
                ReportingManager.IsTestWithoutRecordingFile = ReportingManager.IsCrawler;
                RecordedPlaybackController.Instance.Begin();
            }

            if (!EditorApplication.isPlayingOrWillChangePlaymode || RecordedPlaybackPersistentData.GetRecordingMode() == RecordingMode.Playback && !ReportingManager.IsPlaybackStartedFromEditorWindow && EditorApplication.isPlaying)
            {
                // Destroys the StartRecordedPlaybackFromEditor unless it is currently transitioning to playmode
                DestroyImmediate(gameObject);
            }
        }
    }
}
#endif
