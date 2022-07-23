using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Core.Tools
{
    public static partial class BoundsExtensions
    {
        [Flags]
        public enum SideEnum : int
        {
            NONE = 0,
            ON_X = 1 << 1,
            ON_Y = 1 << 2,
            ON_Z = 1 << 3,
        }

        private static bool AssignableBySide(int Side, float SideDistance, Bounds target, out Vector4 outcome)
        {
            bool result = false;
            outcome = new Vector4(0, 0, 0, 0);

            // Multiple also will pass
            if (SideDistance >= target.size.x)
            {
                result = true;
                outcome += new Vector4(Side, 0, 0, 0);
            }

            if (SideDistance >= target.size.y)
            {
                result = true;
                outcome += new Vector4(0, Side, 0, 0);
            }

            if (SideDistance >= target.size.z)
            {
                result = true;
                outcome += new Vector4(0, 0, Side, 0);
            }

            if (result)
            {
                outcome += new Vector4(0, 0, 0, Side);
            }

            return result;
        }

        public static bool ContainBoundsByVolume(this Bounds bounds, Bounds target)
        {
            var volume1 = bounds.size.x * bounds.size.y * bounds.size.z;
            var volume2 = target.size.x * target.size.y * target.size.z;
            return volume1 >= volume2;
        }

        public static bool CanBeFittedToBoundsSide(this Bounds bounds, Bounds target, out Vector4 planeSide)
        {
            bool result = false;
            int sideAsInt = (int) SideEnum.NONE;
            planeSide = new Vector4(0, 0, 0, sideAsInt);

            result |= AssignableBySide((int) SideEnum.ON_X, bounds.size.x, target, out Vector4 outcomeTemporal);
            planeSide += outcomeTemporal;

            result |= AssignableBySide((int) SideEnum.ON_Y, bounds.size.y, target, out outcomeTemporal);
            planeSide += outcomeTemporal;

            result |= AssignableBySide((int) SideEnum.ON_Z, bounds.size.z, target, out outcomeTemporal);
            planeSide += outcomeTemporal;
            return result;
        }

        /// <summary>
        /// Get the bounds of the compound object in World Space!
        /// </summary>
        /// <param name="go"></param>
        /// <param name="searchInactive"></param>
        /// <returns> World bounds</returns>
        public static Bounds GetBoundsOfVisualObject(GameObject go, bool searchInactive = false)
        {
            MeshFilter[] allrenderers = go.GetComponentsInChildren<MeshFilter>(searchInactive);
            Bounds any = new Bounds(go.transform.position, Vector3.zero);
            foreach (var item in allrenderers)
            {
                foreach (var points in BoundsAsPoints(item.sharedMesh.bounds, item.transform))
                {
                    any.Encapsulate(points);
                }
            }

            return any;
        }

        /// <summary>
        /// Get the bounds of the compound object in World Space!
        /// </summary>
        /// <param name="go"></param>
        /// <param name="searchInactive"></param>
        /// <returns> World bounds</returns>
        public static Bounds GetBoundsOfColliders(GameObject go, int layer, bool searchInactive = false)
        {
            var allcolliders = go.GetComponentsInChildren<Collider>(searchInactive);
            Bounds any = new Bounds(allcolliders.Length > 0 ? allcolliders[0].bounds.center : go.transform.position,
                Vector3.zero);
            for (int i = 0; i < allcolliders.Length; i++)
            {
                if ((allcolliders[i].gameObject.layer & layer) == allcolliders[i].gameObject.layer)
                {
                    any.Encapsulate(allcolliders[i].bounds);
                }
            }

            return any;
        }

        /// <summary>
        /// array of point from the given bounds
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="toWorldObject"> if not null will return in world position</param>
        /// <returns></returns>
        public static Vector3[] BoundsAsPoints(this Bounds bounds, Transform toWorldObject = null)
        {
            Vector3[] points = new Vector3[8];
            Vector3 v3Center = bounds.center;
            Vector3 v3Extents = bounds.extents;

            points[0] = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y,
                v3Center.z - v3Extents.z); // Front top left corner
            points[1] = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y,
                v3Center.z - v3Extents.z); // Front top right corner
            points[2] = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y,
                v3Center.z - v3Extents.z); // Front bottom left corner
            points[3] = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y,
                v3Center.z - v3Extents.z); // Front bottom right corner
            points[4] = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y,
                v3Center.z + v3Extents.z); // Back top left corner
            points[5] = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y,
                v3Center.z + v3Extents.z); // Back top right corner
            points[6] = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y,
                v3Center.z + v3Extents.z); // Back bottom left corner
            points[7] = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y,
                v3Center.z + v3Extents.z); // Back bottom right corner

            // Apply world scale, rotation
            if (toWorldObject != null)
            {
                points[0] = toWorldObject.TransformPoint(points[0]);
                points[1] = toWorldObject.TransformPoint(points[1]);
                points[2] = toWorldObject.TransformPoint(points[2]);
                points[3] = toWorldObject.TransformPoint(points[3]);
                points[4] = toWorldObject.TransformPoint(points[4]);
                points[5] = toWorldObject.TransformPoint(points[5]);
                points[6] = toWorldObject.TransformPoint(points[6]);
                points[7] = toWorldObject.TransformPoint(points[7]);
            }

            return points;
        }

        /// <summary>
        /// Transforms 'bounds' using the specified transform matrix.
        /// </summary>
        /// <remarks>
        /// Transforming a 'Bounds' instance means that the function will construct a new 'Bounds' 
        /// instance which has its center translated using the translation information stored in
        /// the specified matrix and its size adjusted to account for rotation and scale. The size
        /// of the new 'Bounds' instance will be calculated in such a way that it will contain the
        /// old 'Bounds'.
        /// </remarks>
        /// <param name="bounds">
        /// The 'Bounds' instance which must be transformed.
        /// </param>
        /// <param name="transformMatrix">
        /// The specified 'Bounds' instance will be transformed using this transform matrix. The function
        /// assumes that the matrix doesn't contain any projection or skew transformation.
        /// </param>
        /// <returns>
        /// The transformed 'Bounds' instance.
        /// </returns>
        public static Bounds Transform(this Bounds bounds, Matrix4x4 transformMatrix)
        {
            // We will need access to the right, up and look vector which are encoded inside the transform matrix
            Vector3 rightAxis = transformMatrix.GetColumn(0);
            Vector3 upAxis = transformMatrix.GetColumn(1);
            Vector3 lookAxis = transformMatrix.GetColumn(2);

            // We will 'imagine' that we want to rotate the bounds' extents vector using the rotation information
            // stored inside the specified transform matrix. We will need these when calculating the new size if
            // the transformed bounds.
            Vector3 rotatedExtentsRight = rightAxis * bounds.extents.x;
            Vector3 rotatedExtentsUp = upAxis * bounds.extents.y;
            Vector3 rotatedExtentsLook = lookAxis * bounds.extents.z;

            // Calculate the new bounds size along each axis. The size on each axis is calculated by summing up the 
            // corresponding vector component values of the rotated extents vectors. We multiply by 2 because we want
            // to get a size and currently we are working with extents which represent half the size.
            float newSizeX = (Mathf.Abs(rotatedExtentsRight.x) + Mathf.Abs(rotatedExtentsUp.x) +
                              Mathf.Abs(rotatedExtentsLook.x)) * 2.0f;
            float newSizeY = (Mathf.Abs(rotatedExtentsRight.y) + Mathf.Abs(rotatedExtentsUp.y) +
                              Mathf.Abs(rotatedExtentsLook.y)) * 2.0f;
            float newSizeZ = (Mathf.Abs(rotatedExtentsRight.z) + Mathf.Abs(rotatedExtentsUp.z) +
                              Mathf.Abs(rotatedExtentsLook.z)) * 2.0f;

            // Construct the transformed 'Bounds' instance
            var transformedBounds = new Bounds();
            transformedBounds.center = transformMatrix.MultiplyPoint(bounds.center);
            transformedBounds.size = new Vector3(newSizeX, newSizeY, newSizeZ);

            // Return the instance to the caller
            return transformedBounds;
        }
#if UNITY_EDITOR
        public static void DrawGizmoWireFrameBox(this Bounds boundsToDraw, Color color)
        {
            var allPoints = boundsToDraw.BoundsAsPoints();
            DrawBox(allPoints, color);
        }

        // World Pos
        private static void DrawBox(Vector3[] allPoints, Color color)
        {
            Debug.DrawLine(allPoints[0], allPoints[1], color);
            Debug.DrawLine(allPoints[1], allPoints[3], color);
            Debug.DrawLine(allPoints[3], allPoints[2], color);
            Debug.DrawLine(allPoints[2], allPoints[0], color);

            Debug.DrawLine(allPoints[4], allPoints[5], color);
            Debug.DrawLine(allPoints[5], allPoints[7], color);
            Debug.DrawLine(allPoints[7], allPoints[6], color);
            Debug.DrawLine(allPoints[6], allPoints[4], color);

            Debug.DrawLine(allPoints[0], allPoints[4], color);
            Debug.DrawLine(allPoints[1], allPoints[5], color);
            Debug.DrawLine(allPoints[3], allPoints[7], color);
            Debug.DrawLine(allPoints[2], allPoints[6], color);
        }
#endif
    }
}