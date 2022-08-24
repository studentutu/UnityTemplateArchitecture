using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Unity.AutomatedQA
{
    public class VisualFxManager : MonoBehaviour
    {
        // TODO: Modify to handle non-UI GameObjects when implemented. 
        public static VisualFxManager Instance { get; set; }
        public static List<GameObject> PulseRings
        {
            get
            {
                _pulseRings.RemoveAll(x => x == null || !x.GetComponent<Pulse>());
                if (_pulseRings.Count == 0)
                {
                    _pulseRings.Add(MakePulseRing());
                }
                return _pulseRings;
            }
            set
            {
                _pulseRings = value;
            }
        }
        private static List<GameObject> _pulseRings = new List<GameObject>();

        public static List<GameObject> HighlightSquares
        {
            get
            {
                _highlightSquare.RemoveAll(x => x == null || !x.GetComponent<HighlightElement>());
                if (_highlightSquare.Count == 0)
                {
                    _highlightSquare.Add(MakeHighlightSquare());
                }
                return _highlightSquare;
            }
            set
            {
                _highlightSquare = value;
            }
        }
        private static List<GameObject> _highlightSquare = new List<GameObject>();

        public static List<GameObject> VisualFxCanvases
        {
            get
            {
                _visualFxCanvases.RemoveAll(x => x == null || !x.GetComponent<PulseManager>());
                if (_visualFxCanvases.Count == 0)
                {
                    _visualFxCanvases.Add(MakeVisualFxCanvas());
                }
                return _visualFxCanvases;
            }
            set
            {
                _visualFxCanvases = value;
            }
        }
        public static List<PulseManager> ActivePulseManagers {
            get
            {
                for(int i = 0; i < _activePulseManagers.Count; i++)
                {
                    if (_activePulseManagers[i] == null)
                    {
                        _activePulseManagers.RemoveAt(i);
                        i--;
                    }
                }
                return _activePulseManagers;
            }
            set
            {
                _activePulseManagers = value;
            }
        }
        public static List<PulseManager> _activePulseManagers = new List<PulseManager>();


        public static float PulseDuration = 3f;
        public static float PulseInterval = 0.65f;
        private static List<GameObject> _visualFxCanvases = new List<GameObject>();
        private static DragFeedback dragFeedbackManager { get; set; }
        private static GameObject visualFxGo { get; set; }
        private static GameObject circleObjPoolGo { get; set; }
        private static GameObject squareObjPoolGo { get; set; }
        private static GameObject canvasPoolGo { get; set; }
        private static bool lastDragInProgress { get; set; }
        private static FxEventType lastFxEventType { get; set; }
        private enum FxEventType { Click, Drag, Highlight }
        private static int maxPulsesOnScreenAtOnce = 2;
        private static int preMakeRingCount = 50;
        private static int preMakeSquareCount = 20;
        private static Texture2D ringTexture2d { get; set; }
        private static Texture2D squareTexture2D { get; set; }

        public static void SetUp(Transform parentTransform)
        {
            visualFxGo = new GameObject("VisualFx");
            visualFxGo.transform.SetParent(parentTransform);
            Instance = visualFxGo.AddComponent<VisualFxManager>();
            if (!Application.isPlaying ||
                !AutomatedQASettings.ActivatePlaybackVisualFx)
                return;

            canvasPoolGo = new GameObject("VisualFxCanvasObjectPool");
            canvasPoolGo.transform.SetParent(visualFxGo.transform);

            if (AutomatedQASettings.ActivateClickFeedbackFx)
            {
                // Pre-make [preMakeRingCount] ring pulses to negate any performance impact from generating/destroying a lot of GameObjects in a short period.
                circleObjPoolGo = new GameObject("ClickPulseObjectPool");
                circleObjPoolGo.transform.SetParent(visualFxGo.transform);
                ringTexture2d = new Texture2D(2, 2);
                ringTexture2d.LoadImage(Convert.FromBase64String(ringBase64));
                for (int i = 0; i < preMakeRingCount; i++)
                {
                    PulseRings.Add(MakePulseRing());
                }

                // Also make canvas container objects. Around a fifth the total of pulse rings will be plenty.
                for (int i = 0; i < preMakeRingCount / 5; i++)
                {
                    VisualFxCanvases.Add(MakeVisualFxCanvas());
                }
            }

            if (AutomatedQASettings.ActivateHighlightFeedbackFx)
            {
                // Pre-make [preMakeSquareCount] squares to negate any performance impact from generating/destroying a lot of GameObjects in a short period.
                squareObjPoolGo = new GameObject("HighlightSquareObjectPool");
                squareObjPoolGo.transform.SetParent(visualFxGo.transform);
                squareTexture2D = new Texture2D(2, 2);
                squareTexture2D.LoadImage(Convert.FromBase64String(squareBase64));
                for (int i = 0; i < preMakeSquareCount; i++)
                {
                    HighlightSquares.Add(MakeHighlightSquare());
                    VisualFxCanvases.Add(MakeVisualFxCanvas());
                }
            }
        }

        /// <summary>
        /// Creates a new DragFeedback GameObject on start of drag, positioned at point of click.
        /// Moves this GameObject until final drag release. Destroys GameObject at that point.
        /// Cannot use the same GameObject between drags due to TrailRenderer.Clear() not clearing previous positions properly, 
        /// and LineRenderer not clearing previous positions when setting positionCount = 0 or SetPositions() with an empty Vector3 array.
        /// </summary>
        /// <param name="isEndDrag"></param>
        /// <param name="position"></param>
        public void TriggerDragFeedback(bool isEndDrag, Vector3 position)
        {
            // TODO: When PT-2024 is fixed, delete this check.
            if (!Application.isEditor)
                return;

            if (!AutomatedQASettings.ActivatePlaybackVisualFx ||
                !AutomatedQASettings.ActivateDragFeedbackFx ||
                (!lastDragInProgress && isEndDrag)) //If a drag is marked as an end drag, but no drag was started, this was invoked by a mouse release on a normal click.
                return;
            lastFxEventType = FxEventType.Drag;

            if (isEndDrag && dragFeedbackManager != null)
            {
                lastDragInProgress = false;
                dragFeedbackManager.DeActivate(position);
                dragFeedbackManager = null;
            }
            else
            {
                // In some games, a drag event is triggered at playback start, but no actual drag is occuring, and no "release" event to the initial drag is ever triggered. Result is a visible trail from that spot to the first real drag.
                if (lastDragInProgress)
                {
                    if (dragFeedbackManager != null)
                        dragFeedbackManager.Move(position);
                    else
                        lastDragInProgress = false;
                    return;
                }
                lastDragInProgress = true;
                // Builds out a TrailRenderer that generates a Line in world space. Faded tail end is origin of drag, while opaque end is destination.
                GameObject dragFeedbackGo = new GameObject("DragFeedback");
                dragFeedbackManager = dragFeedbackGo.AddComponent<DragFeedback>();
                TrailRenderer trail = dragFeedbackGo.AddComponent<TrailRenderer>();
                trail.emitting = false;
                trail.startWidth = trail.endWidth = Camera.main.orthographic ? 0.3f : 1f;
                Material material = new Material(Shader.Find("Particles/Standard Unlit"));
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.color = new Color32(0, 215, 255, 255);
                trail.materials = new Material[] { material };
                trail.numCapVertices = trail.numCornerVertices = 25;
                Gradient gradient = new Gradient();
                Color color = new Color(0, 215, 255);
                gradient.SetKeys(
                    new GradientColorKey[] {
                    new GradientColorKey(color, 0.0f),
                    new GradientColorKey(color, 1.0f)
                    },
                    new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.4f, 1.0f)
                    });
                trail.colorGradient = gradient;
                trail.emitting = false;
                dragFeedbackGo.transform.SetParent(visualFxGo.transform);
                dragFeedbackManager.Activate(position);
            }
        }

        public void TriggerHighlightAroundTarget(GameObject target)
        {
            if (!AutomatedQASettings.ActivatePlaybackVisualFx ||
                !AutomatedQASettings.ActivateHighlightFeedbackFx)
                return;

            lastFxEventType = FxEventType.Highlight;
            if (target == null || !target.GetComponent<RectTransform>()) return; // TODO: Handle non UI elments

            GameObject highlighterGo = VisualFxCanvases[0];
            highlighterGo.transform.SetParent(visualFxGo.transform);
            highlighterGo.SetActive(true);

            GameObject square = HighlightSquares[0];
            square.transform.SetParent(highlighterGo.transform);
            //square.GetComponent<HighlightElement>().Init(target);

            RectTransform squareRect = square.GetComponent<RectTransform>();
            squareRect.anchorMin = squareRect.anchorMax = new Vector2(0, 0);

            // TODO: This works for some UI's, but not all. Due to some RectTransforms supplying a world position, and others not; with seemingly no way to check if it is one or the other.
            RectTransform targetRect = target.GetComponent<RectTransform>();
            Vector3 centerPointOfObjectOnScreen = targetRect.TransformPoint(targetRect.position);
            centerPointOfObjectOnScreen = !Camera.main.orthographic ? Camera.main.WorldToScreenPoint(centerPointOfObjectOnScreen) : targetRect.position;

            Vector2 sizeOfTarget = new Vector2();
            // If the calculated delta size is negative, the current transform is not dictating the size of the element. Look in parents for size.
            if (targetRect.sizeDelta.x < 0 || targetRect.sizeDelta.y < 0)
            {
                bool validSizeDelta = false;
                Transform currentTarget = targetRect.parent;
                while (!validSizeDelta && currentTarget != null)
                {
                    sizeOfTarget = targetRect.parent.GetComponent<RectTransform>().sizeDelta;
                    if (targetRect.sizeDelta.x < 0 && targetRect.sizeDelta.y < 0) validSizeDelta = true;
                    currentTarget = currentTarget.parent;
                }
            }

            if (sizeOfTarget.x == 0 || sizeOfTarget.y == 0)
                sizeOfTarget = targetRect.sizeDelta;

            int stretchBiasY = sizeOfTarget.y > sizeOfTarget.x ? 40 : 0;
            int stretchBiasX = sizeOfTarget.x > sizeOfTarget.y ? 40 : 0;
            squareRect.sizeDelta = new Vector2(sizeOfTarget.x + 50 + stretchBiasX, sizeOfTarget.y + 50 + stretchBiasY);
            squareRect.position = new Vector3(centerPointOfObjectOnScreen.x, centerPointOfObjectOnScreen.y, 0);
            RawImage img = square.GetComponent<RawImage>();
            img.color = new Color32(0, 255, 255, 200);
            square.SetActive(true);
            StartCoroutine(HandleHighlightRemoval(square, target, highlighterGo));
            VisualFxCanvases.RemoveAt(0);
            HighlightSquares.RemoveAt(0);

        }

        IEnumerator HandleHighlightRemoval(GameObject square, GameObject target, GameObject highlighterGo)
        {
            RawImage img = square.GetComponent<RawImage>();
            float maxLife = 2f;
            float currentLife = 0;
            byte startingAlpha = 200;
            while (currentLife < maxLife && (img != null && target != null && target.activeInHierarchy && target.activeSelf))
            {
                int newAlpha = (int)((maxLife - currentLife) / maxLife * startingAlpha);
                img.color = new Color32(0, 255, 255, (byte)(newAlpha < 0 ? 0 : newAlpha));
                yield return null;
                currentLife += Time.deltaTime;
            }
            ReturnHighlightSquare(square);
            ReturnVisualFxCanvas(highlighterGo);
        }

        public void TriggerPulseOnTarget(GameObject target, bool isMouseDown)
        {
            TriggerPulseOnTarget(target.transform.position, isMouseDown);
        }

        /// <summary>
        /// Renders a pulsing ripple effect over a target.
        /// </summary>
        /// <param name="target">Location to put ripple effect.</param>
        /// <param name="mouseDown">If a mouse has been pressed, but not yet released.</param>
        public void TriggerPulseOnTarget(Vector3 target, bool isMouseDown)
        {
            if (!AutomatedQASettings.ActivatePlaybackVisualFx ||
                !AutomatedQASettings.ActivateClickFeedbackFx)
                return;

            // Drag events also use mouseup logic, so if we were dragging and just released the click, don't also create a click pulse.
            if (lastFxEventType == FxEventType.Drag && !isMouseDown && ActivePulseManagers.Any())
            {
                ActivePulseManagers.Last().KillEarly(true); // Remove the IsMouseDown pulse that is currently holding (waiting for mouse release).
                return; // We don't want a pulse/ripple effect either at the mousedown location or mouseup location of a drag event.
            }
            lastFxEventType = FxEventType.Click;

            if (ActivePulseManagers.Any() && ActivePulseManagers.Last().IsMouseDown)
            {
                ActivePulseManagers.Last().Init(target, false);
            }
            else
            {
                // The number of pulses to immediately remove is the difference between the total active pulses and the requested count, plus the one pulse we are about to create. Or plus zero if we are not creating a new pulse, but triggering the last one to animate.
                int killImmediateCount = ActivePulseManagers.Count + 1 - maxPulsesOnScreenAtOnce;
                int index = 0;
                // Stop pulsing of any existing PulseManagers.
                while (index < ActivePulseManagers.Count)
                {
                    PulseManager pm = ActivePulseManagers[index];
                    if (pm == null || !pm)
                    {
                        ActivePulseManagers.RemoveAt(index);
                        continue;
                    }

                    bool killImmediately = killImmediateCount > 0 && index + 1 <= killImmediateCount;
                    if (killImmediately)
                    {
                        pm.GetComponent<PulseManager>().KillEarly(true);
                        killImmediateCount--;
                    }
                    else
                    {
                        pm.GetComponent<PulseManager>().KillEarly(false);
                        index++;
                    }
                }

                GameObject pulseManagerGo = VisualFxCanvases[0];
                pulseManagerGo.SetActive(true);
                pulseManagerGo.transform.SetParent(visualFxGo.transform, false);
                PulseManager pulseManager = pulseManagerGo.GetComponent<PulseManager>();
                pulseManager.enabled = true;
                pulseManager.Init(target, isMouseDown);
                ActivePulseManagers.Add(pulseManager);
                VisualFxCanvases.RemoveAt(0);
            }
        }

        /// <summary>
        /// Create and add a new PulseRing to the object pool.
        /// </summary>
        /// <returns></returns>
        public static GameObject MakePulseRing()
        {
            GameObject ring = new GameObject("Pulse");
            ring.transform.SetParent(circleObjPoolGo.transform);
            ring.SetActive(false);
            ring.AddComponent<RectTransform>();
            ring.AddComponent<Pulse>();
            RawImage img = ring.AddComponent<RawImage>();
            img.raycastTarget = false; // Do not block clicks.
            img.texture = ringTexture2d;
            return ring;
        }

        /// <summary>
        /// Create and add a new PulseRing to the object pool.
        /// </summary>
        public static GameObject MakeHighlightSquare()
        {
            GameObject square = new GameObject("HighlightSquare");
            square.transform.SetParent(squareObjPoolGo.transform);
            square.SetActive(false);
            square.AddComponent<RectTransform>();
            //square.AddComponent<HighlightElement>();
            //square.AddComponent<LineRenderer>();
            RawImage img = square.AddComponent<RawImage>();
            img.raycastTarget = false; // Do not block clicks.
            img.texture = squareTexture2D;
            return square;
        }

        /// <summary>
        /// Create and add a new VisualFxCanvas to the object pool.
        /// </summary>
        public static GameObject MakeVisualFxCanvas()
        {
            GameObject visualFxCanvasGo = new GameObject("VisualFxManager");
            visualFxCanvasGo.transform.SetParent(canvasPoolGo.transform);
            PulseManager pulseManager = visualFxCanvasGo.AddComponent<PulseManager>();
            pulseManager.enabled = false;
            Canvas canvas = visualFxCanvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; //Render on top of everything else.
            visualFxCanvasGo.SetActive(false);
            return visualFxCanvasGo;
        }

        /// <summary>
        /// Return ring GameObject to the pool for later use.
        /// </summary>
        /// <param name="ring"></param>
        public static void ReturnVisualFxCanvas(GameObject canvasObj)
        {
            PulseManager pm = canvasObj.GetComponent<PulseManager>();
            pm.enabled = false;
            canvasObj.transform.SetParent(canvasPoolGo.transform);
            canvasObj.SetActive(false);
            if (ActivePulseManagers.FindAll(x => x == pm).Any())
                ActivePulseManagers.Remove(ActivePulseManagers.Find(x => x == pm));
            VisualFxCanvases.Add(canvasObj);
        }

        /// <summary>

        /// <summary>
        /// Return ring GameObject to the pool for later use.
        /// </summary>
        /// <param name="ring"></param>
        public static void ReturnPulseRing(GameObject ring)
        {
            ring.transform.SetParent(circleObjPoolGo.transform);
            ring.SetActive(false);
            PulseRings.Add(ring);
        }

        /// <summary>
        /// Return square GameObject to the pool for later use.
        /// </summary>
        /// <param name="square">GameObject with RawImage of square</param>
        public static void ReturnHighlightSquare(GameObject square)
        {
            square.transform.SetParent(circleObjPoolGo.transform);
            square.SetActive(false);
            HighlightSquares.Add(square);
        }

        // Base64 PNG representing basic ring used in ripple effect.
        private static string ringBase64 = "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";
        private static string squareBase64 = "iVBORw0KGgoAAAANSUhEUgAAAPoAAAD6CAYAAACI7Fo9AAABhGlDQ1BJQ0MgcHJvZmlsZQAAKJF9kT1Iw0AcxV/TiiIVBTuIKGSonSyIijhKFYtgobQVWnUwufQLmjQkKS6OgmvBwY/FqoOLs64OroIg+AHi5uak6CIl/i8ptIjx4Lgf7+497t4BQqPCVDMwAaiaZaTiMTGbWxW7XxHAKAYQgSAxU0+kFzPwHF/38PH1LsqzvM/9OfqUvMkAn0g8x3TDIt4gntm0dM77xCFWkhTic+Jxgy5I/Mh12eU3zkWHBZ4ZMjKpeeIQsVjsYLmDWclQiaeJw4qqUb6QdVnhvMVZrdRY6578hcG8tpLmOs0RxLGEBJIQIaOGMiqwEKVVI8VEivZjHv5hx58kl0yuMhg5FlCFCsnxg//B727NwtSkmxSMAV0vtv0xBnTvAs26bX8f23bzBPA/A1da219tALOfpNfbWvgI6N8GLq7bmrwHXO4AQ0+6ZEiO5KcpFArA+xl9Uw4YvAV619zeWvs4fQAy1NXyDXBwCESKlL3u8e6ezt7+PdPq7wdrhnKkatouUQAAAAZiS0dEAAAA5gDmHUVQIAAAAAlwSFlzAAAuIwAALiMBeKU/dgAAAAd0SU1FB+UEHRItA3svdVgAAAAZdEVYdENvbW1lbnQAQ3JlYXRlZCB3aXRoIEdJTVBXgQ4XAAACVUlEQVR42u3a4QmAIBRG0YymePvP5ho2QZCFmnnO/wiEy1fgtgEAAAAAAAAAAADAVNKrp3MujhA6iUh9Qxc4TBV8Ejn8P/bdiYF/dGsOP1h1iw4LEDoIHRA6IHRA6IDQAaEDtx3N3/DiIj4so/FlNIsOPt0BoQNCB4QOCB0QOiB0QOggdEDogNABoQNCB4QOCB0QOggdEDogdEDogNABoQNCB4QOQncEIHRA6IDQAaEDQgeEDggdEDoIHRA6IHRA6IDQAaEDQgeEDkIHhA4IHRA6IHRA6IDQAaGD0AGhA0IHhA4IHRA6IHRA6IDQQeiA0AGhA0IHhA4IHRA6IHQQOiB0QOiA0AGhA0IHhA4IHYQOCB0QOiB0QOiA0AGhA0IHhA5CB4QOCB0QOiB0QOiA0AGhg9ABoQNCB4QOCB0QOiB0QOggdEDogNABoQNCB4QOCB0QOiB0EDogdEDogNABoQNCB4QOCB2EDggdEDogdEDogNABoQNCB6EDQgeEDggdEDogdEDogNABoYPQAaEDQgeEDggdEDogdEDoIHRA6IDQAaEDQgeEDggdEDoIHRA6IHRA6IDQAaEDQgeEDggdhA4IHRA6IHRA6IDQAaEDQgehA0IHhA4IHRA6IHRA6IDQQeiA0AGhA0IHhA4IHRA6IHRA6CB0QOiA0AGhA0IHhA4IHRA6CB0QOiB0QOiA0AGhA0IHLhzN35Bzccxg0QGhA0IHhA4IHYQOCB0QOvAVqfoJF2BgvIiqdi06WHSrDrOv+fPQBQ9TBA4AAAAAAAAAAAAAAADDnGdaHI1irGx/AAAAAElFTkSuQmCC";
    }
}