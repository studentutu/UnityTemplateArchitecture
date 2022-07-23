using System;
using System.Collections;
using System.Collections.Generic;
using App.Core.Extensions;
using App.Core.Tools;
using Cysharp.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RigNonStandardKeyboardRef : Singleton<RigNonStandardKeyboardRef>
{
    [Serializable]
    public class UnityEventInt : UnityEvent<int>
    {
    }

    [SerializeField] private NonNativeKeyboard keyboard;

    [SerializeField]
    private List<UnityEventTriggerSubscriber> AllKeyboardButtons = new List<UnityEventTriggerSubscriber>();

    [SerializeField] private UnityEvent OnOpenKeyboard = new UnityEvent();
    [SerializeField] private UnityEvent OnCloseKeyboard = new UnityEvent();
    [SerializeField] private UnityEventInt HaptiicEventForButtonsTargetpointer = new UnityEventInt();

    private TMP_InputField _withFields = null;
    private InputField _fieldInput = null;

    public NonNativeKeyboard Keyboard => keyboard;
    public event System.Action OnCloseKeyBoardEvent;

    public bool IsKeyboardActive()
    {
        return keyboard != null && keyboard.gameObject.activeInHierarchy;
    }

    private void OnEnable()
    {
        foreach (var btn in AllKeyboardButtons)
        {
            if (btn == null)
            {
                continue;
            }

            btn.AddListener(OnEventTrigger);
        }
    }

    private void OnEventTrigger(BaseEventData arg0)
    {
        var asPointer = arg0 as PointerEventData;
        if (asPointer == null)
        {
            return;
        }

        HaptiicEventForButtonsTargetpointer?.Invoke(asPointer.pointerId);
    }

    private void OnDisable()
    {
        _fieldInput = null;
        _withFields = null;
        foreach (var btn in AllKeyboardButtons)
        {
            if (btn == null)
            {
                continue;
            }

            btn.RemoveListener(OnEventTrigger);
        }
    }

    public void SubsribeTo(TMP_InputField withInoutField, bool asPassword)
    {
        if (keyboard == null)
        {
            return;
        }

        var wasNothing = _withFields == null || _fieldInput == null;
        _fieldInput = null;
        _withFields = withInoutField;
        keyboard.CloseOnInactivity = false;
        keyboard.OnClosed -= ClearAll;
        keyboard.OnClosed += ClearAll;
        if (asPassword)
        {
            keyboard.InputField.contentType = TMP_InputField.ContentType.Password;
        }
        else
        {
            keyboard.InputField.contentType = TMP_InputField.ContentType.Standard;
        }

        if (wasNothing)
        {
            EnableKeyboard(withInoutField);
        }
    }

    public void SubsribeTo(InputField withInoutField, bool asPassword)
    {
        if (keyboard == null)
        {
            return;
        }

        var wasNothing = _withFields == null || _fieldInput == null;
        _fieldInput = withInoutField;
        _withFields = null;

        keyboard.CloseOnInactivity = false;
        keyboard.OnClosed -= ClearAll;
        keyboard.OnClosed += ClearAll;
        if (asPassword)
        {
            keyboard.InputField.contentType = TMP_InputField.ContentType.Password;
        }
        else
        {
            keyboard.InputField.contentType = TMP_InputField.ContentType.Standard;
        }

        if (wasNothing)
        {
            EnableKeyboard(_fieldInput);
        }
    }

    /// <summary>
    /// Call it manually when underlying input field has changed content type
    /// </summary>
    public async void ForceReOpen()
    {
        if (keyboard == null || (_fieldInput == null && _withFields == null))
        {
            return;
        }

        keyboard.CloseOnInactivity = false;
        var previousSimpleInput = _fieldInput;
        var previousTmpInput = _withFields;
        TryClose();
        await UniTask.DelayFrame(1);
        await UniTask.SwitchToMainThread();
        bool isPassField = false;
        if (previousSimpleInput != null)
        {
            isPassField = previousSimpleInput.contentType == InputField.ContentType.Password;
            SubsribeTo(previousSimpleInput, isPassField);
            return;
        }

        if (previousTmpInput != null)
        {
            isPassField = previousTmpInput.contentType == TMP_InputField.ContentType.Password;
            SubsribeTo(previousTmpInput, isPassField);
        }
    }

    public void Unsubsribe()
    {
        _withFields = null;
        _fieldInput = null;
    }

    public void TryClose()
    {
        if (keyboard == null)
        {
            return;
        }

        keyboard.Close();
        keyboard.gameObject.SetActive(false);
    }

    /// <summary>
    /// Will propagate enter command and try to close keyboard
    /// </summary>
    public void ForceEnter()
    {
        if (keyboard == null)
        {
            return;
        }

        keyboard.Enter();
    }

    private void ClearAll(object sender, EventArgs e)
    {
        Unsubsribe();
    }

    private void EnableKeyboard(TMP_InputField withInoutField)
    {
        keyboard.OnTextUpdated -= UpdateText;
        keyboard.OnClosed -= DisableKeyboard;
        keyboard.OnTextSubmitted -= OnTextSubmit;

        keyboard.gameObject.SetActive(true);
        keyboard.PresentKeyboard(withInoutField.text);
        OnOpenKeyboard?.Invoke();

        keyboard.OnClosed += DisableKeyboard;
        keyboard.OnTextSubmitted += OnTextSubmit;
        keyboard.OnTextUpdated += UpdateText;
    }

    private void EnableKeyboard(InputField withInoutField)
    {
        keyboard.OnTextUpdated -= UpdateText;
        keyboard.OnClosed -= DisableKeyboard;
        keyboard.OnTextSubmitted -= OnTextSubmit;

        keyboard.gameObject.SetActive(true);
        keyboard.PresentKeyboard(withInoutField.text);
        OnOpenKeyboard?.Invoke();

        keyboard.OnClosed += DisableKeyboard;
        keyboard.OnTextSubmitted += OnTextSubmit;
        keyboard.OnTextUpdated += UpdateText;
    }

    private void UpdateText(string text)
    {
        if (_withFields != null)
        {
            _withFields.text = text;
        }

        if (_fieldInput != null)
        {
            _fieldInput.text = text;
        }
    }

    private void DisableKeyboard()
    {
        keyboard.OnTextUpdated -= UpdateText;
        keyboard.OnClosed -= DisableKeyboard;
        keyboard.OnTextSubmitted -= OnTextSubmit;

        keyboard.Close();
        OnCloseKeyboard?.Invoke();
        OnCloseKeyBoardEvent?.Invoke();
        keyboard.gameObject.SetActive(false);
    }

    private void OnTextSubmit(object sender, EventArgs e)
    {
        GameObject targetInputField = null;
        if (_withFields != null)
        {
            targetInputField = _withFields.gameObject;
        }

        if (_fieldInput != null)
        {
            targetInputField = _fieldInput.gameObject;
        }

        ExecuteEvents.ExecuteHierarchy(targetInputField, null, ExecuteEvents.submitHandler);
        // ExecuteEvents.Execute(targetInputField, null, ExecuteEvents.submitHandler);
        DisableKeyboard();
    }

    private void DisableKeyboard(object sender, EventArgs e)
    {
        DisableKeyboard();
    }
}