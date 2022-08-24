using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
// using Microsoft.MixedReality.Toolkit;
// using Microsoft.MixedReality.Toolkit.Input;
// using UnityEngine.EventSystems;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Reskill.MRTKExtensions
{
    /// <summary>
    /// Custom Component for global theme management
    /// </summary>
    public class GlobalThemeEngineListener : MonoBehaviour
        // , IMixedRealitySpeechHandler
    {
        [Flags]
        private enum SubscribeOn
        {
            None = 0,
            Interactable = 1,
            Button = 2,
            UnityInputField = 4,
            TMPInputField = 8,
        }

        [Header("Component will listen to the Global theme config")]
        [SerializeField] private Interactable currentInteractable;
        [SerializeField] private Button currentButton;
        [SerializeField] private InputField currentUnityInputField;
        [SerializeField] private TMP_InputField tmpInputField;
        
        [NonSerialized] private SubscribeOn _subscribeOn;
        [NonSerialized] private bool _isInFocus;
        
        private void OnEnable()
        {
            CheckSubscribeType();
            GlobalThemeEngineConfiguration.OnUpdateThemes += CheckCurrentTheme;
            CheckCurrentTheme();
        }

        private void CheckSubscribeType()
        {
            _subscribeOn = SubscribeOn.None;
            if (currentInteractable != null)
            {
                _subscribeOn |= SubscribeOn.Interactable;
            }
            
           
        }
        
        private void CheckCurrentTheme()
        {
            if (_subscribeOn.HasFlag(SubscribeOn.Interactable))
            {
                GlobalThemeEngineConfiguration.CheckThemeOn(currentInteractable);
            }
            
            if (_subscribeOn.HasFlag(SubscribeOn.Button))
            {
                GlobalThemeEngineConfiguration.CheckThemeOn(currentButton);
            }
            
            if (_subscribeOn.HasFlag(SubscribeOn.UnityInputField))
            {
                GlobalThemeEngineConfiguration.CheckThemeOn(currentUnityInputField);
            }
            
            if (_subscribeOn.HasFlag(SubscribeOn.TMPInputField))
            {
                GlobalThemeEngineConfiguration.CheckThemeOn(tmpInputField);
            }
        }

        private void OnDisable()
        {
            GlobalThemeEngineConfiguration.OnUpdateThemes -= CheckCurrentTheme;
            // RegisterHandler<IMixedRealitySpeechHandler>(false);
        }

        
        [UnityEngine.Scripting.Preserve]
        public void SetFocus(bool focusOn)
        {
            _isInFocus = focusOn;
        }
        
        [UnityEngine.Scripting.Preserve]
        public void SubmitIfInFocus()
        {
            if (_isInFocus)
            {
                Debug.LogWarning("Will be CLicked ");
#pragma warning disable
                WaitForMainThread();
#pragma warning restore
            }
        }

        private async Task WaitForMainThread()
        {
            await UniTask.SwitchToMainThread();
            // From Far only
            currentInteractable.TriggerOnClick(true);
        }


        // private void RegisterHandler<T>(bool enable) where T : IEventSystemHandler
        // {
        //     if (enable)
        //     {
        //         CoreServices.InputSystem?.RegisterHandler<T>(this);
        //     }
        //     else
        //     {
        //         CoreServices.InputSystem?.UnregisterHandler<T>(this);
        //     }
        // }
        // public void OnSpeechKeywordRecognized(SpeechEventData eventData)
        // {
        //     var voiceCommand = currentInteractable.VoiceCommand.ToLower();
        //     var trimmed = eventData.Command.Keyword.ToLower().Replace(" ", "");
        //     // Never Comes here!
        //     Debug.LogWarning(" Recognized - " + voiceCommand);
        //     Debug.LogWarning(" Needed - " + trimmed);
        //     
        //     if (_isInFocus)
        //     {
        //         if (trimmed.Equals(voiceCommand))
        //         {
        //             Submit();
        //         }
        //     }
        // }
    }
}