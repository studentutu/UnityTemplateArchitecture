using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Object = UnityEngine.Object;

namespace App.Core.Tools.StateMachine
{
    [CustomEditor(typeof(WorkflowStateAndParameters), true)]
    [CanEditMultipleObjects]
    public class EditorForIState : 
#if ODIN_INSPECTOR
    OdinEditor
#else
    UnityEditor.Editor
#endif
    {
        private Object[] myHolders;
        private Object property;
        private bool copyFrom;

#if ODIN_INSPECTOR
		protected override void OnEnable()
        {
	        base.OnEnable();
#else
	    protected void OnEnable()
	    {
#endif
		    if (targets != null)
            {
                myHolders = targets ;
            }
            else
            {
                myHolders = new[] { target };
            }
        }

        public override void OnInspectorGUI()
        {
	        
	        copyFrom = EditorGUILayout.Toggle("Editor Copy From Animator ?", copyFrom);
	        if (copyFrom)
	        {
	            EditorGUI.indentLevel += 1;
	            property = EditorGUILayout.ObjectField("Animator to copy from", property, typeof(AnimatorController), true);

	            if (GUILayout.Button(" Copy All Parameters from the Animator "))
	            {
	                if (targets != null)
	                {
	                    myHolders = targets;
	                }
	                else
	                {
	                    myHolders = new[] { target };
	                }
	                IStateEditoHelper asInterface;
	                SerializedObject fromItems;
	                foreach (var item in myHolders)
	                {
	                    asInterface = item as IStateEditoHelper;
	                    fromItems = new SerializedObject(item);
	                    asInterface.CopyParamsFrom(property as AnimatorController);

	                    // To Save scriptable object we need to use set dirty and then save Assests
	                    fromItems.ApplyModifiedProperties();
	                    PrefabUtility.RecordPrefabInstancePropertyModifications(item);
	                    EditorUtility.SetDirty(item);
	                    fromItems.Update();
	                }
	                AssetDatabase.SaveAssets();
	                serializedObject.ApplyModifiedProperties(); // needs to be here so that when you manualy change somethiing, it will be displayed and saved
	                serializedObject.Update();
	                return;
	            }
	            EditorGUI.indentLevel -= 1;
	        }
	        // DrawPropertiesExcluding(serializedObject, new string[] { }); // m_Script
	        // serializedObject.ApplyModifiedProperties(); // needs to be here so that when you manualy change somethiing, it will be displayed and saved
	        base.OnInspectorGUI();
        }
    }
}