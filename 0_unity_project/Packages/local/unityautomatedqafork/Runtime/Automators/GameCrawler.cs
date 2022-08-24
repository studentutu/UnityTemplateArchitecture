using System.Collections;
using System.Collections.Generic;
using Unity.AutomatedQA.Listeners;
using Unity.RecordedPlayback;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unity.AutomatedQA
{
    public class GameCrawler : MonoBehaviour
    {
        // TODO: Support random key presses as well, such as "Escape" which often is the only way to open certain menus.

        public GameCrawlerAutomatorConfig Config { get; set; }
        private GameCrawlerAutomatorConfig defaultGameCrawlerConfig = new GameCrawlerAutomatorConfig(); // Use default values of an empty constructor.

        public static bool IsCrawling { get; set; }
        public static bool Stop { get; set; }
        public static bool IsStuck { get; set; }

        internal static List<AutomationListener> CurrentSnapshotOfActiveAndInteractableGameObjects { get; set; } = new List<AutomationListener>();
        internal static List<AutomationListener> LastPassSnapshotOfActiveAndInteractableGameObjects { get; set; } = new List<AutomationListener>();
        protected List<RecordingInputModule.TouchData> Steps = new List<RecordingInputModule.TouchData>();
        private (int index, RecordingInputModule.TouchData touch, float time) LastRecordedAction;
        private float TimeSinceLastStateChange { get; set; }
        private (bool isInProgress, GameObject parent) IsDropDownSelectionInProgress { get; set; }
        private float refreshGameListenerInterval = 2.5f;
        private float refreshGameListenerTimer = 0f;
        private float lastRefresh = 0f;
        private float startTime { get; set; }
        private float timeSpentStuck { get; set; }
        private bool isHighlightActivated { get; set; }

        /// <summary>
        /// Creates GameCrawler instance and starts a crawl.
        /// </summary>
        /// <param name="Config"></param>
        public void Initialize(GameCrawlerAutomatorConfig Config)
        {
            Stop = false;
            this.Config = Config;
            startTime = Time.time;
            isHighlightActivated = AutomatedQASettings.ActivateHighlightFeedbackFx;
            AutomatedQASettings.ActivateHighlightFeedbackFx = false; // TODO: We need to rework how we perform drags, or else there will be a lot of messy code to perform these without saving the interpolated events.
            RecordedPlaybackController.Instance.Begin();
            // We initialize as a recording, but then override as Crawler mode.
            ReportingManager.IsCrawler = true;
            ReportingManager.IsTestWithoutRecordingFile = IsCrawling = true;
            startTime = Time.time;
            StartCoroutine(Crawl());
            RecordedPlaybackAnalytics.SendGameCrawlerData(Config.RunUntilStuck, Config.CrawlTimeout, Config.SecondsToRunBeforeSkippingGenerationOfAReport,
                Config.WaitForNextStepTimeout, Config.MaxTimeStuckBeforeFailing);
        }

        public void Initialize()
        {
            Initialize(defaultGameCrawlerConfig);
        }

        private IEnumerator Crawl()
        {
            // Continue to attempt random actions in game until becoming stuck on a screen, or timing out. Record actions taken and any warnings or errors encountered.
            while (
                !Stop && 
                !IsStuck && 
                (Config.RunUntilStuck ||TimeSinceLastStateChange < Config.WaitForNextStepTimeout) && 
                (Config.RunUntilStuck || GetAdjustedTime() < Config.CrawlTimeout)
                )
            {

                // TODO: Sample heap size & other performance information to report on performance issues.
                if (refreshGameListenerTimer > refreshGameListenerInterval)
                {
                    refreshGameListenerTimer = 0f;
                    lastRefresh = GetAdjustedTime();
                    GameListenerHandler.Refresh();
                }
                refreshGameListenerTimer += GetAdjustedTime() - lastRefresh;

                if (GameListenerHandler.ActiveListeners.Any())
                {
                    if (CurrentSnapshotOfActiveAndInteractableGameObjects.GetUniqueObjectsBetween(GameListenerHandler.ActiveListeners).Any())
                    {
                        TimeSinceLastStateChange = 0f;
                    }

                    CurrentSnapshotOfActiveAndInteractableGameObjects = GameListenerHandler.ActiveListeners;
                    AutomationListener al = CurrentSnapshotOfActiveAndInteractableGameObjects.Random();

                    // Even though the object may be active, it may be completely hidden by another layer of active objects that would intercept the clicks.
                    while (al != null && !IsObjectAccessibleToRealUser(al.gameObject))
                    {
                        CurrentSnapshotOfActiveAndInteractableGameObjects.Remove(al);
                        if (!CurrentSnapshotOfActiveAndInteractableGameObjects.Any())
                        {
                            al = null;
                        }
                        else
                        {
                            al = CurrentSnapshotOfActiveAndInteractableGameObjects.Random();
                        }
                    }
                    if (al == null)
                        continue;

                    // Check if we are stuck (unable to leave the current screen).
                    /*
                        TODO: This has a limitation of not catching a "stuck" situation that allows for navigation between different screens.
                        For example, if a GameCrawler gets stuck in a menu system consisting of many menus, but is unable to return to the main game,
                        this logic will not detect getting stuck in that manner. We should consider how we might detect that and expand these capabilities.
                    */
                    if (!LastPassSnapshotOfActiveAndInteractableGameObjects.GetUniqueObjectsBetween(CurrentSnapshotOfActiveAndInteractableGameObjects).Any())
                    {
                        timeSpentStuck += GetAdjustedTime() - lastRefresh;
                        if (timeSpentStuck > Config.MaxTimeStuckBeforeFailing)
                        {
                            IsStuck = true;
                            AQALogger logger = new AQALogger();
                            logger.Log("Game Crawler became stuck. Review previous step's final screenshot to see point of no return.");
                        }
                    }
                    else 
                    {
                        timeSpentStuck = 0;
                    }

                    // Take active and interactable game objects and select a random one to perform actions on.
                    List<RecordingInputModule.TouchData> touches = DetermineBestTouchActionsToPerform(al);
                    foreach (RecordingInputModule.TouchData touch in touches)
                    {
                        RecordingInputModule.Instance.AddFullTouchData(touch);
                        yield return StartCoroutine(PerformAction(touch));
                        if (touch.eventType == RecordingInputModule.TouchData.type.press)
                        {
                            // Presses have a click and release action. Do not hold the click for more than a frame.
                            yield return new WaitForEndOfFrame();
                        }
                        else
                        {
                            yield return new WaitForSeconds(Config.WaitTimeBetweenAttemptingNextAction);
                        }
                        GameListenerHandler.Refresh();
                    }
                }
                TimeSinceLastStateChange += Time.deltaTime;
                yield return new WaitForSeconds(0.25f);
            }
            AutomatedQASettings.ActivateHighlightFeedbackFx = isHighlightActivated;
            ReportingManager.IsCrawler = IsCrawling = false;
        }

        private bool IsObjectAccessibleToRealUser(GameObject targetGameObjectFound)
        {
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
                bool targetIsNotOverlappedByOtherObjects = true;
                if (buttonReady && toggleReady && inputReady && canvasReady && targetGameObjectFound.TryGetComponent(out RectTransform rectTransform))
                {
                    // The local position within the rect transform where our click should be performed.
                    Vector3 localPos = new Vector3(rectTransform.rect.x / 2f, rectTransform.rect.y / 2f, 0);
                    Camera eventCamera = RecordingInputModule.Instance.GetEventCameraForCanvasChild(targetGameObjectFound);
                    Vector3 targetPositionFromOffset = RectTransformUtility.WorldToScreenPoint(eventCamera, rectTransform.TransformPoint(localPos));

                    // Determine if the click coordinates in the target GameObject would be intercepted by another GameObject that is rendered on top of it.
                    targetIsNotOverlappedByOtherObjects = RecordingInputModule.Instance.NoOverlappingObjectWillInterceptClick(targetGameObjectFound, targetPositionFromOffset, targetGameObjectFound.gameObject.name, true);
                }

                isTargetReady = buttonReady && toggleReady && inputReady && canvasReady && targetIsNotOverlappedByOtherObjects;
            }

            return isTargetReady;
        }

        private List<RecordingInputModule.TouchData> DetermineBestTouchActionsToPerform(AutomationListener al)
        {
            List<RecordingInputModule.TouchData> touches = new List<RecordingInputModule.TouchData>();
            RecordingInputModule.TouchData td = GetBasicTouchData(al);

            // Complete last started events.
            if (LastRecordedAction != default((int, RecordingInputModule.TouchData, float)) && LastRecordedAction.touch.eventType == RecordingInputModule.TouchData.type.press)
            {
                td.objectName = LastRecordedAction.touch.objectName;
                td.objectHierarchy = LastRecordedAction.touch.objectHierarchy;
                td.eventType = RecordingInputModule.TouchData.type.release;
            }
            else if (IsDropDownSelectionInProgress.isInProgress && LastRecordedAction.touch.eventType == RecordingInputModule.TouchData.type.release)
            {
                if (!al.GetComponentsInChildren<Toggle>().ToList().Any())
                {
                    return new List<RecordingInputModule.TouchData>();
                }
                GameObject targetOption = al.GetComponentsInChildren<Toggle>().ToList().Random().gameObject;
                td.objectName = targetOption.name;
                td.objectHierarchy = string.Join("/", AutomatedQaTools.GetHierarchy(targetOption));
                td.eventType = RecordingInputModule.TouchData.type.press;
                IsDropDownSelectionInProgress = (false, null);
            }
            else if (al.TryGetComponent(out InputField inF))
            {
                td.eventType = RecordingInputModule.TouchData.type.input;
                td.inputText = AutomatedQaTools.RandomString(10);
            }
            else if (al.TryGetComponent(out Dropdown dd))
            {
                td.eventType = RecordingInputModule.TouchData.type.press;
                IsDropDownSelectionInProgress = (true, al.gameObject);
            }
            else if (al.TryGetComponent(out Scrollbar sb))
            {
                td.eventType = RecordingInputModule.TouchData.type.drag;
                touches.Add(td);
                // TODO: Handle scrolling
                // touches.Add(td);
                return touches;
            }
            else if (al.TryGetComponent(out Slider sl))
            {
                if (sl.handleRect != null)
                {
                    // Slider drags use interpolations between two RecordingInputModule.TouchData actions. We need to provide both now.
                    td.eventType = RecordingInputModule.TouchData.type.drag;
                    td.position = sl.handleRect.position;
                    touches.Add(td);

                    RecordingInputModule.TouchData td2 = GetBasicTouchData(al);
                    bool vertical = sl.direction == Slider.Direction.BottomToTop || sl.direction == Slider.Direction.TopToBottom;
                    System.Random r = new System.Random((int)GetAdjustedTime());
                    int randomVal = r.Next(-200, 200);
                    td2.eventType = RecordingInputModule.TouchData.type.release;
                    td2.position = vertical ? new Vector2(sl.handleRect.position.x, sl.handleRect.position.y + randomVal) :
                        new Vector2(sl.handleRect.position.x + randomVal, sl.handleRect.position.y);

                    List<RecordingInputModule.TouchData> newTouchData = RecordingInputModule.Instance.GetTouchData();
                    newTouchData.AddRange(touches);
                    RecordingInputModule.Instance.SetTouchData(newTouchData);
                    touches.AddRange(RecordingInputModule.Instance.InterpolateDragEvents(RecordingInputModule.Instance.GetTouchData().Count - 2, td.position, td2.position));
                    touches.Add(td2);
                }
                return touches;
            }
            else if (al.TryGetComponent(out Button b) || al.TryGetComponent(out Toggle t) || al.TryGetComponent(out Selectable s))
            {
                td.eventType = RecordingInputModule.TouchData.type.press;
                td.position = new Vector2(al.transform.position.x + al.GetComponent<RectTransform>().sizeDelta.x / 2, al.transform.position.y + al.GetComponent<RectTransform>().sizeDelta.y / 2);
                GameListenerHandler.Refresh();
            }
            touches.Add(td);
            return touches;
        }

        private RecordingInputModule.TouchData GetBasicTouchData(AutomationListener al)
        {
            return new RecordingInputModule.TouchData
            {
                objectName = al.name,
                objectHierarchy = string.Join("/", AutomatedQaTools.GetHierarchy(al.gameObject)),
                timeDelta = 0f,
                inputDuration = 0.1f,
                positional = false,
                scene = SceneManager.GetActiveScene().name,
            };
        }

        /// <summary>
        /// Grabs the requested step's index, and waits until the step is executed.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private IEnumerator PerformAction(RecordingInputModule.TouchData action)
        {
            if (action == EMIT_COMPLETE)
            {
                RecordingInputModule.Instance.AddFullTouchData(EMIT_COMPLETE);
                Steps.Add(EMIT_COMPLETE);
            }
            int index = RecordingInputModule.Instance.GetTouchData().FindIndex(x => x == action);

            /*
                The InterpolateDragEvents method adds multiple RecordingInputModule.TouchData events (not part of a recording) to "smooth" a drag between two points.
                These dynamically-generated events were added when drag start was invoked (previous DoAction call). We need to adjust our requested index based on this behavior.
            */
            if (LastRecordedAction != default((int, RecordingInputModule.TouchData, float)) &&
                LastRecordedAction.touch.eventType == RecordingInputModule.TouchData.type.drag)
            {
                yield return PerformInterpolatedDragActions(LastRecordedAction.index + 1, index - LastRecordedAction.index);
            }
            LastRecordedAction = (index, action, GetAdjustedTime());

            AQALogger logger = new AQALogger();
            logger.Log($"Crawler performing action [{action.eventType} > {(string.IsNullOrEmpty(action.querySelector) ? action.objectName : action.querySelector)}].");
            
            while (!RecordingInputModule.Instance.UpdatePlay(index))
                yield return new WaitForEndOfFrame();
        }

        private IEnumerator PerformInterpolatedDragActions(int startIndex, int interpolatedEventCount)
        {
            for (int x = 0; x < interpolatedEventCount; x++)
            {
                while (!RecordingInputModule.Instance.UpdatePlay(startIndex + x))
                    yield return new WaitForEndOfFrame();
            }
        }

        public static RecordingInputModule.TouchData EMIT_COMPLETE = new RecordingInputModule.TouchData
        {
            pointerId = -1,
            eventType = RecordingInputModule.TouchData.type.none,
            timeDelta = 2f,
            position = new Vector3(0, 0),
            positional = false,
            waitSignal = "",
            emitSignal = "playbackComplete",
            objectName = "",
            objectTag = "",
            objectHierarchy = "",
            objectOffset = new Vector3(0, 0)
        };

        /// <summary>
        /// A crawl can be started at any time during editor play mode. 
        /// We want to consider the time it was initialized to be comparable to a Time.time of zero, so we should adjust it to be handled as such.
        /// </summary>
        /// <returns></returns>
        private float GetAdjustedTime() {
            return Time.time - startTime;
        }
    }
}