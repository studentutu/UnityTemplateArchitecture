using System.Collections.Generic;
using App.Core.Tools.StateMachine.Data;
using UnityEngine;
using UnityEditor;

namespace App.Core.Tools.StateMachine
{

    [CustomPropertyDrawer(typeof(AnimationParamSerialiazable), false)]
    public class EditorForStateParameters : PropertyDrawer
    {
        private Dictionary<string, bool> allMyPropsEnabled = new Dictionary<string, bool>();
        
        private void OnEnable()
        {
            allMyPropsEnabled.Clear();
        }

        public override void OnGUI(Rect position, SerializedProperty serCallBaseProperty, GUIContent label)
        {
            bool isEnabled = false;
            if (!allMyPropsEnabled.TryGetValue(serCallBaseProperty.propertyPath, out isEnabled))
            {
                allMyPropsEnabled.Add(serCallBaseProperty.propertyPath, isEnabled);
            }

            // Indent label
            // label.text = "      " + label.text;
            Rect onlyToggle2 = new Rect(position.x, position.y + 2, position.width, position.height);
            // Get keyName
            SerializedProperty targetProp = serCallBaseProperty.FindPropertyRelative(nameof(AnimationParamSerialiazable.keyName));
            string nameKey = targetProp.stringValue;

            GUI.Box(onlyToggle2, "", "toolbarDropDown"); // see https://gist.github.com/MadLittleMods/ea3e7076f0f59a702ecb
            position.y += 4;
            position.x += 4;
            Rect onlyToggle = new Rect(position.x, position.y, 100, EditorGUIUtility.singleLineHeight);
            isEnabled = GUI.Toggle(onlyToggle, isEnabled, nameKey);
            
            allMyPropsEnabled[serCallBaseProperty.propertyPath] = isEnabled;
            if (isEnabled)
            {
                EditorGUI.BeginProperty(position, label, serCallBaseProperty);
                EditorGUI.BeginChangeCheck();
                // Using BeginProperty / EndProperty on the parent property means that
                // prefab override logic works on the entire property.
                position.y += 4 + EditorGUIUtility.singleLineHeight;

                // Draw label
                // Rect pos = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
                Rect targetRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);


                // Get nameHash
                SerializedProperty NameHashProp = serCallBaseProperty.FindPropertyRelative(nameof(AnimationParamSerialiazable.nameHash));


                EditorGUI.PropertyField(targetRect, targetProp); // keyName
                targetRect = new Rect(position.x, targetRect.max.y + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);

                EditorGUI.PropertyField(targetRect, NameHashProp); // nameHash
                targetRect = new Rect(position.x, targetRect.max.y + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);

                SerializedProperty typeEnum = serCallBaseProperty.FindPropertyRelative(nameof(AnimationParamSerialiazable.type));
                int currentValue = typeEnum.enumValueIndex;

                AnimatorControllerParameterType typeEnumAsAnimatorEnum;
                switch (currentValue)
                {
                    case 0:
                        typeEnumAsAnimatorEnum = AnimatorControllerParameterType.Float;
                        break;
                    case 1:
                        typeEnumAsAnimatorEnum = AnimatorControllerParameterType.Int;
                        break;
                    case 2:
                        typeEnumAsAnimatorEnum = AnimatorControllerParameterType.Bool;
                        break;
                    default:
                        typeEnumAsAnimatorEnum = AnimatorControllerParameterType.Trigger;
                        break;
                }
                EditorGUI.PropertyField(targetRect, typeEnum); // type

                targetRect = new Rect(position.x, targetRect.max.y + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
                SerializedProperty customPropertyFiedlFromEnum = null;
                switch (typeEnumAsAnimatorEnum)
                {
                    case AnimatorControllerParameterType.Bool:
                        customPropertyFiedlFromEnum = serCallBaseProperty.FindPropertyRelative(nameof(AnimationParamSerialiazable.boolValue));
                        break;
                    case AnimatorControllerParameterType.Float:
                        customPropertyFiedlFromEnum = serCallBaseProperty.FindPropertyRelative(nameof(AnimationParamSerialiazable.floatValue));
                        break;
                    case AnimatorControllerParameterType.Int:
                        customPropertyFiedlFromEnum = serCallBaseProperty.FindPropertyRelative(nameof(AnimationParamSerialiazable.intValue));
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        customPropertyFiedlFromEnum = serCallBaseProperty.FindPropertyRelative(nameof(AnimationParamSerialiazable.triggerValue));
                        break;
                }
                if (customPropertyFiedlFromEnum != null)
                {
                    EditorGUI.PropertyField(targetRect, customPropertyFiedlFromEnum);
                }
                else
                {
                    GUIContent methodlabel = new GUIContent("Set Type for (" + label.text + ")");
                    // Rect methodRect = new Rect(position.x, targetRect.max.y + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
                    // Method select button
                    EditorGUI.PrefixLabel(targetRect, GUIUtility.GetControlID(FocusType.Passive), methodlabel);

                }
                if (!string.IsNullOrEmpty(nameKey))
                {
                    if (NameHashProp.intValue == default(int) || NameHashProp.intValue != Animator.StringToHash(nameKey))
                    {
                        NameHashProp.intValue = Animator.StringToHash(nameKey);
                        serCallBaseProperty.serializedObject.ApplyModifiedProperties();
                        serCallBaseProperty.serializedObject.Update();
                        // return;
                    }
                }
                if (EditorGUI.EndChangeCheck())
                {
                    serCallBaseProperty.serializedObject.ApplyModifiedProperties();
                    serCallBaseProperty.serializedObject.Update();
                }
                // Set indent back to what it was
                EditorGUI.EndProperty();
            }
        }

        // Unity Layout!
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineheight = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
            float height = lineheight * 4 + EditorGUIUtility.singleLineHeight * 1.5f;
            // height += 8;
            bool isEnabled = false;
            if (!allMyPropsEnabled.TryGetValue(property.propertyPath, out isEnabled))
            {
                allMyPropsEnabled.Add(property.propertyPath, isEnabled);
            }
            if (!isEnabled)
            {
                height = EditorGUIUtility.singleLineHeight + 8;
            }
            return height;
        }
    }
}