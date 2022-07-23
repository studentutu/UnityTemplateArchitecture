using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace App.Core.Tools
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [ExecuteAfter(typeof(ContentSizeFitter))]
    public class UpdateContentSizeFitter : UIBehaviour, 
        UnityEngine.UI.ICanvasElement,
        UpdateContentSizeFitter.IEditorFields
    {
        public interface IEditorFields
        {
            string GetNameOfForContentSizeFitter();
            string GetNameOfForRectTransform();

        }
        
        [SerializeField] private ContentSizeFitter fitterToReEnable;
        [SerializeField] private RectTransform fitterRectTransform;

        private bool IsAppQuit = false;
        private bool IsOnPause = false;

        /// <summary>
        /// if true will allow another update
        /// </summary>
        [NonSerialized] private bool _internalUpdate; 
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (fitterToReEnable == null)
            {
                fitterToReEnable = GetComponent<ContentSizeFitter>();
            }

            if (fitterToReEnable != null)
            {
                fitterRectTransform = fitterToReEnable.GetComponent<RectTransform>();
            }
            _internalUpdate = true;
        }
#endif

        protected override void OnEnable()
        {
            base.OnEnable();
            StartAnotherUpdate();
            _internalUpdate = true;
#if UNITY_EDITOR
            // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad))] works only in runtime!
            if (!IsApplicationPlaying())
            {
#pragma warning disable
                WaitForMilliseconds(200);
#pragma warning restore
            }
#endif
            
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);

        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
        }

        private void OnApplicationQuit()
        {
            IsAppQuit = true;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            IsOnPause = pauseStatus;
            _internalUpdate = true;
        }

        protected override void OnCanvasGroupChanged()
        {
            base.OnCanvasGroupChanged();
            UpdateContentFitter();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            UpdateContentFitter();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            UpdateContentFitter();
        }
        /// <summary>
        /// Forces Content Size Fitter to re-evaluate his content
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public void UpdateContentFitter()
        {
            _internalUpdate = true;
            StartAnotherUpdate();
        }

        private async void StartAnotherUpdate()
        {
            if (IsAppQuit || IsOnPause)
            {
                return;
            }
            _internalUpdate = false;
            if (!IsApplicationPlaying())
            {
                await WaitForMilliseconds(50);
                return;
            }

            if (gameObject != null && gameObject.activeInHierarchy)
            {
                StartCoroutine(WaitForFrameAndUpdate());
            }
        }

        private bool IsApplicationPlaying()
        {
            bool result = true;
#if UNITY_EDITOR
            result = Application.isPlaying;
#endif
            return result;
        }


        private IEnumerator WaitForFrameAndUpdate()
        {
            yield return null;
            ReCalculate();
        }

        private async Task WaitForMilliseconds(int milliseconds)
        {
            await Task.Delay(milliseconds);
            ReCalculate();
        }

        private void ReCalculate()
        {
            if (fitterToReEnable != null && fitterRectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(fitterRectTransform);
            }
        }
        
        string IEditorFields.GetNameOfForContentSizeFitter()
        {
            return nameof(fitterToReEnable);
        }

        string IEditorFields.GetNameOfForRectTransform()
        {
            return nameof(fitterRectTransform);
        }

        public void Rebuild(CanvasUpdate executing)
        {
            if (executing == CanvasUpdate.PostLayout)
            {
                InternalUpdate();
            }
        }

        public void LayoutComplete()
        {
            InternalUpdate();
        }

        public void GraphicUpdateComplete()
        {
            InternalUpdate();
        }

        private void InternalUpdate()
        {
            if (_internalUpdate)
            {
                StartAnotherUpdate();
            }
        }
    }
}