using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Core.Tools.Async.Hidden
{
	[ExecuteAlways]
    public class ThreadToolsHelper : App.Core.Tools.SingletonPersistent<ThreadToolsHelper>
    {
        private readonly object lockObject = new object();
        private readonly List<Action> actions = new List<Action>();
        
// #if UNITY_EDITOR
//         private void OnEnable()
//         {
// 	        if(Application.isPlaying) return;
//
// 	        UnityEditor.EditorApplication.update += Update;
//         }
//
//         private void OnDisable()
//         {
// 	        if(Application.isPlaying) return;
//
// 	        UnityEditor.EditorApplication.update += Update;
//         }
// #endif

        public void Add(Action action)
        {
            lock (lockObject)
            {
                actions.Add(action);
            }
        }
        
        public void Remove(Action action)
        {
	        lock (lockObject)
	        {
		        actions.Remove(action);
	        }
        }

        private void Update()
        {
            lock (lockObject)
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    if (actions[i] != null)
                    {
                        actions[i]();
                    }
                }
                actions.Clear();
            }
        }
    }
}