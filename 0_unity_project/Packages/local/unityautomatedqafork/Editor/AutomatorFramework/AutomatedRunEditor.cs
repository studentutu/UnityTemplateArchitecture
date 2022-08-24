using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.AutomatedQA;
using System.Reflection;
using Unity.RecordedPlayback;
using UnityEngine.EventSystems;

namespace Unity.AutomatedQA.Editor
{
    /// <summary>
    /// Custom Editor for AutomatedRun assets
    /// </summary>
    [CustomEditor(typeof(AutomatedRun))]
    public class AutomatedRunEditor : UnityEditor.Editor
    {
        private bool IsRunning = false;
        
        /// <summary>
        /// Create a new Automated Run asset in the project
        /// </summary>
        public static void CreateAsset()
        {
            AutomatedRun asset = ScriptableObject.CreateInstance<AutomatedRun>();

            string assetName = "AutomatedRun.asset";
            int i = 0;
            while (File.Exists(Path.Combine(Application.dataPath, assetName)))
            {
                i++;
                assetName = $"AutomatedRun {i}.asset";
            }
            
            AssetDatabase.CreateAsset(asset, "Assets/"+ assetName);
            AssetDatabase.SaveAssets();

            RecordedPlaybackAnalytics.SendAutomatedRunCreation();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
        }
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var run = target as AutomatedRun;

            if (Application.isPlaying && !RecordingInputModule.isWorkInProgress && IsRunning)
                IsRunning = false;

            EditorGUI.BeginDisabledGroup(IsRunning);
            if (GUILayout.Button("Run"))
            {
                if (Application.isPlaying)
                {
                    CentralAutomationController.Instance.Reset();
                    StartAutomatedQAFromEditor.runWhileEditorIsAlreadyStarted = true;
                }
                ReportingManager.IsPlaybackStartedFromEditorWindow = IsRunning = true;
                StartAutomatedQAFromEditor.StartAutomatedRun(run);
            }
            EditorGUI.EndDisabledGroup();
        }
    }

    [CustomPropertyDrawer(typeof(AutomatorConfig))]
    public class AutomatorConfigDrawer : PropertyDrawer
    {
        public class TypeSelection
        {
            public Type type;
            public string displayName;

            public TypeSelection(Type t)
            {
                type = t;
                if (type != null)
                {
                    displayName = string.IsNullOrEmpty(t.Namespace) ? $"{t.Assembly.GetName().Name}/{t.Name}" : $"{t.Assembly.GetName().Name}/{t.Namespace}/{t.Name}" ;
                }
                else
                {
                    displayName = "<None>";
                }
            }

            public string GetShortName()
            {
                return ToShortName(displayName);
            }
            
            public static string ToShortName(string fullDisplayName)
            {
                return fullDisplayName.Split('.', ' ', '/').Last();
            }
        }
        
        private static TypeSelection[] _types = null;
        public static TypeSelection[] types
        {
            get
            {
                if (_types == null)
                {
                    List<Type> types = new List<Type>();
                    foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        types.AddRange(assembly.GetTypes());
                    }
                    var typesEnumer = types.FindAll(type => type.IsSubclassOf(typeof(AutomatorConfig)) && !type.IsAbstract);

                    var results = new List<TypeSelection>();
                    results.Add(new TypeSelection(null));
                    foreach (var t in typesEnumer)
                    {
                        results.Add(new TypeSelection(t));
                    }

                    _types = results.ToArray();
                }
               

                return _types;
            } 
        }

        public static string[] typeDropdownOptions
        {
            get
            {
                return types.Select(x => x.displayName).ToArray();
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int originalIndex = 0;
            for (int i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (t.GetShortName() == TypeSelection.ToShortName(property.managedReferenceFullTypename)) 
                {
                    originalIndex = i;
                    break;
                }
            }
            
            
            var popupPos = new Rect(position);
            popupPos.height = 20;
            var selectedIndex = EditorGUI.Popup(popupPos, TypeSelection.ToShortName(typeDropdownOptions[originalIndex]), originalIndex, typeDropdownOptions);
            
            if (selectedIndex != originalIndex)
            {
                var newType = types[selectedIndex];
                property.managedReferenceValue = newType.type != null ? Activator.CreateInstance(newType.type) : null;
            }

            var propPos = position;
            EditorGUI.PropertyField(propPos, property, GUIContent.none, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
 