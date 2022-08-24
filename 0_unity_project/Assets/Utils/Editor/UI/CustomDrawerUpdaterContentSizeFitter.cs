using System.Collections;
using System.Collections.Generic;
using App.Core.Tools;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomPropertyDrawer(typeof(UpdateContentSizeFitter))]
public class CustomDrawerUpdaterContentSizeFitter : UnityEditor.Editor
{
    private Object propertyContentSizeFitter;
    private Object propertyRectTransform;

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        UpdateContentSizeFitter e = target as UpdateContentSizeFitter;
        var asEditorInterface = (UpdateContentSizeFitter.IEditorFields)e;
        propertyContentSizeFitter = EditorGUILayout
            .ObjectField(asEditorInterface.GetNameOfForContentSizeFitter(), propertyContentSizeFitter, typeof(ContentSizeFitter), true);
        propertyRectTransform = EditorGUILayout
            .ObjectField(asEditorInterface.GetNameOfForRectTransform(), propertyRectTransform, typeof(RectTransform), true);

        if (propertyContentSizeFitter != null)
        {
            var propertySizeFitter = serializedObject.FindProperty(asEditorInterface.GetNameOfForContentSizeFitter());
            propertySizeFitter.objectReferenceValue = propertyContentSizeFitter;
            var propertySizeFitterRect = serializedObject.FindProperty(asEditorInterface.GetNameOfForRectTransform());
            propertySizeFitterRect.objectReferenceValue = propertyRectTransform;
        }

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
}