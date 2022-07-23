using TMPro;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

public class TMP_InvokeKeyBoard : MonoBehaviour
{
    [SerializeField] private bool PassField = false;
    [SerializeField] private TMP_InputField _tmp_field;

    [SerializeField] private bool _deselectAll;

    [Tooltip("Optional if tmpro is not in use or null")] [SerializeField]
    private InputField _alternativeInputField;

    private void OnEnable()
    {
        if (_tmp_field != null)
        {
            _tmp_field.onSelect.AddListener(InvokeKeyboard);
        }

        if (_alternativeInputField != null)
        {
            _alternativeInputField.shouldHideMobileInput = true;
        }
    }

    [Preserve]
    public void CheckSelectOnInoutField()
    {
        InvokeKeyboard(_alternativeInputField.text);
    }

    private void OnDisable()
    {
        if (_tmp_field != null)
        {
            _tmp_field.onSelect.RemoveListener(InvokeKeyboard);
        }

        OnCloseKeyBoard();
    }

    private void InvokeKeyboard(string arg0)
    {
        var instance = RigNonStandardKeyboardRef.Instance;
        if (instance == null || instance.Keyboard == null)
        {
            return;
        }

        instance.Unsubsribe();
        if (_tmp_field != null)
        {
            instance.SubsribeTo(_tmp_field, PassField);
        }

        if (_alternativeInputField != null)
        {
            instance.SubsribeTo(_alternativeInputField, PassField);
        }


        instance.OnCloseKeyBoardEvent -= OnCloseKeyBoard;
        instance.OnCloseKeyBoardEvent += OnCloseKeyBoard;
        if (_deselectAll)
        {
            string tmp = instance.Keyboard.InputField.text;
            if (!string.IsNullOrEmpty(tmp))
            {
                for (int i = 0; i < tmp.Length; i++)
                {
                    instance.Keyboard.MoveCaretRight();
                }
            }
        }
    }

    private void OnCloseKeyBoard()
    {
        var instance = RigNonStandardKeyboardRef.Instance;
        if (instance == null || instance.Keyboard == null)
        {
            return;
        }

        instance.Unsubsribe();
        instance.OnCloseKeyBoardEvent -= OnCloseKeyBoard;
    }
}