using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace App.Reskill.MRTKExtensions
{
    public class CopyFromNonStandardKeyboard : MonoBehaviour
    {
        [SerializeField] private NonNativeKeyboard keyboard;
        [SerializeField] private TMP_InputField inputFieldTarget;
        [SerializeField] private UnityEvent OnCloseKeyboard = new UnityEvent();

        [UnityEngine.Scripting.Preserve]
        public void EnableKeyboard()
        {
            DisableKeyboard();
            
            keyboard.PresentKeyboard(inputFieldTarget.text);

            keyboard.OnClosed += DisableKeyboard;
            keyboard.OnTextSubmitted += DisableKeyboard;
            keyboard.OnTextUpdated += UpdateText;
        }

        private void UpdateText(string text)
        {
            inputFieldTarget.text = text;
        }

        [UnityEngine.Scripting.Preserve]
        public void DisableKeyboard()
        {
            keyboard.OnTextUpdated -= UpdateText;
            keyboard.OnClosed -= DisableKeyboard;
            keyboard.OnTextSubmitted -= DisableKeyboard;

            keyboard.Close();
            OnCloseKeyboard.Invoke();
        }

        private void DisableKeyboard(object sender, EventArgs e)
        {
            DisableKeyboard();
        }
    }
}