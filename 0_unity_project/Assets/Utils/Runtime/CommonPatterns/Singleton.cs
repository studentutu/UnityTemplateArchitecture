using System;
using System.Reflection;
using UnityEngine;

namespace App.Core.Tools
{
    internal class ApplicationQuitClass
    {
        public static bool ApplicationIsQuitting = false;

        static ApplicationQuitClass()
        {
            Reset();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Reset()
        {
            ApplicationIsQuitting = false;
        }
    }

    public abstract class Singleton<T> : MonoBehaviour
        where T : Singleton<T>
    {
        private const string PARENT_NAME = "[Singleton]";
        private const string FORMATING_STRING = "{0} {1}";

        private static T _instance = null;
 
        private static string GetName()
        {
            return typeof(T).Name;
        }

        public static T Instance
        {
            get
            {
                if (_instance == null && !ApplicationQuitClass.ApplicationIsQuitting)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null && !ApplicationQuitClass.ApplicationIsQuitting)
                    {
                        if (typeof(T).GetCustomAttribute(
                            typeof(SingletonInstanceCheckNullAttribute), true) != null)
                        {
                            var name = GetName();

                            var intermediate = new GameObject(string.Format(FORMATING_STRING, PARENT_NAME, name))
                                .AddComponent<T>();
                            if (intermediate.IsLoadFromPrefab)
                            {
                                var prefab = Resources.Load<GameObject>(intermediate.GetPrefabPath);
                                if (prefab != null)
                                {
                                    DestroyImmediate(intermediate.gameObject);
                                    var go = Instantiate(prefab, null);
                                    go.name = prefab.name;
                                    intermediate = go as T;
                                    if (intermediate == null)
                                    {
                                        intermediate = go.GetComponentInChildren<T>();
                                    }

                                    if (intermediate == null)
                                    {
                                        intermediate =
                                            new GameObject(string.Format(FORMATING_STRING, PARENT_NAME, name))
                                                .AddComponent<T>();
                                        Debug.LogWarning(
                                            "Singleton self creator set to prefab mode, but script is missing! " +
                                            intermediate.GetPrefabPath);
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning(
                                        "Singleton self creator set to prefab mode, but no path is given! " +
                                        intermediate.GetPrefabPath);
                                }
                            }

                            _instance = intermediate;
                        }
                    }

                    if (_instance != null)
                    {
                        _instance.CheckInstance(_instance);
                    }
                }

                return _instance;
            }
        }

        protected virtual string GetPrefabPath { get; }

        protected virtual bool IsLoadFromPrefab
        {
            get { return false; }
        }

        protected virtual bool IsDontDestroy
        {
            get { return false; }
        }

        public static bool IsExist
        {
            get { return _instance != null; }
        }

        private void Awake()
        {
            CheckIsDuplicate();
        }

        private void Start()
        {
            CheckIsDuplicate();
        }

        internal void CheckIsDuplicate()
        {
            if (!IsExist && Instance == null)
            {
                Debug.LogWarning("Missing Singleton Of Type " + nameof(T));
            }

            if (IsExist && Instance != this)
            {
                Debug.LogWarning("Another Singleton On Scene!");
                DestroyImmediate(this.gameObject);
            }
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // [Unity.IL2CPP.CompilerServices.Il2CppSetOption(Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        private void CheckInstance(Singleton<T> instanceCheck)
        {
            if (instanceCheck != null && instanceCheck != this)
            {
                DestroyImmediate(this);
                return;
            }

            _instance = this as T;
            _instance.InitInstance();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            if (_instance.IsDontDestroy)
            {
                GameObjectExtensions.DontDestroyOnLoad(_instance.transform.root.gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                OnDestroySingletonCallback();
            }

            OnCleanUpAfter();
        }

        private void OnApplicationQuit()
        {
            ApplicationQuitClass.ApplicationIsQuitting = true;
            OnApplicationQuitCallback();
        }

        protected virtual void InitInstance()
        {
        }

        protected virtual void OnDestroySingletonCallback()
        {
        }

        protected virtual void OnCleanUpAfter()
        {
        }

        protected virtual void OnApplicationQuitCallback()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SingletonInstanceCheckNullAttribute : System.Attribute
    {
    }

    public abstract class SingletonPersistent<T> : Singleton<T> where T : SingletonPersistent<T>
    {
        protected override bool IsDontDestroy
        {
            get { return true; }
        }
    }

    [SingletonInstanceCheckNull]
    public abstract class SingletonSelfCreator<T> : Singleton<T> where T : SingletonSelfCreator<T>
    {
        protected abstract string PrefabPath { get; }

        protected override string GetPrefabPath
        {
            get { return PrefabPath; }
        }

        protected override bool IsLoadFromPrefab
        {
            get { return true; }
        }

        protected override bool IsDontDestroy
        {
            get { return true; }
        }
    }
}