using UnityEngine;

namespace App.Core.Tools
{
    [DisallowMultipleComponent]
    public class BoundsProvider : MonoBehaviour, BoundsExtensions.IBoundsProvider
    {
        private BoundsExtensions.ExtendedBounds actualBounds;

        public BoundsExtensions.ExtendedBounds ExtendedBounds => actualBounds ?? (actualBounds = new BoundsExtensions.ExtendedBounds(gameObject, this));
        
        public virtual Bounds GetBounds()
        {
            return BoundsExtensions.GetBoundsOfVisualObject(gameObject);
        }

        public virtual float GetBaseOffsetRaycast()
        {
            return Constants.Generation.OFFSET_STANDARD_OBJECTS;
        }

        public Vector3 GetWorldAxisUp()
        {
            return transform.up;
        }

        /// <summary>
        /// Center - position
        /// </summary>
        /// <returns></returns>
        public virtual Vector3 GetOffsetFromCenter()
        {
            var previous = transform.rotation;
            transform.rotation = Quaternion.identity;
            var thisBounds = GetBounds(); // this will ensure the position will not change 
            transform.rotation = previous;
            return thisBounds.center - transform.position;
        }

        public virtual bool AreBoundsChanged()
        {
            return false;
        }


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            ExtendedBounds.ActualBoundsWorld.DrawGizmoWireFrameBox(Color.yellow);
        }

#endif
    }
}