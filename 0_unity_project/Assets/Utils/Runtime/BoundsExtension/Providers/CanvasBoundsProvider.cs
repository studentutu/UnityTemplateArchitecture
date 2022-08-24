using TMPro;
using UnityEngine;

namespace App.Core.Tools
{
    public class CanvasBoundsProvider : BoundsProvider
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private TextSizerTMPRO textSizer;

        private Bounds WorldSpaceBounds
        {
            get
            {
                if (string.IsNullOrEmpty(text.text))
                {
                    text.text = " ";
                }
                text.ForceMeshUpdate(false);
                textSizer.Refresh();
                Vector3[] worldCorners = new Vector3[4];
                text.rectTransform.GetWorldCorners(worldCorners);
                Bounds newBounds = new Bounds(text.transform.position, Vector3.zero);
                foreach (var point in worldCorners)
                {
                    newBounds.Encapsulate(point);
                }
                return newBounds;
            }
        }

        public override Bounds GetBounds()
        {
            return WorldSpaceBounds;
        }

        public override float GetBaseOffsetRaycast()
        {
            return Constants.Generation.OFFSET_TEXT;
        }

        public override bool AreBoundsChanged()
        {
            // TODO: check if the previous text equal to current text
            return true;
        }
    }
}