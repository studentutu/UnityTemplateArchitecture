using System;
using UnityEngine;
using UnityEngine.Events;

namespace App.Core.Tools
{
    [ExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class MonoBehaviourUnityEvents : MonoBehaviour
    {
        [Serializable]
        public class BoolUnityEvent : UnityEvent<bool>
        {
        }

        [SerializeField] private bool IsPersistant = false;
        [SerializeField] internal UnityEvent onAwake = new UnityEvent();
        [SerializeField] internal UnityEvent onEnable = new UnityEvent();
        [SerializeField] internal UnityEvent onStart = new UnityEvent();
        [SerializeField] internal UnityEvent onUpdate = new UnityEvent();
        [SerializeField] internal UnityEvent onFixedUpdate = new UnityEvent();
        [SerializeField] internal UnityEvent onLateUpdate = new UnityEvent();
        [SerializeField] internal UnityEvent onDisable = new UnityEvent();
        [SerializeField] internal UnityEvent onDestroy = new UnityEvent();
        [SerializeField] internal BoolUnityEvent onPause = new BoolUnityEvent();

        private void Awake()
        {
            onAwake.Invoke();
            if (IsPersistant)
            {
                GameObjectExtensions.DontDestroyOnLoad(gameObject);
            }
        }

        private void OnEnable()
        {
            onEnable.Invoke();
        }

        private void Start()
        {
            onStart.Invoke();
        }

        private void FixedUpdate()
        {
            onFixedUpdate.Invoke();
        }

        private void Update()
        {
            onUpdate.Invoke();
        }

        private void LateUpdate()
        {
            onLateUpdate.Invoke();
        }

        private void OnDisable()
        {
            onDisable.Invoke();
        }

        private void OnDestroy()
        {
            onDestroy.Invoke();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            onPause.Invoke(pauseStatus);
        }
    }
}