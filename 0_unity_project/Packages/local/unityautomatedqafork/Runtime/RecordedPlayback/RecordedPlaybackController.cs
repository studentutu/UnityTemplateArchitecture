using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.AutomatedQA;
using Unity.AutomatedQA.Listeners;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Unity.RecordedPlayback
{
    public class RecordedPlaybackController : MonoBehaviour
    {
        private RecordingInputModule inputModule = null;
        private AQALogger logger;

        public static bool Initialized { get; private set; }

        private static RecordedPlaybackController _instance = null;
        public static RecordedPlaybackController Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject(typeof(RecordedPlaybackController).ToString());
                    _instance = go.AddComponent<RecordedPlaybackController>();

                    // Singleton, persist between scenes to record/play across multiple scenes. 
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            logger = new AQALogger();
        }

        public void Begin()
        {
            if (Initialized)
            {
                return;
            }

            Initialized = true;

            if (!ReportingManager.IsTestWithoutRecordingFile && RecordedPlaybackPersistentData.GetRecordingMode() == RecordingMode.Playback && !File.Exists(RecordedPlaybackPersistentData.GetRecordingDataFilePath()))
            {
                logger.LogError($"Recorded Playback file does not exist.");
                return;
            }

            if (inputModule == null)
            {
                inputModule = gameObject.AddComponent<RecordingInputModule>();
            }
            if (RecordedPlaybackPersistentData.GetRecordingMode() == RecordingMode.Record)
            {
                gameObject.AddComponent<GameListenerHandler>();
            }
            SetEventSystem();
            VisualFxManager.SetUp(Instance.transform);
        }

        public void Reset()
        {
            _instance = null;
            Initialized = false;
            if (ReportingManager.IsPlaybackStartedFromEditorWindow)
            {
                GameObject inputObj = new List<GameObject>(FindObjectsOfType<GameObject>()).Find(x =>
                    x != gameObject && x.GetComponent<BaseInputModule>() && x.GetComponent<EventSystem>());
                if (inputObj != null)
                {
                    EventSystem gameEventSystem = inputObj.GetComponent<EventSystem>();
                    if (RecordingInputModule.Instance != null)
                        RecordingInputModule.Instance.GetComponent<EventSystem>().enabled = false;
                    gameEventSystem.enabled = true;
                    inputObj.GetComponent<BaseInputModule>().enabled = true;
                    EventSystem.current = inputObj.GetComponent<EventSystem>();
                }
            }
            DestroyImmediate(gameObject);
        }

        public void SaveRecordingSegment()
        {
            if (inputModule != null)
            {
                inputModule.SaveRecordingSegment();
            }
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += SceneLoadSetup;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= SceneLoadSetup;
        }

        void SceneLoadSetup(Scene scene, LoadSceneMode mode)
        {
            SetEventSystem();
        }

        public static bool Exists()
        {
            return _instance != null;
        }

        public static bool IsPlaybackActive()
        {
            return Exists() && _instance.inputModule.IsPlaybackActive();
        }

        public static bool IsPlaybackCompleted()
        {
            return Exists() && _instance.inputModule.IsPlaybackCompleted();
        }

        public static bool IsRecordingActive()
        {
            return Exists() && _instance.inputModule.RecordingMode == RecordingMode.Record;
        }

        public bool IsInitialized()
        {
            return Initialized;
        }

        /// <summary>
        /// Check if an EventSystem already exists at the time of recording or playback start.
        /// If one exists, set our EventSystem variables to the values defined by the existing system.
        /// Finally, disable the pre-existing system. There can only be one active EventSystem.
        /// </summary>
        void SetEventSystem()
        {
            if (!Initialized)
            {
                return;
            }

            if (EventSystem.current != null)
            {
                GameObject inputObj = new List<GameObject>(FindObjectsOfType<GameObject>()).Find(x =>
                    x != gameObject && x.GetComponent<BaseInputModule>() && x.GetComponent<EventSystem>());
                if (inputObj == null)
                {
                    logger.Log("No existing Event System & Input Module was found");
                    return;
                }

                RecordingInputModule ourModule = inputModule;
                StandaloneInputModule theirModule = inputObj.GetComponent<StandaloneInputModule>();
                BaseInputModule theirBaseModule = inputObj.GetComponent<BaseInputModule>();
                if (theirModule != null)
                {
                    ourModule.cancelButton = theirModule.cancelButton;
                    ourModule.submitButton = theirModule.submitButton;
                    ourModule.verticalAxis = theirModule.verticalAxis;
                    ourModule.horizontalAxis = theirModule.horizontalAxis;
                    ourModule.inputActionsPerSecond = theirModule.inputActionsPerSecond;
                    ourModule.repeatDelay = theirModule.repeatDelay;
                }

                EventSystem ourEventSystem = ourModule.GetComponent<EventSystem>();
                EventSystem theirEventSystem = inputObj.GetComponent<EventSystem>();
                ourEventSystem.firstSelectedGameObject = theirEventSystem.firstSelectedGameObject;
                ourEventSystem.sendNavigationEvents = theirEventSystem.sendNavigationEvents;
                ourEventSystem.pixelDragThreshold = theirEventSystem.pixelDragThreshold;

                theirBaseModule.enabled = theirEventSystem.enabled = false;
            }

            EventSystem.current = GetComponent<EventSystem>();
        }
    }
}