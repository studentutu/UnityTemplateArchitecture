using System;
using System.Collections;
using System.Collections.Generic;
using App.Core.Tools.Async;
using UnityEngine;
using UnityEngine.UI;

namespace App.Core.Tools
{
    [DisallowMultipleComponent]
    public class ScrollRectSnapToElement : MonoBehaviour
    {
        // The direction we are snapping in
        public enum SnapDirection
        {
            Horizontal,
            Vertical,
        }

        [SerializeField] private ScrollRect scrollRect; // the scroll rect to scroll
        [SerializeField] private SnapDirection direction; // the direction we are scrolling
        [SerializeField] private GameObject[] items; // how many items we have in our scroll rect
        [SerializeField] private bool enableForceToNormalized = false;
        [SerializeField] [HideInInspector]
        private AnimationCurve
            curve = AnimationCurve.Linear(0f, 0f, 1f,
                1f); // a curve for transitioning in order to give it a little bit of extra polish

        [SerializeField] [Range(1, 5f)]
        private float speed = 3f; // the speed in which we snap ( normalized position per second? )

        private Coroutine snapping = null;
        private int target = -1;

        [UnityEngine.Scripting.Preserve]
        public void SnapToElement(GameObject element)
        {
            var Localtarget = -1;
            for (int i = 0; i < items.Length && Localtarget == -1; i++)
            {
                if (items[i] == element)
                {
                    Localtarget = i;
                }
            }

            if (Localtarget >= 0)
            {
                target = Localtarget;
                StartSnapping();
            }
        }

        public void StartSnapping()
        {
            if (snapping != null)
            {
                StopCoroutine(snapping);
            }

            snapping = ThreadTools.StartCoroutine(SnapRect());
        }

        private void OnEnable()
        {
            scrollRect.verticalNormalizedPosition = 1;
            scrollRect.horizontalNormalizedPosition = 0;
            if (snapping != null)
            {
                StopCoroutine(snapping);
            }

            if (enableForceToNormalized)
            {
                snapping = ThreadTools.StartCoroutine(SnapToDefault());
            }
        }

        private IEnumerator SnapToDefault()
        {
            for (int i = 0; i < 30; i++)
            {
                yield return null;
                scrollRect.verticalNormalizedPosition = 1;
                scrollRect.horizontalNormalizedPosition = 0;
            }
        }

        private IEnumerator SnapRect()
        {
            if (scrollRect == null)
                throw new System.Exception("Scroll Rect can not be null");
            if (items.Length == 0)
                throw new System.Exception("Item count can not be zero");

            float startNormal = direction == SnapDirection.Horizontal
                ? scrollRect.horizontalNormalizedPosition
                : scrollRect.verticalNormalizedPosition; // find our start position
            float delta = 1f / (items.Length - 1); // percentage each item takes
            
            float endNormal = delta * target; // this finds the normalized value of our target
            float duration =
                Mathf.Abs((endNormal - startNormal) /
                          speed); // this calculates the time it takes based on our speed to get to our target
            
            float timer = 0f; // timer value of course
            while (timer < 1f) // loop until we are done
            {
                timer = Mathf.Min(1f, timer + (Time.deltaTime / duration)); // calculate our timer based on our speed
                float value =
                    Mathf.Lerp(startNormal, endNormal,
                        curve.Evaluate(timer)); // our value based on our animation curve, cause linear is lame

                if (direction ==
                    SnapDirection.Horizontal) // depending on direction we set our horizontal or vertical position
                {
                    scrollRect.horizontalNormalizedPosition = value;
                }
                else
                {
                    scrollRect.verticalNormalizedPosition = 1-value;
                }

                yield return new WaitForEndOfFrame(); // wait until next frame
            }

            snapping = null;
            target = -1;
        }
    }
}