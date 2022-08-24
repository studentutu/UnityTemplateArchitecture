using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.AutomatedQA;
using Unity.RecordedPlayback;
using UnityEditor;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.AutomatedQA.Listeners;
#if AQA_USE_TMP
using TMPro;
#endif

namespace UnityEngine.EventSystems
{
    [AddComponentMenu("Automated QA/Recording Input Module")]
    /// <summary>
    /// A BaseInputModule designed for mouse / keyboard / controller input.
    /// </summary>
    /// <remarks>
    /// Input module for working with, mouse, keyboard, or controller.
    /// </remarks>
    public class RecordingInputModule : PointerInputModule
    {
        public static RecordingInputModule Instance { get; set; }
        private static readonly int maxScreenshots = 1000;
        private static readonly float dragRateLimit = 0.05f;
        private static readonly string playbackCompleteSignal = "playbackComplete";
        private static readonly string segmentCompleteSignal = "segmentComplete";

        [SerializeField]
        [HideInInspector]
        public static bool isWorkInProgress { get; set; }

        private static int screenshotCounter = 0;
        private static DateTime start = DateTime.Now;

        private float m_PrevActionTime;
        private Vector2 m_LastMoveVector;
        private int m_ConsecutiveMoveCount = 0;

        private Vector2 m_LastMousePosition;
        private Vector2 m_MousePosition;

        private PointerEventData m_InputPointerEvent;

        private InputModuleRecordingData _recordingData = new InputModuleRecordingData { touchData = new List<TouchData>() };
        private List<TouchData> playbackData = new List<TouchData>();
        private List<TouchData> recordingData = new List<TouchData>();
        private TouchData activeDrag;
        private string currentEntryScene;
        private GameObject lastPressedObject;

        private AQALogger logger;

        public RecordingMode RecordingMode
        {
            get
            {
                return _recordingMode;
            }
        }
        private RecordingMode _recordingMode;

        private StringEvent callback = new StringEvent();

        private HashSet<string> pendingSignals = new HashSet<string>();

        private HashSet<string> hierarchyWarnings = new HashSet<string>();

        internal static string ScreenshotFolderPath { get; set; }

        // Variables used by IsReadyForNextAction between invocations.
        private float currentPeriodBetweenTargetReadinessChecks { get; set; }
        private float lastActionTriggeredTime { get; set; }
        private static readonly float targetReadinessWaitInterval = 0.5f;

        // Variables used by GetTouchPosition() between invocations.
        private TouchData targetOriginData { get; set; }
        private GameObject targetGameObjectFound { get; set; }
        private Vector3 targetPositionFromOffset { get; set; }
        private bool targetIsNotOverlappedByOtherObjects { get; set; }
        private bool targetChecked { get; set; }

        protected RecordingInputModule()
        {
        }

        protected override void Awake()
        {
            base.Awake();
            logger = new AQALogger();
        }

        protected override void OnEnable()
        {
            Instance = this;
            InitConfigData();
            InitRecordingData();
            InitScenes();
            SendAnalytics();
            base.OnEnable();
        }

        private void InitConfigData()
        {
            _recordingMode = RecordedPlaybackPersistentData.GetRecordingMode();
        }

        private void InitRecordingData()
        {
            lastEventTime = Time.time;
            RecordableInput.Reset();
            ReportingManager.IsCompositeRecording = false;
            if (IsPlaybackActive() && !ReportingManager.IsTestWithoutRecordingFile)
            {
                _recordingData = RecordedPlaybackPersistentData.GetRecordingData<InputModuleRecordingData>();
                RecordedPlaybackPersistentData.RecordedResolution = _recordingData.recordedResolution;
                RecordedPlaybackPersistentData.RecordedAspectRatio = _recordingData.recordedAspectRatio;
                playbackData = _recordingData.GetAllTouchData();
                ReportingManager.InitializeDataForNewTest();
                ReportingManager.IsCompositeRecording = _recordingData.recordingType == InputModuleRecordingData.type.composite;
            }
            else if (_recordingMode == RecordingMode.Record)
            {
                _recordingData.entryScene = SceneManager.GetActiveScene().name;
            }
        }

        private void InitScenes()
        {
            ReportingManager.EntryScene = SceneManager.GetActiveScene().name;
        }

        private void SendAnalytics()
        {
            RecordedPlaybackAnalytics.SendRecordedPlaybackEnv();
        }

        public void SetConfigMode(RecordingMode mode, bool persist = false)
        {
            _recordingMode = mode;
            if (persist)
            {
                RecordedPlaybackPersistentData.SetRecordingMode(mode);
            }
        }

        private int _current_index = 0;
        public int GetCurrentIndex()
        {
            return _current_index;
        }

        private playbackExecutionState currentState => pendingSignals.Count > 0 ? playbackExecutionState.wait : playbackExecutionState.play;
        private float waitStartTime = 0f;
        private float timeAdjustment = 0f;
        private float lastEventTime = 0f;
        private float lastRecordingTime = 0f;

        protected virtual void Update()
        {
            RecordableInput.Update();

            if (!IsPlaybackActive() || ReportingManager.IsTestWithoutRecordingFile)
            {
                if (ReportingManager.IsTestWithoutRecordingFile)
                {
                    ReportingManager.CreateMonitoringService();
                }
                return;
            }
            ReportingManager.CreateMonitoringService();

            switch (currentState)
            {
                case playbackExecutionState.play:
                    UpdatePlay();
                    break;
                case playbackExecutionState.wait:
                    // we don't need to do anything here. we can update our timers when we receive the signal we're waiting on
                    break;
            }
        }

        public void Pause(string signal)
        {
            pendingSignals.Add(signal);
            waitStartTime = GetElapsedTime();
        }

        internal float GetLastEventTime()
        {
            return lastEventTime;
        }

        internal bool UpdatePlay(int overrideStepIndex = -1)
        {
            //Generated tests may directly choose the step execution order.
            if (overrideStepIndex >= 0)
            {
                _current_index = overrideStepIndex;
            }

            int iterations = 0;
            while (_current_index < playbackData.Count && playbackData[_current_index].timeDelta <= GetElapsedTime()
                                                    && (overrideStepIndex < 0 || overrideStepIndex == _current_index))
            {
                var td = playbackData[_current_index];
                if (!IsReadyForNextAction(td))
                    return iterations > 0;

                lastEventTime += td.timeDelta;
                if (td.eventType != TouchData.type.none)
                {
                    try
                    {
                        DoAction(_current_index);
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                    }
                }

                if (!string.IsNullOrEmpty(td.waitSignal))
                {
                    pendingSignals.Add(td.waitSignal);
                    waitStartTime = GetElapsedTime();
                }

                if (_recordingMode == RecordingMode.Extend && _current_index == playbackData.Count - 1 &&
                    td.emitSignal == playbackCompleteSignal)
                {
                    playbackData = new List<TouchData>();
                    _recordingMode = RecordingMode.Record;
                    logger.Log("Playback complete, begin recording new segment");
                }
                else if (!string.IsNullOrEmpty(td.emitSignal))
                {
                    if (ReportingManager.IsPlaybackStartedFromEditorWindow && !ReportingManager.IsAutomatorTest)
                    {
                        /*
                            This is a recording file launched from an editor window.
                            Its possible that other recordings will be launched from the same play session.
                            Support that by generating a test and resetting the ReportingManager.
                        */
                        ReportingManager.FinalizeReport();
                        ReportingManager.Reset();
                    }

                    callback.Invoke(td.emitSignal);
                    if (td.emitSignal == playbackCompleteSignal)
                    {
                        Instance.EndRecording();
                    }
                }
                // If this is generated code, each step needs to be handled and executed explicitly by the Driver.cs logic.
                if (overrideStepIndex > 0)
                    return true;
                ++_current_index;
                ++iterations;
            }

            return iterations > 0;
        }

        /// <summary>
        /// Determines if the next action in list of TouchData is ready to be executed. Provides a dynamic wait period, or a hard coded wait provided by the timeDelta value of TouchData.
        /// </summary>
        private bool IsReadyForNextAction(TouchData td)
        {
            // Reset stored values used to prevent duplication of work in later "perform action" logic.
            if (targetOriginData != td)
            {
                targetGameObjectFound = null;
                targetOriginData = td;
                targetChecked = false;
            }
            // Checking that a game object is completely ready for interaction is heavier than alternative wait logic. Since this is called every frame, only check every second.
            currentPeriodBetweenTargetReadinessChecks -= Time.deltaTime;

            // If TouchData is not related to a GameObject, we should ignore any readiness checs related to GameObjects.
            bool isGameObjectTouch = td.HasObject();

            // Has the next GameObject taken too long to become ready for the next interaction?
            if (Time.time - lastActionTriggeredTime >= (SceneManager.GetActiveScene().isLoaded ? AutomatedQASettings.DynamicWaitTimeout : AutomatedQASettings.DynamicLoadSceneTimeout))
            {
                currentPeriodBetweenTargetReadinessChecks = -targetReadinessWaitInterval;
                lastActionTriggeredTime = Time.time;
                return true;
            }

            // Are we in the `intervalBetweenTargetReadinessChecks` interval between readiness checks?
            if (AutomatedQASettings.UseDynamicWaits && currentPeriodBetweenTargetReadinessChecks > 0f)
            {
                return false;
            }

            // Even if we are using dynamic waits, it is possible for a GameObject to be ready instantly, resulting in actions being performed within a frame of each other. Use the timeDelta as a minimum wait time.
            if (td.timeDelta >= GetElapsedTime())
            {
                return false;
            }

            // If timeDelta has been exceeded, and we are using dynamic waits, check that the target GameObject is ready.
            if (AutomatedQASettings.UseDynamicWaits && isGameObjectTouch)
            {
                if (targetGameObjectFound == null)
                {
                    targetGameObjectFound = FindObject(td, ElementQuery.Instance.GetAllActiveGameObjects());
                }

                bool isTargetReady = false;
                bool readySelf = targetGameObjectFound != null && targetGameObjectFound.activeSelf && targetGameObjectFound.activeInHierarchy;
                if (readySelf)
                {
                    // Check that any buttons, toggles, inputs, are canvases are in a ready state.
                    bool buttonReady = targetGameObjectFound.GetComponent<Button>() != null ? targetGameObjectFound.GetComponent<Button>().interactable : true;
                    bool toggleReady = targetGameObjectFound.GetComponent<Toggle>() != null ? targetGameObjectFound.GetComponent<Toggle>().interactable : true;
                    bool inputReady = targetGameObjectFound.GetComponent<InputField>() != null ? targetGameObjectFound.GetComponent<InputField>().interactable : true;
                    CanvasGroup cg = targetGameObjectFound.GetComponent<CanvasGroup>();
                    bool canvasReady = cg != null ? cg.interactable : true;

                    /*
                     * Objects may be visible and interactable, but in an animation (for example). This can result in other objects intercepting the clicks.
                     * Wait until nothing would intercept our trigger. This is the least performant check, so only perform it if all other readiness checks have passed.
                    */
                    targetIsNotOverlappedByOtherObjects = true;
                    if (buttonReady && toggleReady && inputReady && canvasReady && targetGameObjectFound.TryGetComponent(out RectTransform rectTransform))
                    {
                        // The local position within the rect transform where our click should be performed.
                        Vector3 localPos = rectTransform.rect.min + td.objectOffset * rectTransform.rect.size + rectTransform.rect.size / 2f;
                        Camera eventCamera = GetEventCameraForCanvasChild(targetGameObjectFound);
                        targetPositionFromOffset = RectTransformUtility.WorldToScreenPoint(eventCamera, rectTransform.TransformPoint(localPos));

                        //// Is the object in the camera viewport/frustum?
                        if (!ValidateObjectIsVisibleOnScreen(targetPositionFromOffset, td.objectName, false))
                        {
                            return false;
                        }

                        // Determine if the click coordinates in the target GameObject would be intercepted by another GameObject that is rendered on top of it.
                        targetIsNotOverlappedByOtherObjects = NoOverlappingObjectWillInterceptClick(targetGameObjectFound, targetPositionFromOffset, td.objectName, true);
                        targetChecked = true;
                    }

                    isTargetReady = buttonReady && toggleReady && inputReady && canvasReady && targetIsNotOverlappedByOtherObjects;
                }

                if (!isTargetReady)
                {
                    currentPeriodBetweenTargetReadinessChecks = targetReadinessWaitInterval;
                    return false;
                }
            }

            // Reset values to ready states for next action to perform.
            currentPeriodBetweenTargetReadinessChecks = -targetReadinessWaitInterval;
            lastActionTriggeredTime = Time.time;
            return true;
        }

        private float GetElapsedTime()
        {
            return Time.time - lastEventTime - timeAdjustment;
        }

        public List<TouchData> GetTouchData()
        {
            return playbackData;
        }

        public void SetTouchData(List<TouchData> data)
        {
            playbackData = data;
        }

        public void AddTouchData(TouchData data)
        {
            playbackData.Add(data);
        }

        public void InsertTouchData(int index, params TouchData[] data)
        {
            if (index < playbackData.Count)
            {
                List<TouchData> tds = new List<TouchData>();
                for (int x = 0; x < playbackData.Count; x++)
                {
                    if (x == index)
                    {
                        tds.AddRange(data);
                    }
                    tds.Add(playbackData[x]);
                }
                playbackData = tds;
            }
            else
            {
                foreach (TouchData td in data)
                    AddTouchData(td);
            }
        }

        public void ClearTouchData()
        {
            playbackData = new List<TouchData>();
            recordingData = new List<TouchData>();
            _current_index = 0;
        }

        void DoAction(int index)
        {
            if (!IsPlaybackActive())
            {
                return;
            }
            var td = playbackData[index];
            if (_recordingMode == RecordingMode.Playback || ReportingManager.IsCrawler)
            {
                ReportingManager.StepData step = new ReportingManager.StepData();
                step.ActionType = td.eventType.ToString();
                step.Name = $"{(string.IsNullOrEmpty(td.objectName) ? $"{{Position x{td.position.x} y{td.position.y}}}" : td.objectName)}";
                step.Hierarchy = string.IsNullOrEmpty(td.objectHierarchy) ? "{N/A}" : td.objectHierarchy;
                step.QuerySelector = string.IsNullOrEmpty(td.querySelector) ? "{N/A}" : td.querySelector;
                ReportingManager.AddStep(step);
            }
            if (td.eventType != TouchData.type.drag)
            {
                CaptureScreenshots();
            }

            List<GameObject> objPool = ElementQuery.Instance.GetAllActiveGameObjects();
            GameObject targetObject = FindObject(td, objPool);
            Vector2 pos = GetTouchPosition(td, targetObject);
            if (_recordingMode == RecordingMode.Playback || ReportingManager.IsCrawler)
            {
                ReportingManager.UpdateCurrentStep(pos);
            }
            var touch = new Touch();
            touch.fingerId = td.pointerId;
            touch.position = pos;
            touch.rawPosition = pos;
            touch.tapCount = 1;
            touch.pressure = 1.0f; // standard touch is 1
            touch.maximumPossiblePressure = 1.0f;
            touch.type = TouchType.Direct;

            if (td.eventType == TouchData.type.input)
            {
                logger.LogDebug($"Simulating {td.eventType}");

                StartCoroutine(InputText(td));
            }
            else if (td.eventType == TouchData.type.press)
            {
                logger.LogDebug(td.HasObject() && !td.positional
                    ? $"Simulating {td.eventType} on object {td.GetObjectIdentifier()} at {touch.position}"
                    : $"Simulating {td.eventType} at screen position {touch.position}");

                touch.phase = TouchPhase.Began;
                automationEvents.Add(new AutomationEvent(touch, td.button));
                lastPressedObject = targetObject;

                // Simulate fake action in the old input system
                if (index + 1 < playbackData.Count && playbackData[index + 1].eventType == TouchData.type.release)
                {
                    var nextTd = playbackData[index + 1];
                    var upPos = pos + nextTd.GetScreenPosition() - td.GetScreenPosition();
                    if (nextTd.HasObject() && nextTd.objectName != td.objectName)
                    {
                        upPos = GetTouchPosition(nextTd);
                    }
                    RecordableInput.FakeMouseDown((int)td.button, pos, upPos, nextTd.timeDelta);
                    RecordableInput.FakeTouch(touch, upPos, nextTd.timeDelta);
                }
                else
                {
                    RecordableInput.FakeMouseDown((int)td.button, pos, pos);
                }

                VisualFxManager.Instance.TriggerPulseOnTarget(pos, true);
            }
            else if (td.eventType == TouchData.type.drag)
            {
                logger.LogDebug($"Simulating {td.eventType} at screen position {touch.position}");

                touch.deltaTime = td.timeDelta;
                touch.phase = TouchPhase.Moved;
                automationEvents.Add(new AutomationEvent(touch, td.button));

                if (td.HasObject() && index < playbackData.Count - 1)
                {
                    var drop = playbackData[index + 1];
                    if (drop.eventType == TouchData.type.release)
                    {
                        Vector2 dropPos = drop.GetScreenPosition();
                        if (drop.HasObject())
                        {
                            dropPos = GetTouchPosition(drop);
                            if (_recordingMode == RecordingMode.Playback || ReportingManager.IsCrawler)
                            {
                                ReportingManager.UpdateCurrentStep(dropPos);
                            }
                        }

                        playbackData = InterpolateDragEvents(index, pos, dropPos);
                    }
                }
                VisualFxManager.Instance.TriggerDragFeedback(false, pos);
            }
            else if (td.eventType == TouchData.type.release)
            {
                logger.LogDebug(td.HasObject() && !td.positional
                    ? $"Simulating {td.eventType} on object {td.GetObjectIdentifier()} at {touch.position}"
                    : $"Simulating {td.eventType} at screen position {touch.position}");

                touch.phase = TouchPhase.Ended;
                automationEvents.Add(new AutomationEvent(touch, td.button));

                // Invoke click action if press and release happen on the same object
                if (targetObject != null)
                {
                    var hasGameElement = targetObject.TryGetComponent(out GameElement gameElement);
                    if (hasGameElement && lastPressedObject != null && td.objectName == lastPressedObject.name)
                    {
                        gameElement.OnClickAction();
                    }

                    lastPressedObject = null;
                }

                VisualFxManager.Instance.TriggerPulseOnTarget(pos, false);
                VisualFxManager.Instance.TriggerDragFeedback(true, pos);
            }
            else if (td.eventType == TouchData.type.key)
            {
                logger.LogDebug($"Simulating {td.eventType} {td.keyCode}");

                KeyCode key;
                if (Enum.TryParse(td.keyCode, true, out key))
                {
                    RecordableInput.FakeKeyDown(key, td.inputDuration);
                }
                else
                {
                    logger.LogError($"Failed to parse keyCode \"{td.keyCode}\". Expected value of UnityEngine.KeyCode Enum.");
                }
            }
            else if (td.eventType == TouchData.type.keyName)
            {
                logger.LogDebug($"Simulating {td.eventType} {td.keyCode}");

                RecordableInput.FakeKeyDown(td.keyCode, td.inputDuration);
            }
            else if (td.eventType == TouchData.type.button)
            {
                logger.LogDebug($"Simulating {td.eventType} {td.keyCode}");

                RecordableInput.FakeButtonDown(td.keyCode, td.inputDuration);
            }

        }

        /// <summary>
        /// Looks for object using the tag and name.
        /// Since grabbing the objPool is not a light operation, the pool can be instantiated outside this method to allow 
        /// only a single call to grab the object pool when it is needed by multiple operations.
        /// </summary>
        /// <param name="td"></param>
        /// <param name="objPool"></param>
        /// <returns></returns>
        private GameObject FindObject(TouchData td, List<GameObject> objPool)
        {
            // Advanced finding through query selectors takes precidence.
            if (!string.IsNullOrEmpty(td.querySelector))
            {
                return ElementQuery.Find(td.querySelector);
            }

            if (td.objectTag != "Untagged" && !string.IsNullOrEmpty(td.objectTag))
            {
                var gameObjects = GameObject.FindGameObjectsWithTag(td.objectTag);
                foreach (var obj in gameObjects)
                {
                    if (obj.name == td.objectName)
                    {
                        return obj;
                    }
                }
            }

            var maxOutliers = int.MaxValue;
            var foundObjects = 0;
            GameObject result = null;
            foreach (GameObject obj in objPool)
            {
                if (obj.name == td.objectName)
                {
                    foundObjects++;
                    var h1 = new HashSet<string>(AutomatedQaTools.GetHierarchy(obj));
                    var h2 = new HashSet<string>(td.objectHierarchy.Split('/'));
                    h1.SymmetricExceptWith(h2);

                    var outliers = h1.Count;
                    if (result == null || outliers < maxOutliers || (outliers == maxOutliers && td.objectIndex == GetHierarchyIndex(obj)))
                    {
                        foundObjects = 1;
                        result = obj;
                        maxOutliers = outliers;
                    }
                }
                else
                {
                    var hasGameElement = obj.TryGetComponent(out GameElement gameElement);
                    if (hasGameElement && gameElement.isActiveAndEnabled && obj.name == td.objectName)
                    {
                        return obj;
                    }
                }
            }

            if (maxOutliers != 0 && foundObjects > 1 && !hierarchyWarnings.Contains(td.objectHierarchy))
            {
                logger.LogWarning($"Object hierarchy {td.objectHierarchy} has been changed, please update the recording file with the new path");
                hierarchyWarnings.Add(td.objectHierarchy);
            }

            return result;
        }

        private int GetHierarchyIndex(GameObject obj)
        {
            int index = 0;
            var parent = obj.transform.parent;
            if (parent == null)
            {
                foreach (GameObject child in obj.scene.GetRootGameObjects())
                {
                    if (child.name == obj.name && child.CompareTag(obj.tag))
                    {
                        if (child.gameObject == obj)
                            return index;
                        index++;
                    }
                }
            }
            else
            {
                foreach (Transform child in parent.transform)
                {
                    if (child.name == obj.name && child.CompareTag(obj.tag))
                    {
                        if (child.gameObject == obj)
                            return index;
                        index++;
                    }
                }
            }

            return index;
        }

        private RaycastResult? FindRayForObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return null;
            }

            List<RaycastResult> raycastResults = new List<RaycastResult>();

            RaycastResult? result = null;

            float minX = 0;
            float minY = 0;
            for (float x = 0; x < 1; x += .01f)
            {
                for (float y = 0; y < 1; y += .01f)
                {
                    var testPos = new Vector2(x * Screen.width, y * Screen.height);
                    var testResult = FindObjectWithRaycast(gameObject, testPos, raycastResults);
                    if (testResult != null)
                    {
                        minX = x;
                        minY = y;
                        result = testResult;
                        goto LoopEnd;
                    }
                }
            }

        LoopEnd:
            if (result != null)
            {
                return FindObjectCenter(gameObject, minX, minY, raycastResults);
            }

            return null;
        }

        private RaycastResult? FindObjectCenter(GameObject obj, float minX, float minY, List<RaycastResult> raycastResults)
        {
            float maxX = minX;
            float maxY = minY;
            for (float i = 0; i < 1 - minX; i += .01f)
            {
                var testPos = new Vector2((i + minX) * Screen.width, (i + minY) * Screen.height);
                var testResult = FindObjectWithRaycast(obj, testPos, raycastResults);
                if (testResult == null)
                {
                    break;
                }

                maxX = Math.Max(i + minX, maxX);
                maxY = Math.Max(i + minY, maxY);
            }

            var centerPos = new Vector2((minX + maxX) / 2 * Screen.width, (minY + maxY) / 2 * Screen.height);
            return FindObjectWithRaycast(obj, centerPos, raycastResults);
        }

        private RaycastResult? FindObjectWithRaycast(GameObject obj, Vector2 pos, List<RaycastResult> raycastResults = null)
        {
            if (raycastResults == null)
            {
                raycastResults = new List<RaycastResult>();
            }

            raycastResults.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = pos }, raycastResults);
            if (raycastResults.Count > 0)
            {
                foreach (var result in raycastResults)
                {
                    var currentObject = result.gameObject;
                    while (currentObject != null)
                    {
                        if (currentObject == obj)
                        {
                            return result;
                        }
                        var parent = currentObject.transform.parent;
                        currentObject = parent != null ? parent.gameObject : null;
                    }
                }
            }

            // TODO: extra raycasts could be bad in the case where we have to crawl the screen
            var gameElement = FindGameElementObject(Camera.main, pos);
            if (gameElement != null && gameElement.gameObject == obj)
            {
                return new RaycastResult { screenPosition = pos };
            }

            return null;
        }

        private GameElement FindGameElementObject(Camera cam, Vector2 pos)
        {
            GameElement gameElement;
            var ray = cam.ScreenPointToRay(pos);
            var hit2D = Physics2D.GetRayIntersection(ray);
            if (hit2D.collider != null && hit2D.collider.gameObject.TryGetComponent(out gameElement))
            {
                return gameElement;
            }
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject.TryGetComponent(out gameElement))
            {
                return gameElement;
            }

            return null;
        }

        private Vector2 GetTouchPosition(TouchData td, GameObject target = null)
        {
            if (td.positional || !td.HasObject())
            {
                return td.GetScreenPosition();
            }

            if (target == null)
            {
                if (!string.IsNullOrEmpty(td.querySelector))
                {
                    target = ElementQuery.Find(td.querySelector);
                }

                if (target == null)
                {
                    List<GameObject> objPool = ElementQuery.Instance.GetAllActiveGameObjects();
                    target = FindObject(td, objPool);
                }
            }

            // Check if the GameObject is in the visible camera frustum.
            if (target != null)
            {
                bool hasRectTransform = target.TryGetComponent(out RectTransform rectTransform);
                if (hasRectTransform)
                {
                    // The local position within the rect transform where our click should be performed.
                    Vector3 localPos = rectTransform.rect.min + td.objectOffset * rectTransform.rect.size + rectTransform.rect.size / 2f;
                    Camera eventCamera = GetEventCameraForCanvasChild(target);
                    targetPositionFromOffset = RectTransformUtility.WorldToScreenPoint(eventCamera, rectTransform.TransformPoint(localPos));

                    if (FindObjectWithRaycast(target, targetPositionFromOffset) != null)
                    {
                        // Is the target object rendered within the camera frustum, and thus visible on screen.
                        ValidateObjectIsVisibleOnScreen(targetPositionFromOffset, td.objectName);

                        // Determine if the click coordinates in the target GameObject would be intercepted by another GameObject that is rendered on top of it.
                        NoOverlappingObjectWillInterceptClick(target, targetPositionFromOffset, td.objectName);
                        return targetPositionFromOffset;
                    }
                }

                var cam = GetEventCameraForCanvasChild(target) ?? Camera.main;
                var objPos = cam.WorldToScreenPoint(target.transform.position);
                var testResult = FindObjectWithRaycast(target, objPos);
                if (testResult.HasValue)
                {
                    return testResult.Value.screenPosition;
                }
            }

            // If we are using dynamic waits for target readiness, then we may have already done this check and recorded the target position.
            if (AutomatedQASettings.UseDynamicWaits && targetOriginData == td && targetGameObjectFound != null && targetChecked)
            {
                if (targetIsNotOverlappedByOtherObjects)
                {
                    return targetPositionFromOffset;
                }
                ThrowGameObjectWillInterceptClickError();
            }

            var raycastResult = FindRayForObject(target);
            if (raycastResult == null)
            {
                // Throw error if object is not found to fail unit tests.
                logger.LogError($"Cannot play recorded action: object {td.GetObjectIdentifier()} does not exist or is not viewable on screen. Ensure the object exists and reposition the object inside the screen space.");
                // TODO skip this touch event?
                return td.GetScreenPosition();
            }
            return raycastResult.Value.screenPosition;
        }

        /// <summary>
        /// Determine if an event camera is associated with the parent canvas element. Using the correct camera, or null value, is required for generating accurate relative coordinates in the game world.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        internal Camera GetEventCameraForCanvasChild(GameObject gameObject)
        {
            Camera eventCamera = null;
            GameObject canvasGo = gameObject;
            while (eventCamera == null && canvasGo != null)
            {
                canvasGo.TryGetComponent(out Canvas canvas);
                if (canvas != null)
                {
                    canvasGo = canvas.gameObject;
                    if (canvas.TryGetComponent(out GraphicRaycaster gfxRaycaster))
                    {
                        eventCamera = gfxRaycaster.eventCamera;
                    }
                }
                canvasGo = canvasGo.transform.parent != null ? canvasGo.transform.parent.gameObject : null;
            }
            return eventCamera;
        }

        private bool ValidateObjectIsVisibleOnScreen(Vector3 positionFromoffset, string nameOfObject, bool throwError = true)
        {
            float distRectX = Vector3.Distance(new Vector3(Screen.width / 2, 0f, 0f), new Vector3(positionFromoffset.x, 0f, 0f));
            float distRectY = Vector3.Distance(new Vector3(0f, Screen.height / 2, 0f), new Vector3(0f, positionFromoffset.y, 0f));

            // Determine if the click coordinates in the target GameObject are off the screen.
            if (distRectX > Screen.width / 2 || distRectY > Screen.height / 2)
            {
                if (ReportingManager.IsCrawler)
                {
                    // Remove this action as the GameObject cannot be interacted with.
                    playbackData.RemoveAt(playbackData.Count - 1);
                    return true;
                }
                if (throwError)
                {
                    string msg =
                        $"Click position recorded relative to spot within the target GameObject \"{nameOfObject}\" is positioned outside of the camera frustum (is not visible to the camera). " +
                        $"Since this is a GameObject in the UI layer, this may mean that the object has not scaled or positioned properly in the current aspect ratio ({Screen.width}w X {Screen.height}h) " +
                        $"and current resolution ({Screen.currentResolution.width} X {Screen.currentResolution.height}) compared to " +
                        $" the recorded aspect ratio ({RecordedPlaybackPersistentData.RecordedAspectRatio.x}h X {RecordedPlaybackPersistentData.RecordedAspectRatio.y}w) and recorded resolution ({RecordedPlaybackPersistentData.RecordedResolution.x} X {RecordedPlaybackPersistentData.RecordedResolution.y}).";
                    if (AutomatedQASettings.ThrowGameObjectInvisibleToCamera)
                    {
                        logger.LogError(msg);
                    }
                    else
                    {
                        logger.LogWarning(msg);
                    }
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Find any objects that will intercept the performed click. This can be used to assert that there is no intercepting object, or simply check for intercepting objects.
        /// </summary>
        /// <param name="isSoftCheck">If true, no asserted failure will occur when overlapping objects are found, and we will simply notify the caller.</param>
        /// <returns></returns>
        internal bool NoOverlappingObjectWillInterceptClick(GameObject target, Vector3 posFromOffset, string nameOfObject, bool isSoftCheck = false)
        {
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = posFromOffset }, raycastResults);
            if (raycastResults.Count > 0)
            {
                if (raycastResults.First().gameObject != target)
                {

                    // First raycast hit may not be the GameObject that we are targetting. It may be a child element, like an icon or text inside of a button.
                    bool isChildOfTargetGameObject = false;
                    Transform tran = raycastResults.First().gameObject.transform.parent;
                    while (tran != null)
                    {
                        if (tran.gameObject == target)
                        {
                            isChildOfTargetGameObject = true;
                            break;
                        }
                        tran = tran.parent == null ? null : tran.parent.transform;
                    }
                    // Handle exceptions where !isChildOfTargetGameObject is not an error state.
                    bool exceptionFound = false;
                    /*
                     * If the target is a slider, it is possible for a touch event to not fire on the target slider, while still successfully performing a drag. 
                     * This only happens when rapidly dragging a slider back and forth.
                     * If the slider is in the raycastResults list, but not at the top of stack, then it is legitimately blocked by another GameObject.
                    */
                    exceptionFound = !raycastResults.FindAll(rcr => rcr.gameObject == target).Any() && target.GetComponent<Slider>();
                    if (!isChildOfTargetGameObject && !exceptionFound)
                    {
                        if (isSoftCheck)
                        {
                            return false;
                        }
                        if (ReportingManager.IsCrawler)
                        {
                            // Remove this action as the GameObject cannot be interacted with.
                            playbackData.RemoveAt(playbackData.Count - 1);
                            return false;
                        }
                        ThrowGameObjectWillInterceptClickError();
                    }
                }
            }
            return true;
        }

        private void ThrowGameObjectWillInterceptClickError()
        {
            logger.LogError($"Click position recorded relative to spot within the target GameObject \"{targetGameObjectFound.name}\" would be caught by the wrong GameObject, which is overlapping the target GameObject at the click coordinates. " +
                        $"Since this is a GameObject in the UI layer, this may mean that the object has not scaled or positioned properly in the current aspect ratio ({Screen.width}w X {Screen.height}h) and current resolution ({Screen.currentResolution.width} X {Screen.currentResolution.height}) compared to the recorded aspect ratio ({RecordedPlaybackPersistentData.RecordedAspectRatio.x}h X {RecordedPlaybackPersistentData.RecordedAspectRatio.y}w) and recorded resolution ({RecordedPlaybackPersistentData.RecordedResolution.x} X {RecordedPlaybackPersistentData.RecordedResolution.y}).");

        }

        private IEnumerator InputText(TouchData td)
        {
            GameObject input = null;
            if (!string.IsNullOrEmpty(td.querySelector))
            {
                input = ElementQuery.Find(td.querySelector);
            }

            if (input == null)
                input = FindObject(td, ElementQuery.Instance.GetAllActiveGameObjects());

            if (input == null)
                logger.LogError($"Input field could not be found [Name:{td.objectName}] [Hierarchy:{td.objectHierarchy}]");

            InputField inputField = null;
            if (input.TryGetComponent(out inputField))
            {
                inputField.text = string.Empty;
            }

#if AQA_USE_TMP
            TMP_InputField tmpInputField = null;
            if (!input.TryGetComponent(out tmpInputField))
            {
                // If this object does not have a TMP_InputField component, it's grandparent GameObject might.
                if (inputField == null && tmpInputField == null)
                {
                    if (input.TryGetComponent(out TMP_Text tmpText))
                    {
                        GameObject go1 = input.transform.parent.gameObject;
                        if (!go1.TryGetComponent(out tmpInputField))
                        {
                            GameObject go2 = go1.transform.parent.gameObject;
                            if (!go2.TryGetComponent(out tmpInputField))
                            {
                                AQALogger logger = new AQALogger();
                                logger.LogError($"Could not find TMP_InputField associated with text field (Selector: {(string.IsNullOrEmpty(td.querySelector) ? td.objectName : td.querySelector)}).");
                            }
                        }
                    }
                }
            }
            if (tmpInputField != null)
            {
                tmpInputField.text = string.Empty;
            }
#endif

            // If an input action started quickly after the input field was clicked, a pulse action may be obscuring what we type.
            if (VisualFxManager.ActivePulseManagers.Any())
                VisualFxManager.ReturnVisualFxCanvas(VisualFxManager.ActivePulseManagers.Last().gameObject);
            VisualFxManager.Instance.TriggerHighlightAroundTarget(input);

            // Get the time between each letter to wait, simulating typing. Do not exceed a time that would
            float timeBetweenLetters = td.inputDuration / td.inputText.Length;
            for (int i = 0; i < td.inputText.Length; i++)
            {
#if AQA_USE_TMP
                if (tmpInputField != null)
                {
                    tmpInputField.text += td.inputText[i];
                } else
#endif
                if (inputField != null)
                {
                    inputField.text += td.inputText[i];
                }
                if (i < td.inputText.Length - 1)
                    yield return new WaitForSeconds(timeBetweenLetters);
            }
        }

        [SerializeField] private string m_HorizontalAxis = "Horizontal";

        /// <summary>
        /// Name of the vertical axis for movement (if axis events are used).
        /// </summary>
        [SerializeField] private string m_VerticalAxis = "Vertical";

        /// <summary>
        /// Name of the submit button.
        /// </summary>
        [SerializeField] private string m_SubmitButton = "Submit";

        /// <summary>
        /// Name of the submit button.
        /// </summary>
        [SerializeField] private string m_CancelButton = "Cancel";

        [SerializeField] private float m_InputActionsPerSecond = 10;

        [SerializeField] private float m_RepeatDelay = 0.5f;

        /// <summary>
        /// Number of keyboard / controller inputs allowed per second.
        /// </summary>
        public float inputActionsPerSecond
        {
            get { return m_InputActionsPerSecond; }
            set { m_InputActionsPerSecond = value; }
        }

        /// <summary>
        /// Delay in seconds before the input actions per second repeat rate takes effect.
        /// </summary>
        /// <remarks>
        /// If the same direction is sustained, the inputActionsPerSecond property can be used to control the rate at which events are fired. However, it can be desirable that the first repetition is delayed, so the user doesn't get repeated actions by accident.
        /// </remarks>
        public float repeatDelay
        {
            get { return m_RepeatDelay; }
            set { m_RepeatDelay = value; }
        }

        /// <summary>
        /// Name of the horizontal axis for movement (if axis events are used).
        /// </summary>
        public string horizontalAxis
        {
            get { return m_HorizontalAxis; }
            set { m_HorizontalAxis = value; }
        }

        /// <summary>
        /// Name of the vertical axis for movement (if axis events are used).
        /// </summary>
        public string verticalAxis
        {
            get { return m_VerticalAxis; }
            set { m_VerticalAxis = value; }
        }

        /// <summary>
        /// Maximum number of input events handled per second.
        /// </summary>
        public string submitButton
        {
            get { return m_SubmitButton; }
            set { m_SubmitButton = value; }
        }

        /// <summary>
        /// Input manager name for the 'cancel' button.
        /// </summary>
        public string cancelButton
        {
            get { return m_CancelButton; }
            set { m_CancelButton = value; }
        }

        private bool ShouldIgnoreEventsOnNoFocus()
        {
            if (IsPlaybackActive())
            {
                return false;
            }

            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                case OperatingSystemFamily.Linux:
                case OperatingSystemFamily.MacOSX:
#if UNITY_EDITOR
                    if (EditorApplication.isRemoteConnected)
                        return false;
#endif
                    return true;
                default:
                    return false;
            }
        }

        public override void UpdateModule()
        {
            if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
            {
                if (m_InputPointerEvent != null && m_InputPointerEvent.pointerDrag != null && m_InputPointerEvent.dragging)
                {
                    ReleaseMouse(m_InputPointerEvent, m_InputPointerEvent.pointerCurrentRaycast.gameObject);
                }

                m_InputPointerEvent = null;

                return;
            }

            m_LastMousePosition = m_MousePosition;
            m_MousePosition = input.mousePosition;
        }

        private void ReleaseMouse(PointerEventData pointerEvent, GameObject currentOverGo)
        {
            ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

            var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // PointerClick and Drop events
            if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
                AddTouchData(pointerEvent, TouchData.type.release, pointerEvent.pointerPress);
            }
            else if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
            {
                var dropObject = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
                AddTouchData(pointerEvent, TouchData.type.release, dropObject);
            }
            else
            {
                AddTouchData(pointerEvent, TouchData.type.release);
            }

            pointerEvent.eligibleForClick = false;
            pointerEvent.pointerPress = null;
            pointerEvent.rawPointerPress = null;

            if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

            pointerEvent.dragging = false;
            pointerEvent.pointerDrag = null;

            // redo pointer enter / exit to refresh state
            // so that if we moused over something that ignored it before
            // due to having pressed on something else
            // it now gets it.
            if (currentOverGo != pointerEvent.pointerEnter)
            {
                HandlePointerExitAndEnter(pointerEvent, null);
                HandlePointerExitAndEnter(pointerEvent, currentOverGo);
            }

            m_InputPointerEvent = pointerEvent;
        }

        public override bool IsModuleSupported()
        {
            var input1 = input;
            return input1.mousePresent || input1.touchSupported;
        }

        public override bool ShouldActivateModule()
        {
            if (!base.ShouldActivateModule())
                return false;

            var shouldActivate = false;
            shouldActivate |= input.GetButtonDown(m_SubmitButton);
            shouldActivate |= input.GetButtonDown(m_CancelButton);
            shouldActivate |= !Mathf.Approximately(input.GetAxisRaw(m_HorizontalAxis), 0.0f);
            shouldActivate |= !Mathf.Approximately(input.GetAxisRaw(m_VerticalAxis), 0.0f);
            shouldActivate |= (m_MousePosition - m_LastMousePosition).sqrMagnitude > 0.0f;
            shouldActivate |= input.GetMouseButtonDown(0);

            if (input.touchCount > 0)
                shouldActivate = true;

            return shouldActivate;
        }

        /// <summary>
        /// See BaseInputModule.
        /// </summary>
        public override void ActivateModule()
        {
            if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
                return;

            base.ActivateModule();

            var mousePosition = input.mousePosition;
            m_MousePosition = mousePosition;
            m_LastMousePosition = mousePosition;

            var toSelect = eventSystem.currentSelectedGameObject;
            if (toSelect == null)
                toSelect = eventSystem.firstSelectedGameObject;

            eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
        }

        /// <summary>
        /// See BaseInputModule.
        /// </summary>
        public override void DeactivateModule()
        {
            base.DeactivateModule();
            ClearSelection();
        }

        public override void Process()
        {
            if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
            {
                return;
            }

            bool usedEvent = SendUpdateEventToSelectedObject();

            // case 1004066 - touch / mouse events should be processed before navigation events in case
            // they change the current selected GameObject and the submit button is a touch / mouse button.

            // touch needs to take precedence because of the mouse emulation layer
            // TODO: use synthetic touch data here

            if (IsPlaybackActive())
            {
                ProcessSyntheticTouchEvents();
            }
            else
            {
                if (!ProcessTouchEvents() && input.mousePresent)
                {
                    ProcessMouseEvent();
                }
            }

            if (eventSystem.sendNavigationEvents)
            {
                if (!usedEvent)
                {
                    usedEvent |= SendMoveEventToSelectedObject();
                }

                if (!usedEvent)
                {
                    SendSubmitEventToSelectedObject();
                }
            }
        }

        private bool ProcessTouchEvents()
        {
            for (int i = 0; i < input.touchCount; ++i)
            {
                Touch touch = input.GetTouch(i);

                if (touch.type == TouchType.Indirect)
                    continue;

                var pointer = GetTouchPointerEventData(touch, out var pressed, out var released);

                ProcessTouchPress(pointer, pressed, released);

                if (!released)
                {
                    ProcessMove(pointer);
                    ProcessDrag(pointer);
                }
                else
                    RemovePointerData(pointer);
            }

            return input.touchCount > 0;
        }

        private List<AutomationEvent> automationEvents = new List<AutomationEvent>();

        private void ProcessSyntheticTouchEvents()
        {
            if (!IsPlaybackActive())
            {
                return;
            }

            foreach (var automationEvent in automationEvents)
            {
                Touch touch = automationEvent.touch;
                if (touch.type == TouchType.Indirect)
                {
                    continue;
                }

                var pointer = GetTouchPointerEventData(touch, out var pressed, out var released);
                pointer.pointerId = touch.fingerId;
                pointer.button = automationEvent.button;

                ProcessTouchPress(pointer, pressed, released);

                if (!released)
                {
                    ProcessMove(pointer);
                    ProcessDrag(pointer);
                }
                else
                {
                    RemovePointerData(pointer);
                }
            }

            automationEvents.Clear();
        }

        /// <summary>
        /// This method is called by Unity whenever a touch event is processed. Override this method with a custom implementation to process touch events yourself.
        /// </summary>
        /// <param name="pointerEvent">Event data relating to the touch event, such as position and ID to be passed to the touch event destination object.</param>
        /// <param name="pressed">This is true for the first frame of a touch event, and false thereafter. This can therefore be used to determine the instant a touch event occurred.</param>
        /// <param name="released">This is true only for the last frame of a touch event.</param>
        /// <remarks>
        /// This method can be overridden in derived classes to change how touch press events are handled.
        /// </remarks>
        private void ProcessTouchPress(PointerEventData pointerEvent, bool pressed, bool released)
        {
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if (pressed)
            {
                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pressPosition = pointerEvent.position;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                if (pointerEvent.pointerEnter != currentOverGo)
                {
                    // send a pointer enter to the touched element if it isn't the one to select...
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                    pointerEvent.pointerEnter = currentOverGo;
                }

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

                // didn't find a press handler... search for a click handler
                if (newPressed == null)
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                float time = Time.unscaledTime;

                if (newPressed == pointerEvent.lastPress)
                {
                    var diffTime = time - pointerEvent.clickTime;
                    if (diffTime < 0.3f)
                        ++pointerEvent.clickCount;
                    else
                        pointerEvent.clickCount = 1;

                    pointerEvent.clickTime = time;
                }
                else
                {
                    pointerEvent.clickCount = 1;
                }

                pointerEvent.pointerPress = newPressed;
                pointerEvent.rawPointerPress = currentOverGo;

                pointerEvent.clickTime = time;

                // Save the drag handler as well
                pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (pointerEvent.pointerDrag != null)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);

                m_InputPointerEvent = pointerEvent;

                var activeObject = pointerEvent.pointerPress != null ? pointerEvent.pointerPress : pointerEvent.pointerDrag;
                AddTouchData(pointerEvent, TouchData.type.press, activeObject);
            }

            // PointerUp notification
            if (!released)
            {
                return;
            }

            ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

            // see if we mouse up on the same element that we clicked on...
            var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // PointerClick and Drop events
            if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
                AddTouchData(pointerEvent, TouchData.type.release, pointerEvent.pointerPress);
            }
            else if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
            {
                var dropObject = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
                AddTouchData(pointerEvent, TouchData.type.release, dropObject);
            }
            else
            {
                AddTouchData(pointerEvent, TouchData.type.release);
            }

            pointerEvent.eligibleForClick = false;
            pointerEvent.pointerPress = null;
            pointerEvent.rawPointerPress = null;

            if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

            pointerEvent.dragging = false;
            pointerEvent.pointerDrag = null;

            // send exit events as we need to simulate this on touch up on touch device
            ExecuteEvents.ExecuteHierarchy(pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler);
            pointerEvent.pointerEnter = null;

            m_InputPointerEvent = pointerEvent;
        }

        /// <summary>
        /// Calculate and send a submit event to the current selected object.
        /// </summary>
        private void SendSubmitEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return;

            var data = GetBaseEventData();
            if (input.GetButtonDown(m_SubmitButton))
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.submitHandler);

            if (input.GetButtonDown(m_CancelButton))
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.cancelHandler);
        }

        private Vector2 GetRawMoveVector()
        {
            Vector2 move = Vector2.zero;
            move.x = input.GetAxisRaw(m_HorizontalAxis);
            move.y = input.GetAxisRaw(m_VerticalAxis);

            if (input.GetButtonDown(m_HorizontalAxis))
            {
                if (move.x < 0)
                    move.x = -1f;
                if (move.x > 0)
                    move.x = 1f;
            }

            if (input.GetButtonDown(m_VerticalAxis))
            {
                if (move.y < 0)
                    move.y = -1f;
                if (move.y > 0)
                    move.y = 1f;
            }

            return move;
        }

        /// <summary>
        /// Calculate and send a move event to the current selected object.
        /// </summary>
        /// <returns>If the move event was used by the selected object.</returns>
        private bool SendMoveEventToSelectedObject()
        {
            float time = Time.unscaledTime;

            Vector2 movement = GetRawMoveVector();
            if (Mathf.Approximately(movement.x, 0f) && Mathf.Approximately(movement.y, 0f))
            {
                m_ConsecutiveMoveCount = 0;
                return false;
            }

            bool similarDir = (Vector2.Dot(movement, m_LastMoveVector) > 0);

            // If direction didn't change at least 90 degrees, wait for delay before allowing consecutive event.
            if (similarDir && m_ConsecutiveMoveCount == 1)
            {
                if (time <= m_PrevActionTime + m_RepeatDelay)
                    return false;
            }
            // If direction changed at least 90 degree, or we already had the delay, repeat at repeat rate.
            else
            {
                if (time <= m_PrevActionTime + 1f / m_InputActionsPerSecond)
                    return false;
            }

            var axisEventData = GetAxisEventData(movement.x, movement.y, 0.6f);

            if (axisEventData.moveDir != MoveDirection.None)
            {
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, axisEventData, ExecuteEvents.moveHandler);
                if (!similarDir)
                    m_ConsecutiveMoveCount = 0;
                m_ConsecutiveMoveCount++;
                m_PrevActionTime = time;
                m_LastMoveVector = movement;
            }
            else
            {
                m_ConsecutiveMoveCount = 0;
            }

            return axisEventData.used;
        }

        /// <summary>
        /// Process all mouse events.
        /// </summary>
        private void ProcessMouseEvent(int id = 0)
        {
            var mouseData = GetMousePointerEventData(id);
            var leftButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData;

            // Process the first mouse button fully
            ProcessMousePress(leftButtonData);
            ProcessMove(leftButtonData.buttonData);
            ProcessDrag(leftButtonData.buttonData);

            // Now process right / middle clicks
            ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData);
            ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData.buttonData);
            ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData);
            ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData.buttonData);

            if (!Mathf.Approximately(leftButtonData.buttonData.scrollDelta.sqrMagnitude, 0.0f))
            {
                var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(leftButtonData.buttonData.pointerCurrentRaycast.gameObject);
                ExecuteEvents.ExecuteHierarchy(scrollHandler, leftButtonData.buttonData, ExecuteEvents.scrollHandler);
            }
        }

        private bool SendUpdateEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;

            var data = GetBaseEventData();
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
            return data.used;
        }

        /// <summary>
        /// Calculate and process any mouse button state changes.
        /// </summary>
        private void ProcessMousePress(MouseButtonEventData data)
        {
            var pointerEvent = data.buttonData;
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if (data.PressedThisFrame())
            {
                PressMouse(pointerEvent, currentOverGo);
            }

            // PointerUp notification
            if (data.ReleasedThisFrame())
            {
                ReleaseMouse(pointerEvent, currentOverGo);
            }
        }

        private void PressMouse(PointerEventData pointerEvent, GameObject currentOverGo)
        {
            // Without an onFocusLeave event, we need to consider a mouse press to be the end of any ongoing text input action.
            GameListenerHandler.FinalizeAnyTextInputInProgress();

            pointerEvent.eligibleForClick = true;
            pointerEvent.delta = Vector2.zero;
            pointerEvent.dragging = false;
            pointerEvent.useDragThreshold = true;
            pointerEvent.pressPosition = pointerEvent.position;
            pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

            DeselectIfSelectionChanged(currentOverGo, pointerEvent);

            // search for the control that will receive the press
            // if we can't find a press handler set the press
            // handler to be what would receive a click.
            var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

            // didn't find a press handler... search for a click handler
            if (newPressed == null)
                newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            float time = Time.unscaledTime;

            if (newPressed == pointerEvent.lastPress)
            {
                var diffTime = time - pointerEvent.clickTime;
                if (diffTime < 0.3f)
                    ++pointerEvent.clickCount;
                else
                    pointerEvent.clickCount = 1;

                pointerEvent.clickTime = time;
            }
            else
            {
                pointerEvent.clickCount = 1;
            }

            pointerEvent.pointerPress = newPressed;
            pointerEvent.rawPointerPress = currentOverGo;

            pointerEvent.clickTime = time;

            // Save the drag handler as well
            pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

            if (pointerEvent.pointerDrag != null)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);

            m_InputPointerEvent = pointerEvent;

            var activeObject = pointerEvent.pointerPress != null ? pointerEvent.pointerPress : pointerEvent.pointerDrag;
            AddTouchData(pointerEvent, TouchData.type.press, activeObject);
        }

        private static float move_x;
        private static float move_y;

        protected override void ProcessMove(PointerEventData pointerEvent)
        {
            if (move_x == pointerEvent.delta.x && move_y == pointerEvent.delta.y)
            {
                base.ProcessMove(pointerEvent);
                return;
            }

            move_x = pointerEvent.delta.x;
            move_y = pointerEvent.delta.y;

            //AddTouchData(td);

            base.ProcessMove(pointerEvent);
        }

        protected override void ProcessDrag(PointerEventData pointerEvent)
        {
            base.ProcessDrag(pointerEvent);

            if (!pointerEvent.IsPointerMoving() ||
                Cursor.lockState == CursorLockMode.Locked ||
                pointerEvent.pointerDrag == null)
            {
                return;
            }

            if (pointerEvent.dragging && activeDrag == null)
            {
                activeDrag = AddTouchData(pointerEvent, TouchData.type.drag, pointerEvent.pointerDrag);
            }
        }

        internal void AddFullTouchData(TouchData td, bool setLastEventTime = false)
        {
            playbackData.Add(td);

            if (setLastEventTime)
            {
                lastEventTime += td.timeDelta;
            }
        }

        internal TouchData AddTouchData(PointerEventData pointerEvent, TouchData.type type, GameObject activeObject = null)
        {
            TouchData td = null;
            if (_recordingMode == RecordingMode.Record)
            {
                var now = Time.time;
                td = new TouchData
                {
                    pointerId = pointerEvent.pointerId,
                    eventType = type,
                    timeDelta = now - lastRecordingTime,
                    position = new Vector2(pointerEvent.position.x / Screen.width,
                        pointerEvent.position.y / Screen.height),
                    positional = activeObject == null,
                    scene = SceneManager.GetActiveScene().name,
                    button = pointerEvent.button
                };

                // Look for an object with a RecordingListener attached
                if (activeObject == null)
                {
                    var recordableObject = FindGameElementObject(Camera.main, pointerEvent.position);
                    if (recordableObject != null)
                    {
                        td.positional = false;
                        activeObject = recordableObject.gameObject;
                    }
                }

                if (activeObject != null)
                {
#if UNITY_EDITOR
                    Selection.activeGameObject = activeObject;
#endif
                    td.objectName = activeObject.name;
                    td.objectTag = activeObject.tag;
                    td.objectHierarchy = string.Join("/", AutomatedQaTools.GetHierarchy(activeObject));
                    td.objectIndex = GetHierarchyIndex(activeObject);
                    td.querySelector = ElementQuery.ConstructQuerySelectorString(activeObject);

                    // calculate offset inside the object
                    RectTransform rectTransform;
                    if (activeObject.TryGetComponent(out rectTransform))
                    {
                        var rect = rectTransform.rect;
                        Vector2 localPos;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, pointerEvent.position, pointerEvent.pressEventCamera, out localPos);

                        // offset is calculated in the range (-0.5, -0.5) to (0.5, 0.5) relative to the object's center
                        td.objectOffset = (localPos - rect.min - rect.size / 2f) / rect.size;
                    }
                }

                if (td.eventType == TouchData.type.release)
                {
                    activeDrag = null;
                }

                if (td.eventType != TouchData.type.drag)
                {
                    CaptureScreenshots();
                }
                lastRecordingTime = now;

                logger.LogDebug(td.HasObject() && !td.positional
                    ? $"Recording new {td.eventType} on object {td.GetObjectIdentifier()}"
                    : $"Recording new {td.eventType} at screen position {td.GetScreenPosition()}");
                recordingData.Add(td);
            }

            return td;
        }

        internal List<TouchData> InterpolateDragEvents(int index, Vector2 startPos, Vector2 endPos)
        {
            var newTouchData = new List<TouchData>();
            for (int i = 0; i < playbackData.Count; i++)
            {
                var td = playbackData[i];
                newTouchData.Add(td);
                if (i == index && td.eventType == TouchData.type.drag)
                {
                    newTouchData.AddRange(GetInterpolatedDragEvents(i, startPos, endPos));
                    var drop = playbackData[++i];
                    if (drop.eventType == TouchData.type.release)
                    {
                        drop.timeDelta = 0f;
                    }
                    newTouchData.Add(drop);
                }
            }

            return newTouchData;
        }

        private List<TouchData> GetInterpolatedDragEvents(int index, Vector2 startPos, Vector2 endPos)
        {
            var result = new List<TouchData>();
            var td = playbackData[index];
            if (index < playbackData.Count - 1 && td.eventType == TouchData.type.drag && playbackData[index + 1].eventType == TouchData.type.release)
            {
                var drop = playbackData[index + 1];
                var deltaPos = endPos - startPos;
                for (float i = dragRateLimit; i < drop.timeDelta + dragRateLimit; i += dragRateLimit)
                {
                    var delta = i > drop.timeDelta ? dragRateLimit - (i - drop.timeDelta) : dragRateLimit;
                    var totalDelta = Math.Min(i / drop.timeDelta, 1);
                    var screenPos = totalDelta * deltaPos + startPos;
                    var interpolatedTouch = new TouchData
                    {
                        pointerId = td.pointerId,
                        eventType = TouchData.type.drag,
                        timeDelta = delta * drop.timeDelta,
                        position = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height),
                        positional = true,
                        scene = SceneManager.GetActiveScene().name
                    };
                    result.Add(interpolatedTouch);
                }
            }

            return result;
        }

        public void SaveRecordingSegment()
        {
            if (playbackData.Count == 0)
            {
                logger.Log("List of recorded actions in this segment is empty");
                return;
            }

            var endEvent = new TouchData
            {
                eventType = TouchData.type.none,
                timeDelta = Time.time - lastRecordingTime,
                emitSignal = segmentCompleteSignal,
                scene = SceneManager.GetActiveScene().name
            };
            recordingData.Add(endEvent);

            var epoch = ((DateTimeOffset)start).ToUnixTimeSeconds();
            var filename = $"recording_segment_{_recordingData.recordings.Count}_{epoch}.json";
            var filepath = Path.Combine(AutomatedQASettings.PersistentDataPath, filename);
            logger.Log($"Writing {filepath}");
            var recordedSegment = new InputModuleRecordingData(recordingData);
            recordedSegment.entryScene = string.IsNullOrEmpty(currentEntryScene) ? _recordingData.entryScene : currentEntryScene;
            recordedSegment.SaveToFile(filepath);

            recordingData = new List<TouchData>();
            _recordingData.recordings.Add(new Recording { filename = filename });
            _recordingData.recordingType = InputModuleRecordingData.type.composite;
            currentEntryScene = SceneManager.GetActiveScene().name;
            lastRecordingTime += endEvent.timeDelta;
        }

        public void CaptureScreenshots()
        {
            if (AutomatedQASettings.EnableScreenshots)
            {
                StartCoroutine(CaptureScreenshot(0f));
                StartCoroutine(CaptureScreenshot(AutomatedQASettings.PostActionScreenshotDelay));
            }
        }

        public static void Screenshot()
        {
            if (Instance != null) Instance.CaptureScreenshots();
        }

        private IEnumerator CaptureScreenshot(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);

            // Limit the maximum number of screenshots for safety
            if (screenshotCounter >= maxScreenshots)
                yield break;

            string folder = Path.Combine(AutomatedQASettings.PackageAssetsFolderName, $"{_recordingMode} screenshots {start.ToString("yyyy-MM-dd-THH-mm-ss")}");
            string path = Path.Combine(Application.persistentDataPath, folder);
            string filename = $"{_recordingMode} screenshot {screenshotCounter++}.png";

            // ScreenCapture.CaptureScreenshot tries to handle pushing into persistent data path for us
            // but this forces us to handle for the other cases... :(
#if (UNITY_IOS || UNITY_ANDROID) && (!UNITY_EDITOR)
            ScreenshotFolderPath = folder;
            string file = Path.Combine(folder, filename);
#else
            string file = Path.Combine(path, filename);
            ScreenshotFolderPath = path;
#endif

            Directory.CreateDirectory(path);
            if (_recordingMode == RecordingMode.Playback || ReportingManager.IsCrawler)
            {
                ReportingManager.AddScreenshot(file);
            }
            yield return CaptureScreenshot(file);
        }

        // On Mac manually capture the screenshot to avoid "ignoring depth surface" warnings that come from ScreenCapture.CaptureScreenshot
        private IEnumerator CaptureScreenshot(string path)
        {
            /* 
               Do not take screenshots in batchmode due to it not supporting the code "yield return new WaitForEndOfFrame()", 
               which is required for both screenshot methods to function correctly.
               Also do not capture screenshots on Cloud runs at this time due to video recordings being available instead
            */
            if (!Application.isBatchMode && AutomatedQASettings.hostPlatform != HostPlatform.Cloud)
            {
                yield return new WaitForEndOfFrame();
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                Texture2D screenImage = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                screenImage.Apply();
                byte[] imageBytes = screenImage.EncodeToPNG();

                File.WriteAllBytes(path, imageBytes);
#else
                ScreenCapture.CaptureScreenshot(path);
#endif
            }
            yield return null;
        }

        private void OnApplicationQuit()
        {
            EndRecording();
        }

        public void EndRecording()
        {
            if ((_recordingMode != RecordingMode.Record && !ReportingManager.IsCrawler) && !ReportingManager.IsPlaybackStartedFromEditorWindow)
                return;

            if (ReportingManager.IsCrawler)
                recordingData.Add(GameCrawler.EMIT_COMPLETE);

            if (recordingData.Count == 0 && _recordingData.recordings.Count == 0)
            {
                RecordedPlaybackController.Instance.Reset();
                return;
            }
            else if (_recordingData.recordingType == InputModuleRecordingData.type.composite && recordingData.Count > 0)
            {
                SaveRecordingSegment();
            }

            GameListenerHandler.FinalizeAnyTextInputInProgress(); // In case the very last action was typing into a text field, but no action was taken afterwords to trigger the end of the input step's recording.
            _recordingData.touchData = recordingData;
            _recordingData.AddPlaybackCompleteEvent(Time.time - lastRecordingTime);
            _recordingData.recordedAspectRatio = RecordedPlaybackPersistentData.RecordedAspectRatio = new Vector2(Screen.width, Screen.height);
            _recordingData.recordedResolution = RecordedPlaybackPersistentData.RecordedResolution = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
            RecordedPlaybackPersistentData.SetRecordingData(_recordingData);
            playbackData = new List<TouchData>();

            // Allows multiple recordings to be played in the same session.
            if (!ReportingManager.IsAutomatorTest && !ReportingManager.IsCrawler)
            {
                RecordedPlaybackController.Instance.Reset();
            }
            ReportingManager.IsPlaybackStartedFromEditorWindow = isWorkInProgress = false;
        }

        /// <summary>
        ///   <para>Data container for a recorded action.</para>
        /// </summary>
        [Serializable]
        public class TouchData
        {
            public int pointerId;
            public type eventType;
            public float timeDelta;
            public Vector2 position;
            public bool positional;
            public string scene;
            public PointerEventData.InputButton button;

            public string waitSignal;
            public string emitSignal;

            public string keyCode;
            public string inputText;
            public float inputDuration;

            public string objectName;
            public string objectTag;
            public string objectHierarchy;
            public int objectIndex;
            public string querySelector;
            public Vector2 objectOffset;

            /// <summary>
            ///   <para>A type of recordable action performed by the user.</para>
            /// </summary>
            public enum type
            {
                none = 0,
                press = 1,
                release = 2,
                move = 3,
                drag = 4,
                key = 5,
                input = 7,
                button = 8,
                keyName = 9,
            }

            /// <summary>
            ///   <para>Was this action performed on an object.</para>
            /// </summary>
            public bool HasObject()
            {
                return !(string.IsNullOrEmpty(objectName) && string.IsNullOrEmpty(objectTag));
            }

            /// <summary>
            /// Get the full path to the object in the scene hierarchy
            /// </summary>
            /// <returns>The full path to the object in the scene hierarchy</returns>
            public string GetObjectScenePath()
            {
                if(string.IsNullOrEmpty(objectHierarchy))
                {
                    return objectName;
                }

                return objectHierarchy + "/" + objectName;
            }

            /// <summary>
            ///   <para>Get the coordinates on the screen where this action occured (for positional actions only).</para>
            /// </summary>
            public Vector2 GetScreenPosition()
            {
                return new Vector2(position.x * Screen.width, position.y * Screen.height);
            }

            /// <summary>
            ///   <para>Unique identifier to find the object interacted with.</para>
            /// </summary>
            public string GetObjectIdentifier()
            {
                if (!string.IsNullOrEmpty(querySelector))
                {
                    return querySelector;
                }

                return objectName;
            }
        }

        [Serializable]
        private struct AutomationEvent
        {
            public Touch touch;
            public PointerEventData.InputButton button;

            public AutomationEvent(Touch touch, PointerEventData.InputButton button)
            {
                this.touch = touch;
                this.button = button;
            }
        }

        [Serializable]
        public class Recording
        {
            public string filename;
        }

        [Serializable]
        public class InputModuleRecordingData : BaseRecordingData
        {
            private AQALogger logger = new AQALogger();

            public string entryScene = string.Empty;
            public type recordingType;
            public Vector2 recordedAspectRatio;
            public Vector2 recordedResolution;
            public List<Recording> recordings;
            public List<TouchData> touchData;

            public InputModuleRecordingData(type recordingType = type.single)
            {
                this.recordingType = recordingType;
                touchData = new List<TouchData>();
                recordings = new List<Recording>();
            }

            public InputModuleRecordingData(List<TouchData> touchData)
            {
                this.touchData = touchData;
                this.recordingType = type.single;
                recordings = new List<Recording>();
            }

            public enum type
            {
                single,
                composite
            }

            public List<TouchData> GetAllTouchData(string segmentDir = null, int depth = 0)
            {
                if (depth >= 10)
                {
                    logger.LogError($"Recursive limit exceeded while reading file segments,");
                    return new List<TouchData>();
                }

                var baseDir = segmentDir ?? AutomatedQASettings.PersistentDataPath;
                List<TouchData> combinedTouchData = new List<TouchData>();
                foreach (var recording in recordings)
                {
                    logger.Log($"Loading segment {recording.filename}");
                    var segment = FromFile(Path.Combine(baseDir, recording.filename));

                    combinedTouchData.AddRange(segment.GetAllTouchData(segmentDir, depth + 1));

                    // allows a composite recordings to be used as a segment
                    if (combinedTouchData.Count > 0 && combinedTouchData.Last().emitSignal == playbackCompleteSignal)
                    {
                        combinedTouchData.Last().emitSignal = segmentCompleteSignal;
                    }
                }

                foreach (var td in touchData)
                {
                    combinedTouchData.Add(td);
                }

                return combinedTouchData;
            }

            public void AddRecording(string recordingFileName)
            {
                logger.Log("recordingFileName " + recordingFileName);
                var newRecording = new Recording { filename = recordingFileName };
                logger.Log("newRecording " + newRecording);
                recordings.Add(newRecording);
            }

            public void AddPlaybackCompleteEvent(float timeDelta = 0f)
            {
                var endEvent = new TouchData
                {
                    eventType = TouchData.type.none,
                    timeDelta = timeDelta,
                    emitSignal = playbackCompleteSignal,
                    scene = SceneManager.GetActiveScene().name
                };

                touchData.Add(endEvent);
            }

            public static InputModuleRecordingData FromFile(string filepath)
            {
                var text = File.ReadAllText(filepath);
                return JsonUtility.FromJson<InputModuleRecordingData>(text);
            }
        }

        private enum playbackExecutionState
        {
            play,
            wait
        }

        public void SendSignal(string name)
        {
            pendingSignals.Remove(name);

            if (currentState == playbackExecutionState.play)
            {
                timeAdjustment += GetElapsedTime() - waitStartTime;
            }
        }


        [Serializable]
        public class StringEvent : UnityEvent<string>
        {
        }

        public bool IsPlaybackCompleted()
        {
            return (_recordingMode == RecordingMode.Playback || ReportingManager.IsCrawler) && _current_index >= playbackData.Count;
        }

        public bool IsPlaybackActive()
        {
            return _recordingMode == RecordingMode.Playback || ReportingManager.IsCrawler || _recordingMode == RecordingMode.Extend;
        }
    }
}