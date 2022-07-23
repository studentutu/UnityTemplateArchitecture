using System;
using System.Collections;
using App.Core.Tools.Async.Hidden;
using UnityEngine;

namespace App.Core.Tools.Async
{
    public static class ThreadTools
    {
        private static ThreadToolsHelper _helper = null;

        private static ThreadToolsHelper Helper
        {
            get
            {
                if (_helper == null)
                {
                    _helper = ThreadToolsHelper.Instance;
                    if (_helper != null)
                    {
                        GameObjectExtensions.DontDestroyOnLoad(_helper);
                        Initialize();
                    }
                }

                return _helper;
            }
        }

        // Invoke on main thread
        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            if (_helper == null)
            {
                var instance = ThreadToolsHelper.Instance;
                if (instance != null)
                {
                    instance.Remove(EmptyFunction);
                    instance.Add(EmptyFunction);
#if UNITY_EDITOR
                    instance.hideFlags = HideFlags.HideAndDontSave;
#endif
                }
            }
        }

        public static Coroutine StartCoroutine(IEnumerator coroutine)
        {
            if (Helper == null)
            {
                return null;
            }

            return Helper.StartCoroutine(coroutine);
        }

        public static void StopCoroutine(Coroutine toStop)
        {
            if (Helper != null)
            {
                Helper.StopCoroutine(toStop);
            }
        }

        public static void InvokeInMainThread(this Action action)
        {
            Helper.Add(action);
        }

        public static void InvokeInMainThread<T>(this Action<T> action, T param)
        {
            Action clojure = () => action(param);
            Helper.Add(clojure);
        }

        public static void InvokeInMainThread<T1, T2>(this Action<T1, T2> action, T1 p1, T2 p2)
        {
            Action clojure = () => action(p1, p2);
            Helper.Add(clojure);
        }

        public static void InvokeInMainThread<T1, T2, T3>(this Action<T1, T2, T3> action, T1 p1, T2 p2, T3 p3)
        {
            Action clojure = () => action(p1, p2, p3);
            Helper.Add(clojure);
        }

        public static void InvokeInMainThread<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> action, T1 p1, T2 p2, T3 p3,
            T4 p4)
        {
            Action clojure = () => action(p1, p2, p3, p4);
            Helper.Add(clojure);
        }

        private static void EmptyFunction()
        {
        }
    }
}