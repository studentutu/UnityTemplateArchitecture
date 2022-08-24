using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.AutomatedQA
{

    public class Pulse : MonoBehaviour
    {
        private static readonly float maxLife = 2.5f;
        public bool IsSpeedUpActive;
        private bool IsMouseDown;
        private bool WasMouseDown;
        private float alphaAtTimeOfSpeedup;
        private float lifetime;
        private int alpha = 0;
        private float precentageSizeOfRingWhenMouseIsBeingHeld = 0.5f;
        private float currentLifetime;
        private float speedUpLifetime;
        private float finalWidthAndHeight;
        private RectTransform rect;

        public void Init(bool isMouseDown, bool startFast)
        {
            IsSpeedUpActive = startFast;
            IsMouseDown = isMouseDown;
            WasMouseDown = false;
            alpha = 255;
            currentLifetime = 0;
            lifetime = maxLife;
            speedUpLifetime = 1f;
            rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 0);
            if (IsSpeedUpActive)
                SpeedUp(false);
            StartCoroutine(Grow());
        }

        /// <summary>
        /// A user holding a click will trigger a ring that does not animate. When their click is released, begin animation.
        /// </summary>
        public void Continue() {
            WasMouseDown = true;
            IsMouseDown = false;
            StartCoroutine(Grow());
        }

        /// <summary>
        /// A new click was made before the current one was finished, so any existing ripple effects should quickly disappear.
        /// </summary>
        public void SpeedUp(bool killImmediate)
        {
            if (IsSpeedUpActive)
                return;
            IsSpeedUpActive = true;
            IsMouseDown = false;
            if (killImmediate) {
                StopAllCoroutines();
                GetComponent<RawImage>().color = new Color32(255, 255, 255, 0);
                GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);
                VisualFxManager.ReturnPulseRing(gameObject);
            }
            lifetime = maxLife; // Reset.
            currentLifetime = speedUpLifetime;
            alphaAtTimeOfSpeedup = alpha;
        }

        private IEnumerator Grow()
        {
            // Calculate a max width & height value based on whichever is smaller, screen width or screen height.
            finalWidthAndHeight = Screen.width > Screen.height ? Screen.height / 5f : Screen.width / 5f;
            RawImage img = GetComponent<RawImage>();
            rect.localScale = new Vector3(1.5f, 1.5f, 1);
            float widthAndHeight = 0f;
            currentLifetime = lifetime;

            // If resuming after mousedown, continue where we left off, rather than resetting these to their initial values.
            if (WasMouseDown)
            {
                lifetime = maxLife - VisualFxManager.PulseInterval + Time.deltaTime;
                widthAndHeight = rect.sizeDelta.x;
            }
            // Mouse is pressed. Quickly pulse to a certain distance and hold. Do not remove ring or grow further.
            else if (IsMouseDown)
            {
                lifetime = VisualFxManager.PulseInterval;
                finalWidthAndHeight = precentageSizeOfRingWhenMouseIsBeingHeld * finalWidthAndHeight;
                currentLifetime = precentageSizeOfRingWhenMouseIsBeingHeld * currentLifetime;
                widthAndHeight = precentageSizeOfRingWhenMouseIsBeingHeld * finalWidthAndHeight;
            }

            while (currentLifetime >= 0)
            {
                float currentWidthAndHeight = finalWidthAndHeight * (widthAndHeight / finalWidthAndHeight);
                rect.sizeDelta = new Vector2(currentWidthAndHeight, currentWidthAndHeight); // Increase ring pulse size over time.

                if (!IsMouseDown)
                {
                    // Become transparent over time. If speeding up, smoothly and quickly adjust increasing alpha.
                    if (IsSpeedUpActive)
                    {
                        alpha = (int)(currentLifetime / speedUpLifetime * alphaAtTimeOfSpeedup) / 2;
                    }
                    else if (WasMouseDown)
                    {
                        alpha = (int)(currentLifetime / VisualFxManager.PulseInterval * 255);
                    }
                    else 
                    {
                        alpha = (int)(currentLifetime / lifetime * 255);
                    }
                    
                    img.color = new Color32(255, 255, 255, (byte)(alpha < 0 ? 0 : alpha));
                }

                // Grow ring size. Regardless of speeding up, we do not want the ripple to speed up. We only want the ring to disappear faster. Use lifetime instead of speedUpLifetime to achieve that.
                if(!IsMouseDown)
                    widthAndHeight += Time.deltaTime / lifetime * finalWidthAndHeight;
                currentLifetime -= Time.deltaTime;
                yield return null;
            }

            WasMouseDown = false;
            if (!IsMouseDown)
                VisualFxManager.ReturnPulseRing(gameObject);
        }
    }
}