using UnityEngine;

namespace App.Core.Tools
{
    public static partial class BoundsExtensions
    {
        public interface IBoundsProvider
        {
            Bounds GetBounds();
            float GetBaseOffsetRaycast();
            Vector3 GetWorldAxisUp();

            /// <summary>
            /// Center - Position. Always use with the rotation of Identity. (this will ensure the bounds are exactly matched object's bounds)
            /// </summary>
            /// <returns></returns>
            Vector3 GetOffsetFromCenter();

            bool AreBoundsChanged();
        }
    }
}