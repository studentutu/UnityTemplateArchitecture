using System;
using UnityEngine;

namespace Tests
{
    public static class UnityEngineTestExtension
    {
        [ExecuteAlways]
        private class TestMono : MonoBehaviour
        {
            public void OnDisable()
            {
                Debug.Assert(true);
            }

            public void OnDestroy()
            {
                Debug.Assert(true);
            }
        }
        
        /// <summary>
        /// Make Sure that the target test classes have [ExecuteInEditMode]
        /// </summary>
        /// <param name="any"></param>
        public static void DestroyImmediatelyWithCallbacks( this MonoBehaviour any)
        {
#if UNITY_EDITOR
            if (any != null)
            {
                DestroyImmediatelyWithCallbacks(any.gameObject);
            }
#endif
        }
        
        /// <summary>
        /// Make Sure that the target test classes have [ExecuteInEditMode]
        /// </summary>
        public static void DestroyImmediatelyWithCallbacks( this GameObject any)
        {
#if UNITY_EDITOR
            if (any != null)
            {
                any.BroadcastMessage(nameof(TestMono.OnDisable),SendMessageOptions.DontRequireReceiver);
                any.BroadcastMessage(nameof(TestMono.OnDestroy),SendMessageOptions.DontRequireReceiver);

                GameObject.DestroyImmediate(any);
            }
#endif
        }
        
        public static T NewGameObjectFactoryOfObject<T>() where T: MonoBehaviour
        {
            var newGameObject = new GameObject(nameof(T));
            var currentResult = newGameObject.AddComponent<T>();
            return currentResult;
        }
    }
}