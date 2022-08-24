using System;
using System.Collections.Generic;
using TestPlatforms.Cloud;
using Unity.AutomatedQA;
using Unity.AutomatedQA.Editor;
using Unity.CloudTesting.Editor;
using Unity.RecordedPlayback.Editor;
using Unity.RecordedPlayback;
using UnityEditor;
using UnityEngine;
using UnityEngine.iOS;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Unity.AutomatedQA.Editor
{
    /// <summary>
    /// Automated QA Editor Window for other miscellaneous tools.
    /// </summary>
    public class ToolsSubWindow : HubSubWindow
    {
        private static readonly string WINDOW_FILE_NAME = "tools-window";
        private static string resourcePath = "Packages/com.unity.automated-testing/Editor/ToolsWindow/";
        
        
        private ScrollView root;
        private VisualElement baseRoot;

   

        public override void Init()
        {
        }

        public override void SetUpView(ref VisualElement br)
        {
            br.Clear();
            root = new ScrollView();
            baseRoot = br;
            baseRoot.Add(root);

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(resourcePath + $"{WINDOW_FILE_NAME}.uxml");
            visualTree.CloneTree(baseRoot);

            baseRoot.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(resourcePath + $"{WINDOW_FILE_NAME}.uss"));
            baseRoot.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(HubWindow.HUB_RESOURCES_PATH + $"{HubWindow.ALL_WINDOWS_USS_FILE_NAME}"));

            root.Add(HubWindow.Instance.AddHubBackButton());
            root.Add(new IMGUIContainer(() =>
            {
                UpdateIMGUI();
            }));

        }

        private void UpdateIMGUI()
        {
            GUILayout.Label("Tools");
            
            if (GUILayout.Button("Create Automated Run"))
            {
                AutomatedRunEditor.CreateAsset();
            }
            
            if (GUILayout.Button("Add Game Elements To Scene Objects"))
            {
                GameElementValidator.AddGameElementsToSceneObjects();
            }
        }

        public override void OnGUI()
        {
           
        }

    }
}