using System;
using System.Collections.Generic;
using App.Core.Tools.StateMachine.Data;
using UnityEngine;

namespace App.Core.Tools.StateMachine
{
    /// <summary>
    /// State machine class
    /// </summary>
    public class WorkflowStateControllerView : MonoBehaviour
    {
        //---------------------------------------------------------------------
        // Editor
        //---------------------------------------------------------------------

        [SerializeField] private bool isAllowingTransitionToSelf;
        [SerializeField] private int ignoreFirstNSelfTransitions = 0;
        [SerializeField] protected Animator animatorForStateController = null;
        public Action<WorkflowStateAndParameters> onStateChanged;

        //---------------------------------------------------------------------
        // Properties
        //---------------------------------------------------------------------

        public WorkflowStateAndParameters CurrentWorkflowState { get; private set; }
		
        //---------------------------------------------------------------------
        // Internal
        //---------------------------------------------------------------------

        protected static Dictionary<AnimatorControllerParameterType, Action<Animator, AnimationParamSerialiazable>>
            strategy = new Dictionary<AnimatorControllerParameterType, Action<Animator, AnimationParamSerialiazable>>
            {
                {AnimatorControllerParameterType.Trigger, OnTrigger},
                {AnimatorControllerParameterType.Bool, OnBool},
                {AnimatorControllerParameterType.Float, OnFloat},
                {AnimatorControllerParameterType.Int, OnInt}
            };

        private int? currentNumberOfIgnores = null;

        protected int CurrentNumberOfIgnores
        {
	        get
	        {
		        if (currentNumberOfIgnores == null)
		        {
			        currentNumberOfIgnores = ignoreFirstNSelfTransitions;
		        }

		        return currentNumberOfIgnores.Value;
	        }
	        set
	        {
		        currentNumberOfIgnores = value;
	        }
        }
        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------
		
        [UnityEngine.Scripting.Preserve]
        public virtual void TryChangeState(WorkflowStateAndParameters nextWorkflowState)
        {
	        if (!isAllowingTransitionToSelf && CurrentWorkflowState == nextWorkflowState)
	        {
#if UNITY_EDITOR
		        Debug.LogWarning(name + " is already in : " + nextWorkflowState.name);
#endif
		        return;
	        }
	        
	        // Allow transition to itself, but ignore first n-self-transitions
	        if (CurrentWorkflowState == nextWorkflowState  && CurrentNumberOfIgnores > 0)
	        {
#if UNITY_EDITOR
		        Debug.LogWarning("Ignoring first n self transitions " + CurrentNumberOfIgnores);
#endif
		        CurrentNumberOfIgnores--;
		        return;
	        }
	        
            SetAllParamsFromState(animatorForStateController, nextWorkflowState);
        }

        // Do not effect the animator transitions, only the invocation of Try Change State!
        public void SetAllowTransitionToItself(bool allow)
        {
            isAllowingTransitionToSelf = allow;
        }
        
        public void SetSelfTransitionsIgnoreNFirstTimes(int ignoreFirst)
        {
	        CurrentNumberOfIgnores = ignoreFirst;
        }

        /// <summary>
        /// invoked from Animator through the GameEvent Scriptable Object!
        /// You can use StateEnterActions instead
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public void OnEnterState(WorkflowStateAndParameters data)
        {
            CurrentWorkflowState = data;
            onStateChanged?.Invoke(CurrentWorkflowState);
        }
        
        /// <summary>
        /// invoked from Animator through the GameEvent Scriptable Object!
        /// You can use StateEnterActions instead
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public void OnExitState(WorkflowStateAndParameters data)
        {
	        if (CurrentWorkflowState == data)
	        {
		        CurrentWorkflowState = null;
	        }
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static void OnTrigger(Animator animatorToUSe, AnimationParamSerialiazable parameter)
        {
            if (parameter.triggerValue)
            {
                animatorToUSe.SetTrigger(parameter.nameHash);
            }
            else
            {
                animatorToUSe.ResetTrigger(parameter.nameHash);
            }
        }

        private static void OnBool(Animator animatorToUSe, AnimationParamSerialiazable parameter)
        {
            animatorToUSe.SetBool(parameter.nameHash, parameter.boolValue);
        }

        private static void OnFloat(Animator animatorToUSe, AnimationParamSerialiazable parameter)
        {
            animatorToUSe.SetFloat(parameter.nameHash, parameter.floatValue);
        }

        private static void OnInt(Animator animatorToUSe, AnimationParamSerialiazable parameter)
        {
            animatorToUSe.SetInteger(parameter.nameHash, parameter.intValue);
        }

        private static void SetAllParamsFromState(Animator animatorToUse, IState stateToGetparametersFrom)
        {
            if (animatorToUse != null && animatorToUse.enabled && stateToGetparametersFrom != null)
            {
	            var runtimeData = stateToGetparametersFrom.CurrentParameters;
                for (var i = 0; i < runtimeData.Length; i++)
                {
                    if (runtimeData[i] != null && strategy.ContainsKey(runtimeData[i].type))
                    {
                        strategy[runtimeData[i].type]
                                (animatorToUse, runtimeData[i]);
                    }
                }
            }
        }
    }
}