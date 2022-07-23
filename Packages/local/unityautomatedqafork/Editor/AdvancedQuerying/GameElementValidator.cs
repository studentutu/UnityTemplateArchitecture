using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AutomatedQA;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unity.AutomatedQA
{
    [UnityEditor.InitializeOnLoad]
    internal static class GameElementValidator
    {
        static GameElementValidator()
        {
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            var elements = Object.FindObjectsOfType<GameElement>();
            foreach (var gameElement in elements)
            {
                if (string.IsNullOrEmpty(gameElement.AutoId))
                {
                    gameElement.ResetAutoID();
                }
            }
        }

        /// <summary>
        /// Automatically add GameElements to all interactable UI Elements in the scene
        /// </summary>
        //[MenuItem("Automated QA/Add Game Elements To Scene Objects")]
        public static void AddGameElementsToSceneObjects()
        {
            if (!ValidateAddGameElementsToSceneObjects())
            {
                return;
            }

            int added = 0;
            
            var objs = Object.FindObjectsOfType<UIBehaviour>();
            foreach (var obj in objs)
            {
                if (obj.gameObject.GetComponent<GameElement>() == null)
                {
                    obj.gameObject.AddComponent<GameElement>();
                    added++;
                }
            }

            var ges = Object.FindObjectsOfType<GameElement>();
            new AQALogger().Log($"Added {added} new GameElements to scene objects. Total: {ges.Count()}.");
        }
        
        /// <summary>
        /// Validate that the editor is not in play mode
        /// </summary>
        [MenuItem("Automated QA/Add Game Elements To Scene Objects", true)]
        public static bool ValidateAddGameElementsToSceneObjects()
        {
            return !Application.isPlaying;
        }
        
    }
}

