using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace App.Core.Attributes.Editor
{
    [CustomPropertyDrawer(typeof(DrawIfAttribute))]
    public class DrawIfPropertyDrawer : PropertyDrawer
    {
        private DrawIfAttribute drawIf;
        private SerializedProperty comparedField;
        private float propertyHeight;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return propertyHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            drawIf = attribute as DrawIfAttribute;
            string[] nested = drawIf.ComparedPropertyName.Split('/');
            string mainName = drawIf.ComparedPropertyName;
            SerializedProperty temporal = property;
            for (int i = 0; i < nested.Length; i++)
            {
				mainName = nested[i];
				temporal = GetTargetPropertyFromGivenChild(temporal, mainName);
            }
            comparedField = temporal;
            if (comparedField == null)
            {
	            Debug.LogWarning("Not found compared field : " + drawIf.ComparedPropertyName);
				return;
            }

            object comparedFieldValue = EditorUtils.GetTargetObjectOfProperty(comparedField);
            double comparedFieldNumber = 0;
            bool isConditionMet = false;

            // todo should be better way to check if it's numeric, this can fail later with equality of integers etc.
            if (!double.TryParse(Convert.ToString(drawIf.ComparedValue, CultureInfo.InvariantCulture), NumberStyles.Any, NumberFormatInfo.InvariantInfo, out double comparedNumber) ||
                !double.TryParse(Convert.ToString(comparedFieldValue, CultureInfo.InvariantCulture), NumberStyles.Any, NumberFormatInfo.InvariantInfo, out comparedFieldNumber))
            {
                if (drawIf.ComparisonType != ComparisonType.Equals && drawIf.ComparisonType != ComparisonType.NotEquals)
                {
                    Debug.LogError("The only comparison types available to type '" + comparedFieldValue.GetType() + "' are Equals and NotEqual. (On object '" + property.serializedObject.targetObject.name + "')");
                    return;
                }
            }

            if (comparedFieldValue == null)
            {
	            Debug.LogWarning("Not found compared field value");
	            return;
            }
            
            switch (drawIf.ComparisonType)
            {
                case ComparisonType.Equals:
                    if (comparedFieldValue.Equals(drawIf.ComparedValue))
                    {
                        isConditionMet = true;
                    }
                    break;
                case ComparisonType.NotEquals:
                    if (!comparedFieldValue.Equals(drawIf.ComparedValue))
                    {
                        isConditionMet = true;
                    }
                    break;
                case ComparisonType.GreaterOrEquals:
                    if (comparedFieldNumber >= comparedNumber)
                    {
                        isConditionMet = true;
                    }
                    break;
                case ComparisonType.SmallerOrEquals:
                    if (comparedFieldNumber <= comparedNumber)
                    {
                        isConditionMet = true;
                    }
                    break;
                case ComparisonType.GreaterThan:
                    if (comparedFieldNumber > comparedNumber)
                    {
                        isConditionMet = true;
                    }
                    break;
                case ComparisonType.SmallerThan:
                    if (comparedFieldNumber < comparedNumber)
                    {
                        isConditionMet = true;
                    }
                    break;
            }

            propertyHeight = base.GetPropertyHeight(property, label);
            if (isConditionMet)
            {
                EditorGUI.PropertyField(position, property);
            }
            else
            {
                if (drawIf.DisablingType == DisablingType.ReadOnly)
                {
                    GUI.enabled = false;
                    EditorGUI.PropertyField(position, property);
                    GUI.enabled = true;
                }
                else
                {
                    propertyHeight = 0f;
                }
            }
        }

        private SerializedProperty GetTargetPropertyFromGivenChild(SerializedProperty fromProperty, string name)
        {
	        SerializedProperty result = null;
	        result = fromProperty.serializedObject.FindProperty(name);
	        if (result == null)
	        {
		        result = fromProperty.FindPropertyRelative(name);
		        if (result == null)
		        {
			        // Check if we are inside the object or in the array
			        var getPropAtPath = fromProperty.propertyPath.Substring(0,
				        fromProperty.propertyPath.Length - fromProperty.name.Length);
			        result = fromProperty.serializedObject.FindProperty(getPropAtPath + name);
			        if (result != null)
			        {
				        return result;
			        }

			        if (string.IsNullOrEmpty(getPropAtPath))
			        {
				        return null;
			        }

			        getPropAtPath = getPropAtPath.Substring(0, getPropAtPath.Length - 1);
			        // check if is inside the object
			        // get parentProperty
			        result = fromProperty.serializedObject.FindProperty(getPropAtPath);
			        if (result != null)
			        {
				        // try get target property
				        result = GetTargetPropertyFromGivenChild(result, name);
				        if (result != null)
				        {
					        return result;
				        }
			        }

			        // check if is inside the array/list
			        int lastIndexOf = getPropAtPath.LastIndexOf('[');
			        if (lastIndexOf < 0)
			        {
				        return null;
			        }

			        // get parentProperty
			        var sitInsideArray = getPropAtPath.Substring(0, lastIndexOf - 1);
			        result = fromProperty.serializedObject.FindProperty(sitInsideArray);
			        if (result == null)
			        {
				        return null;
			        }

			        // try get target property
			        result = GetTargetPropertyFromGivenChild(result, name);
		        }
	        }

	        return result;
        }
    }
}