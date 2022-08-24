using System;
using UnityEngine;

namespace App.Core.Tools
{
    public static partial class BoundsExtensions
    {
        /// <summary>
        /// Once set - bounds are not changing
        /// </summary>
        public class BoundsProviderSimple : IBoundsProvider
        {
            private readonly Transform transform;
            private readonly float offsetFrombaseForRaycast;
            private Vector3 upLocalAxis;
            private Vector3 offsetFromCenter;
            private Func<Bounds> worldBounds;

            private BoundsProviderSimple()
            {
            }

            /// <summary>
            /// Provider which will use Transfrom to calculate world up axis
            /// </summary>
            /// <param name="operateOn"></param>
            /// <param name="worldBounds">Custom Canvas BOudns?</param>
            /// <param name="offset"></param>
            /// <param name="upLocalAxis"> e.g Vector3.right(1,0,0), Vector3.Up and so on, also if negative - will be inverted</param>
            public BoundsProviderSimple(Transform operateOn, Func<Bounds> worldBounds, float offset,
                Vector3 upLocalAxis)
            {
                transform = operateOn;
                this.upLocalAxis = upLocalAxis;
                this.offsetFrombaseForRaycast = offset;
                this.worldBounds = worldBounds;
            }

            public BoundsProviderSimple(Vector3 offsetFromCenter, Func<Bounds> worldBounds, float offset,
                Vector3 upWorldAxis)
            {
                transform = null;
                this.upLocalAxis = upWorldAxis;
                this.offsetFrombaseForRaycast = offset;
                this.offsetFromCenter = offsetFromCenter;
                this.worldBounds = worldBounds;
            }

            Bounds IBoundsProvider.GetBounds()
            {
                return worldBounds();
            }

            float IBoundsProvider.GetBaseOffsetRaycast()
            {
                return offsetFrombaseForRaycast;
            }

            Vector3 IBoundsProvider.GetWorldAxisUp()
            {
                if (transform == null)
                {
                    return upLocalAxis;
                }

                Vector3 axis = Vector3.zero;
                Plane helpPlane;
                if (upLocalAxis.x > 0)
                {
                    axis = transform.right;
                }

                if (upLocalAxis.y > 0)
                {
                    axis = transform.up;
                }

                if (upLocalAxis.z > 0)
                {
                    axis = transform.forward;
                }

                if (upLocalAxis.x < 0)
                {
                    helpPlane = new Plane(transform.right, transform.position);
                    helpPlane = helpPlane.flipped;
                    axis = helpPlane.normal;
                }

                if (upLocalAxis.y < 0)
                {
                    helpPlane = new Plane(transform.up, transform.position);
                    helpPlane = helpPlane.flipped;
                    axis = helpPlane.normal;
                }

                if (upLocalAxis.z < 0)
                {
                    helpPlane = new Plane(transform.forward, transform.position);
                    helpPlane = helpPlane.flipped;
                    axis = helpPlane.normal;
                }

                return axis;
            }

            Vector3 IBoundsProvider.GetOffsetFromCenter()
            {
                if (transform == null)
                {
                    return offsetFromCenter;
                }

                var thisBounds = ((IBoundsProvider) this).GetBounds();
                return thisBounds.center - transform.position;
            }

            bool IBoundsProvider.AreBoundsChanged()
            {
                return false;
            }
        }
    }
}