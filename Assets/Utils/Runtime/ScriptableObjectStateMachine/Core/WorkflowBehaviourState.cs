using UnityEngine;
#pragma warning disable

namespace App.Core.Tools.StateMachine
{
    /// <summary>
    /// Animator state machine state behaviour.
    /// Sends events on Enter, Update, Exit.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class WorkflowBehaviourState : StateMachineBehaviour
    {
        //---------------------------------------------------------------------
        // Editor
        //---------------------------------------------------------------------
        
        [Header("Data")] 
        [SerializeField] private WorkflowStateAndParameters data;
        
        [Header("Events")]
        [SerializeField] private WorkflowBehaviourStateEvent onStateEnter;
        [SerializeField] private WorkflowBehaviourStateEvent onStateExit;
        // [SerializeField] private BehaviourStateEvent onStateUpdate;
        
        
        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------
        // public StateAndParameters Data => data;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (onStateEnter != null)
            {
                onStateEnter.Raise(data);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (onStateExit != null)
            {
                onStateExit.Raise(data);
            }
        }

        // public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        // {
        //     if (onStateUpdate != null)
        //     {
        //         onStateUpdate.Raise(data);
        //     }
        // }

        // public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        // {
        // }
        //
        // public override void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        // {
        // }
    }
}