#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity.AutomatedQA.Editor
{
    [ExecuteInEditMode]
    public class StartAutomatedQAFromEditor : MonoBehaviour
    {
        private AQALogger logger;

        [SerializeField]
        [HideInInspector]
        private AutomatedRun.RunConfig runConfig;

        [SerializeField]
        [HideInInspector]
        private string AutomatorName;

        // Supports running multiple automators without changing playmode.
        public static bool runWhileEditorIsAlreadyStarted { get; set; }

        public static void StartAutomatedRun(AutomatedRun run)
        {
            var go = new GameObject("StartAutomatedQAFromEditor");
            var init = go.AddComponent<StartAutomatedQAFromEditor>();
            init.runConfig = run.config;
            init.AutomatorName = run.ToString().Replace("(Unity.AutomatedQA.AutomatedRun)", string.Empty).Trim();
            EditorApplication.isPlaying = true;
            RecordingInputModule.isWorkInProgress = true;
        }

        private IEnumerator Start()
        {
            logger = new AQALogger();
            // Wait for 1 frame to avoid initializing too early
            yield return null;

            if (Application.isPlaying)
            {
                if (runConfig == null)
                {
                    logger.LogError($"runConfig is null");
                }

                ReportingManager.CurrentTestName = AutomatorName;
                CentralAutomationController.Instance.Run(runConfig);
            }
            
            if (!runWhileEditorIsAlreadyStarted && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Destroys the StartRecordedPlaybackFromEditor unless it is currently transitioning to playmode
                DestroyImmediate(this.gameObject);
            }
        }
    }
}
#endif