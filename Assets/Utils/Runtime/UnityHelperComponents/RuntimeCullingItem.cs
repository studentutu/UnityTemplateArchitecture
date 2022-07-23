using System;
using App.Core.CommonPatterns;
using UnityEngine;
using UnityEngine.Events;

namespace App.Core
{
    [ExecutionOrder(-102)]
    public class RuntimeCullingItem : MonoBehaviour
    {
        [SerializeField] private UnityEvent _onEnabledHandler = new UnityEvent();
        [SerializeField] private UnityEvent _onDisabledHandler = new UnityEvent();
        [SerializeField] private SerializableBoolFunc _isVisibleCustom = null;
        private bool IsVisible = true;

        internal bool IsVisibleCustom()
        {
            if (_isVisibleCustom == null)
            {
                return true;
            }

            return _isVisibleCustom.Invoke();
        }

        private void OnEnable()
        {
            var instance = RuntimeCullingUpdater.Instance;
            if (instance == null)
            {
                return;
            }

            EventBus.Subscription<SubScribeToRuntimeUpdateCulling>()
                .Send(this, true);
        }

        private void OnDisable()
        {
            EventBus.Subscription<SubScribeToRuntimeUpdateCulling>()
                .Send(this, false);
        }

        internal void OnNeedsToBeEnabled()
        {
            if (!IsVisible)
            {
                _onEnabledHandler?.Invoke();
            }

            IsVisible = true;
        }

        internal void OnNeedsToBeDisabled()
        {
            if (IsVisible)
            {
                _onDisabledHandler?.Invoke();
            }

            IsVisible = false;
        }
    }
}