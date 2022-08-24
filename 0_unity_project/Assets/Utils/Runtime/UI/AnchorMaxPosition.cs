using System;
using Cysharp.Threading.Tasks;
using QuickEditor.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace App.Core.UI
{
    [ExecuteAlways]
    public class AnchorMaxPosition : MonoBehaviour
    {
        [SerializeField] private float _maxHeight;
        [SerializeField] private LayoutElement _layoutElement;
        [SerializeField] private bool _editorUpdate;
        [SerializeField] private RectTransform _contentToCheck;

        [SerializeField] private GameObject _bottomNoRaycast;

        [SerializeField] private UnityEvent _increaseBottomForInvitations;
        [SerializeField] private UnityEvent _decreaseBottomForInvitations;

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if (_layoutElement == null)
            {
                _layoutElement = GetComponent<LayoutElement>();
            }

            if (!Application.isPlaying && _layoutElement != null && _contentToCheck != null)
            {
                if (!_editorUpdate)
                {
                    _editorUpdate = true;
                    CheckAndSetMaxAnchorPosition();
                }
            }
        }
#endif
        private void Update()
        {
            CheckAndSetMaxAnchorPosition();
        }

        private void CheckAndSetMaxAnchorPosition()
        {
            var currentHeight = _contentToCheck.GetHeight();
            if (currentHeight == 0)
            {
                return;
            }

            if (currentHeight >= _maxHeight)
            {
                _layoutElement.ignoreLayout = true;
                _increaseBottomForInvitations?.Invoke();
                if (_bottomNoRaycast != null)
                {
                    _bottomNoRaycast.SetActive(true);
                }
            }
            else
            {
                _layoutElement.ignoreLayout = false;
                _decreaseBottomForInvitations?.Invoke();
                if (_bottomNoRaycast != null)
                {
                    _bottomNoRaycast.SetActive(false);
                }
            }
        }
    }
}