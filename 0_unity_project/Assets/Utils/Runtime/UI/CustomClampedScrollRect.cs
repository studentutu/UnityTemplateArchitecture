using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace App.Core
{
    public class CustomClampedScrollRect : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _checkIfStillInBounds;
        [SerializeField] private RectTransform _checkIfInThisBoundUpper;
        [SerializeField] private RectTransform _checkIfInThisBoundLower;
        [SerializeField] private float _maxDeviation = 0.001f;

        private Bounds? _lower;
        private Bounds? _upper;
        private float _timerDeceleration = 0;

        private void OnEnable()
        {
            _scrollRect.movementType = ScrollRect.MovementType.Elastic;
            StartCoroutine(WaitToCreateBounds());
        }

        private IEnumerator WaitToCreateBounds()
        {
            yield return new WaitForSeconds(0.1f);
            _upper = _checkIfInThisBoundUpper.GetBounds();
            _lower = _checkIfInThisBoundLower.GetBounds();
        }

        private void LateUpdate()
        {
            if (_upper == null || _lower == null)
            {
                _scrollRect.movementType = ScrollRect.MovementType.Elastic;
                return;
            }

            if (_timerDeceleration > 0)
            {
                _scrollRect.movementType = ScrollRect.MovementType.Elastic;
                _timerDeceleration -= Time.deltaTime;
                _scrollRect.velocity = Vector2.zero;
                ForceUpdateAndStopDrag();
                return;
            }

            if (!_checkIfStillInBounds.IsInsideBoundary(_lower.Value) ||
                !_checkIfStillInBounds.IsInsideBoundary(_upper.Value))
            {
                CheckAndIfNeededSlowDown();
            }
            else
            {
                _scrollRect.movementType = ScrollRect.MovementType.Elastic;
            }
        }

        private void CheckAndIfNeededSlowDown()
        {
            _scrollRect.velocity = Vector2.zero;
            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            var currentNormliziedPosition = 0f;
            if (_scrollRect.horizontal)
                currentNormliziedPosition = _scrollRect.horizontalNormalizedPosition;
            if (_scrollRect.vertical)
                currentNormliziedPosition = _scrollRect.verticalNormalizedPosition;

            if (currentNormliziedPosition < 0 && currentNormliziedPosition > -_maxDeviation)
            {
                return;
            }

            if (currentNormliziedPosition > 1 && currentNormliziedPosition < (1 + _maxDeviation))
            {
                return;
            }

            _timerDeceleration = 0.3f;
            _scrollRect.movementType = ScrollRect.MovementType.Elastic;
            ForceUpdateAndStopDrag();
        }

        private void ForceUpdateAndStopDrag()
        {
            PointerEventData data = new PointerEventData(null);
            data.button = PointerEventData.InputButton.Right;
            
            _scrollRect.OnEndDrag(data);
        }

        private void OnDisable()
        {
            _scrollRect.movementType = ScrollRect.MovementType.Elastic;
        }
    }
}