using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace App.Reskill.MRTKExtensions
{
    public static class GlobalThemeEngineConfiguration
    {
        [Serializable]
        public class BaseAndItsValue
        {
            public string typeName;
            public ThemeDefinition currentTheme;

            public static BaseAndItsValue CreateDefault<T>() 
                where T : InteractableThemeBase
            {
                var newOne = new BaseAndItsValue();

                newOne.typeName = typeof(T).Name;
                newOne.currentTheme = ThemeDefinition.GetDefaultThemeDefinition<T>().Value;
                return newOne;
            }
        }

        public static event Action OnUpdateThemes;
        
        /// <summary>
        ///  List of themes see from the inheritors of the InteractableThemeBase
        /// </summary>
        private static List<BaseAndItsValue> _currentApplicableThemes = new List<BaseAndItsValue>();
        

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InitializeOnSceneLoad()
        {
            _currentApplicableThemes.Clear();
            CheckThemes();
            SceneManager.sceneLoaded -= OnNewSceneLoaded;
            SceneManager.sceneLoaded += OnNewSceneLoaded;
            OnUpdateThemes?.Invoke();
        }

        private static void OnNewSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            CheckThemes();
        }

        private static void CheckThemes()
        {
            CheckEmptyThemes();
        }

        private static void CheckEmptyThemes()
        {
            if (_currentApplicableThemes.Count == 0)
            {
                var actualThemesFromResources = LoadCurrentThemesFromResources();
                // Apply only those themes - otherwise the  custom offsets/parameters will be lost
                if (actualThemesFromResources == null || actualThemesFromResources.Count == 0)
                {
                    // currentInteractableThemes.Add(BaseAndItsValue.CreateDefault<InteractableActivateTheme>());
                    // currentInteractableThemes.Add(BaseAndItsValue.CreateDefault<InteractableAnimatorTheme>());
                    // currentInteractableThemes.Add(BaseAndItsValue.CreateDefault<InteractableAudioTheme>());
                    _currentApplicableThemes.Add(BaseAndItsValue.CreateDefault<InteractableColorTheme>());
                    _currentApplicableThemes.Add(BaseAndItsValue.CreateDefault<InteractableMaterialTheme>());
                    // currentInteractableThemes.Add(BaseAndItsValue.CreateDefault<InteractableOffsetTheme>());
                    _currentApplicableThemes.Add(BaseAndItsValue.CreateDefault<InteractableRotationTheme>());
                    // currentInteractableThemes.Add(BaseAndItsValue.CreateDefault<InteractableScaleTheme>());
                    // currentInteractableThemes.Add(BaseAndItsValue.CreateDefault<InteractableShaderTheme>());
                    // currentInteractableThemes.Add(BaseAndItsValue.CreateDefault<InteractableStringTheme>());
                    _currentApplicableThemes.Add(BaseAndItsValue.CreateDefault<InteractableTextureTheme>());
                    // currentInteractableThemes.Add(BaseAndItsValue.CreateDefault<InteractableGrabScaleTheme>());
                    // currentInteractableThemes.Add(BaseAndItsValue.CreateDefault<ScaleOffsetColorTheme>());
                    _currentApplicableThemes.Add(BaseAndItsValue.CreateDefault<InteractableColorChildrenTheme>());
                }
            }
        }

        private static List<BaseAndItsValue> LoadCurrentThemesFromResources()
        {
            // TODO: Create Resource to load 
            
            return null;
        }
        
        public static void ChangeTheme<T>( ThemeDefinition changeProperties ) 
            where T : InteractableThemeBase
        {
            var typeToSeek = typeof(T).Name;
            var currentTheme = _currentApplicableThemes
                .Find((obj)=> obj.typeName.Equals(typeToSeek));
            
            if (currentTheme == null)
            {
                currentTheme = BaseAndItsValue.CreateDefault<T>();
                _currentApplicableThemes.Add(currentTheme);
            }

            currentTheme.currentTheme = changeProperties;
            OnUpdateThemes?.Invoke();
        }

        public static void CheckThemeOn(Interactable toCHeck)
        {
            // TODO: find themes to change
            
            // As Last step - refresh to update theme on the object
            toCHeck.RefreshSetup();
        }
        
        public static void CheckThemeOn(UnityEngine.UI.Button toCHeck)
        {
            // TODO: change theme on UI Button

            
        }

        public static void CheckThemeOn(UnityEngine.UI.InputField toCHeck)
        {
            // TODO: change theme on UI InputField
            
        }
        
        public static void CheckThemeOn(TMPro.TMP_InputField toCHeck)
        {
            // TODO: change theme on Text Mesh Pro Input Field
            
        }
    }
}