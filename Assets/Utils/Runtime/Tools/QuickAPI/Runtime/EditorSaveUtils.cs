using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace App.Core.Tools
{
    public static class EditorSaveUtils
    {
#if UNITY_EDITOR
        public static void SaveObjectInEditor<T>(T anyObject)
        where T : UnityEngine.Component
        {
            var serObject = new UnityEditor.SerializedObject(anyObject);
            serObject.ApplyModifiedProperties();
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(anyObject);
        }

        public static void SaveEditorScene<T>(T anyObject)
            where T : UnityEngine.Component
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(anyObject.gameObject.scene);
        }
#endif
    }
}