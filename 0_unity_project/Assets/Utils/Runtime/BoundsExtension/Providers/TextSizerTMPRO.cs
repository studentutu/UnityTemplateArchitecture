using System;
using TMPro;
using UnityEngine;

namespace App.Core.Tools
{
    [ExecuteInEditMode]
    public class TextSizerTMPRO : MonoBehaviour
    {
        [SerializeField] private bool forceEditResize = false;

        [Tooltip(" Leave it on to resize the text rect as well. Needed for boudns!")] [SerializeField]
        private bool resizeTextObject = true;

        [SerializeField] private TMP_Text text = null;
        [SerializeField] private Vector2 padding = default;
        [SerializeField] private Vector2 maxSize = new Vector2(1000, float.PositiveInfinity);
        [SerializeField] private Vector2 MinSize = default;
        [SerializeField] private Mode controlAxes = Mode.Both;

        [Flags]
        public enum Mode
        {
            None = 0,
            Horizontal = 0x1,
            Vertical = 0x2,
            Both = Horizontal | Vertical
        }

        private string lastText;
        private Mode lastControlAxes = Mode.None;
        private Vector2 lastSize;
        private bool forceRefresh;
        private bool isTextNull = true;
        private RectTransform textRectTransform = null;
        private RectTransform selfRectTransform = null;

        private RectTransform TextRectTransform
        {
            get
            {
                if (textRectTransform == null)
                {
                    textRectTransform = text.GetComponent<RectTransform>();
                }

                return textRectTransform;
            }
        }

        private RectTransform SelfRectTransform
        {
            get
            {
                if (selfRectTransform == null)
                {
                    selfRectTransform = GetComponent<RectTransform>();
                }

                return selfRectTransform;
            }
        }

        protected virtual float MinX
        {
            get
            {
                if ((controlAxes & Mode.Horizontal) != 0) return MinSize.x;
                return SelfRectTransform.rect.width - padding.x;
            }
        }

        protected virtual float MinY
        {
            get
            {
                if ((controlAxes & Mode.Vertical) != 0) return MinSize.y;
                return SelfRectTransform.rect.height - padding.y;
            }
        }

        protected virtual float MaxX
        {
            get
            {
                if ((controlAxes & Mode.Horizontal) != 0) return maxSize.x;
                return SelfRectTransform.rect.width - padding.x;
            }
        }

        protected virtual float MaxY
        {
            get
            {
                if ((controlAxes & Mode.Vertical) != 0) return maxSize.y;
                return SelfRectTransform.rect.height - padding.y;
            }
        }

        protected virtual void Update()
        {
            if (!isTextNull && (text.text != lastText || lastSize != SelfRectTransform.rect.size || forceRefresh ||
                                controlAxes != lastControlAxes))
            {
                var preferredSize = text.GetPreferredValues(MaxX, MaxY);
                preferredSize.x = Mathf.Clamp(preferredSize.x, MinX, MaxX);
                preferredSize.y = Mathf.Clamp(preferredSize.y, MinY, MaxY);
                preferredSize += padding;

                if ((controlAxes & Mode.Horizontal) != 0)
                {
                    SelfRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredSize.x);
                    if (resizeTextObject)
                    {
                        TextRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredSize.x);
                    }
                }

                if ((controlAxes & Mode.Vertical) != 0)
                {
                    SelfRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredSize.y);
                    if (resizeTextObject)
                    {
                        TextRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredSize.y);
                    }
                }

                lastText = text.text;
                lastSize = SelfRectTransform.rect.size;
                lastControlAxes = controlAxes;
                forceRefresh = false;
            }
        }

        // Forces a size recalculation on next Update
        public virtual void Refresh()
        {
            forceRefresh = true;
            isTextNull = text == null;
            Update();
        }

        private void OnValidate()
        {
            if (forceEditResize)
            {
                forceEditResize = false;
                Refresh();
            }
        }
    }
}