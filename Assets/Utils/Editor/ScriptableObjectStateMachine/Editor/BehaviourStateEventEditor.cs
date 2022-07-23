#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace App.Core.Tools.StateMachine.Editor
{
    [CustomEditor(typeof(WorkflowBehaviourStateEvent))]
    public class BehaviourStateEventEditor : UnityEditor.Editor
    {
        private Object property;
        
        public override void OnInspectorGUI()
        {
            GUI.enabled = Application.isPlaying;
            WorkflowBehaviourStateEvent e = target as WorkflowBehaviourStateEvent;
            property = EditorGUILayout.ObjectField("Data", property, typeof(WorkflowStateAndParameters), false);
            if (GUILayout.Button("Raise"))
            {
                if (e != null)
                {
                    e.Raise(property as WorkflowStateAndParameters);
                }
            }
        }
    }
}
#endif