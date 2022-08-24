using System;
using UnityEngine;
using UnityEngine.Events;
#pragma warning disable

namespace App.Core.Tools.StateMachine
{
    public class WorkflowBehaviourStateEventListener : MonoBehaviour
    {
        [Tooltip("Animator Binder. You can use multiple BehaviourStateEventListener's to listen to " +
                 "Enter/Updates/Exits of Binders on other Animators")]
        [SerializeField] private WorkflowBehaviourStateEvent animatorBinder;
        [SerializeField] private ResponseState response = new ResponseState();
        
        //---------------------------------------------------------------------
        // Messages
        //---------------------------------------------------------------------

        private void OnEnable()
        {
            OnEnableLogic();
        }

        private void OnDisable()
        {
            OnDisableLogic();
        }
        
        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public virtual void Response(WorkflowStateAndParameters data)
        {
#if UNITY_EDITOR
	        Debug.Log(animatorBinder.name + " with State " + data);
#endif
            response.Invoke(data);
        }
        
        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        protected virtual void OnEnableLogic()
        {
            if (animatorBinder != null)
            {
                animatorBinder.Register(this);
            }
        }

        protected virtual void OnDisableLogic()
        {
            if (animatorBinder != null)
            {
                animatorBinder.UnRegister(this);
            }
        }
        
        //---------------------------------------------------------------------
        // Nested
        //---------------------------------------------------------------------

        [Serializable]
        public class ResponseState : UnityEvent<WorkflowStateAndParameters> { }
        
    }
}