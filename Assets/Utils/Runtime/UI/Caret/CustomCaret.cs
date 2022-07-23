using System.Collections.Generic;
using App.Core.Extensions;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;

namespace Virtuix.Launcher
{
    public class CustomCaret : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private TMP_Text TextObject;
        [SerializeField] private TMP_Text ActualCaret;

        [SerializeField]
        private UnityEventTriggerSubscriber onClick = new UnityEventTriggerSubscriber(EventTriggerType.PointerClick);

        [SerializeField] private float CursorDelay = 0.3f;
        private float _timer = 0f;
        private int _currentCaretAt = -1;

        private void OnEnable()
        {
            TextObject.text = _inputField.text;
            ActualCaret.enabled = true;
            _inputField.onValueChanged.AddListener(OnChangedInput);
            onClick.AddListener(OnClickMovePointerByEvenData);
        }

        private void OnDisable()
        {
            TextObject.text = "";
            ActualCaret.enabled = false;
            _timer = 0;
            _currentCaretAt = -1;
            _inputField.onValueChanged.RemoveListener(OnChangedInput);
            onClick.RemoveListener(OnClickMovePointerByEvenData);
        }

        [Preserve]
        public void MoveCaretToLastCharacter()
        {
            TextObject.text = _inputField.text;
            _inputField.caretPosition = _inputField.text.Length;
            _timer = 0;
            _currentCaretAt = -1;
        }

        private void OnClickMovePointerByEvenData(BaseEventData eventData)
        {
            // Get World Point
            var asPointerEventData = eventData as PointerEventData;
            if (asPointerEventData == null)
            {
                return;
            }

            var text = _inputField.textComponent.text;

            // Transform Into Local Anchored Position
            var positionWorld = asPointerEventData.pointerCurrentRaycast.worldPosition;
            var fromExtension = RectTransformExtensions.GetPixelFromWorldPoint(_inputField.textViewport, positionWorld);

            // find nearest
            float currentX = 0;
            int maxNumber = text.Length;

            float constantValue = 0;
            for (int i = 0; i < maxNumber; i++)
            {
                if (i == 1)
                {
                    constantValue = TextObject.textInfo.characterInfo[i].xAdvance;
                }

                if (currentX >= fromExtension.x)
                {
                    _inputField.caretPosition = Mathf.Clamp(i - 1, 0, maxNumber);
                    return;
                }

                if (TextObject.textInfo.characterInfo.Length > i)
                {
                    currentX = TextObject.textInfo.characterInfo[i].xAdvance;
                }
                else
                {
                    currentX += constantValue;
                }
            }

            _inputField.caretPosition = maxNumber;
        }


        private async void OnChangedInput(string arg0)
        {
            await UniTask.DelayFrame(1);
            var text = _inputField.textComponent.text;
            _currentCaretAt = Mathf.Clamp(_inputField.caretPosition, 0, text.Length);
            TextObject.text = GenerateSubstring(_currentCaretAt, text);
            ActualCaret.enabled = false;
            _timer = 0;
        }

        [Preserve]
        public async void OnChangeCaretPositionClick()
        {
            await UniTask.DelayFrame(1);
            var text = _inputField.textComponent.text;
            _currentCaretAt = Mathf.Clamp(_inputField.caretPosition, 0, text.Length);
            TextObject.text = GenerateSubstring(_currentCaretAt, text);
            ActualCaret.enabled = true;
            _timer = 0;
        }

        private static string GenerateSubstring(int until, string fromString)
        {
            if (until >= fromString.Length)
            {
                return fromString;
            }

            if (until <= 0)
            {
                return "";
            }

            return fromString.Substring(0, until);
        }

        private void Update()
        {
            var text = _inputField.textComponent.text;

            var previous = _currentCaretAt;
            _currentCaretAt = Mathf.Clamp(_inputField.caretPosition, 0, text.Length);
            if (previous != _currentCaretAt || text.EndsWith(" "))
            {
                TextObject.text = GenerateSubstring(_currentCaretAt, text);
            }

            //do blink
            _timer += Time.deltaTime;
            if (_timer > CursorDelay)
            {
                _timer = 0f;
                ActualCaret.enabled = !ActualCaret.enabled;
            }
        }
    }
}