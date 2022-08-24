using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Core.Tools.StateMachine
{
    [CreateAssetMenu(menuName = "AnimatorStateMachine/WorkflowAnimatorBinder", fileName = "Binder",order = 1)]
    public class WorkflowBehaviourStateEvent : ScriptableObject
    {
        //---------------------------------------------------------------------
        // Internal
        //---------------------------------------------------------------------
        
        private readonly List<WorkflowBehaviourStateEventListener> listeners = new List<WorkflowBehaviourStateEventListener>();
        
        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public void Register(WorkflowBehaviourStateEventListener l)
        {
            listeners.Add(l);
        }

        public void UnRegister(WorkflowBehaviourStateEventListener l)
        {
            listeners.Remove(l);
        }

        public void Raise(WorkflowStateAndParameters data)
        {
	        if (data == null)
	        {
#if DEBUG
		        Debug.LogWarning("Raising empty Data!");
#endif
		        return;
	        }

	        for (var i = 0; i < listeners.Count; i++)
            {
	            if (listeners[i] != null)
	            {
		            listeners[i].Response(data);
	            }
	            else
	            {
		            Debug.LogError(" listener is null when data " + data.name);
	            }
            }
        }
    }
}