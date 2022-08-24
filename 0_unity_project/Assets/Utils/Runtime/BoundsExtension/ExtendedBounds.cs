using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Core.Tools
{
    public static partial class BoundsExtensions
    {
        public class ExtendedBounds : IDisposable
        {
            private const float EPSILON = 0.000000025f;
            private const float LINE_INTERCEPT_MAX_DISTANCE = 0.0638f;
            public readonly static int[] TopLines = new int[] {0, 4, 8, 10};
            public readonly static int[] BottomLines = new int[] {3, 7, 9, 11};
            public readonly static int[] LeftLines = new int[] {1, 5, 8, 9};
            public readonly static int[] RightLines = new int[] {2, 6, 10, 11};
            public readonly static int[] FrontLines = new int[] {0, 1, 2, 3};
            public readonly static int[] BackLines = new int[] {4, 5, 6, 7};
            public readonly static int[] PlanesTopBottom = new int[] {0, 3};
            public readonly static int[] PlanesLeftRight = new int[] {1, 4};
            public readonly static int[] PlanesFrontBack = new int[] {2, 5};

            private static Transform helperChild = null;
            private static Transform helperParent = null;

            private static Transform HelperChild
            {
                get
                {
                    if (helperChild == null)
                    {
                        var helperGo = new GameObject
                        {
                            hideFlags = HideFlags.HideAndDontSave
                        };
                        helperChild = helperGo.transform;
                    }

                    return helperChild;
                }
            }

            private static Transform HelperParent
            {
                get
                {
                    if (helperParent == null)
                    {
                        var helperGo = new GameObject
                        {
                            hideFlags = HideFlags.HideAndDontSave
                        };
                        helperParent = helperGo.transform;
                    }

                    return helperParent;
                }
            }

            /// <summary>
            /// Be aware when setting position also calculate in the offset!
            /// </summary>
            /// <value></value>
            public Vector3 Position
            {
                get
                {
                    if (operateOn != null)
                    {
                        position = operateOn.transform.position;
                    }

                    return position;
                }
                set
                {
                    if (operateOn != null)
                    {
                        operateOn.transform.position = value;
                    }

                    position = value;
                }
            }

            public Quaternion Rotation
            {
                get
                {
                    if (operateOn != null)
                    {
                        rotation = operateOn.transform.rotation;
                    }

                    return rotation;
                }
                set
                {
                    if (operateOn != null)
                    {
                        operateOn.transform.rotation = value;
                    }

                    rotation = value;
                }
            }

            public Vector3 WorldScale
            {
                get
                {
                    if (operateOn != null)
                    {
                        scale = operateOn.transform.lossyScale;
                    }

                    return scale;
                }
                set
                {
                    if (operateOn != null)
                    {
                        operateOn.transform.SetGlobalScale(value);
                    }

                    scale = value;
                }
            }

            public Vector3 WorldAxisUp
            {
                get
                {
                    if (operateOn == null)
                    {
                        return Rotation * provider.GetWorldAxisUp();
                    }

                    return provider.GetWorldAxisUp();
                }
            }

            public float OffsetForWorldPoint
            {
                get { return provider.GetBaseOffsetRaycast(); }
            }

            private Vector3? offsetFromCenter = null;

            protected Vector3 OffsetFromCenter
            {
                get
                {
                    if (offsetFromCenter == null)
                    {
                        offsetFromCenter = provider.GetOffsetFromCenter();
                    }

                    return offsetFromCenter.Value;
                }
            }

            private bool disposed = false;
            private readonly GameObject operateOn;
            private readonly IBoundsProvider provider;
            private Vector3 position;
            private Quaternion rotation;
            private Vector3 scale;

            private Quaternion lastRotationLines = Quaternion.identity;
            private Vector3 worldScaleLines = Vector3.one;
            private Vector3 worldPosLines = Vector3.zero;
            private ValueTuple<Vector3, Vector3>[] lineSegments = null;

            private Quaternion lastRotationPlanes = Quaternion.identity;
            private Vector3 worldScalePlanes = Vector3.one;
            private Vector3 worldPosPlanes = Vector3.zero;
            private ValueTuple<Vector3, Plane>[] planes = null;

            private Quaternion lastRotationSize = Quaternion.identity;
            private Vector3 worldScaleSize = Vector3.one;
            private Vector3 worldPosSize = Vector3.zero;
            private Vector3? actualSize = null;

            private Bounds? actualBoundsWorld = null;
            private Quaternion lastRotationActualBounds = Quaternion.identity;
            private Vector3 lastPositionActualBounds = Vector3.zero;
            private Vector3 lastWorldScaleActualBounds = Vector3.one;

            public Bounds ActualBoundsWorld
            {
                get
                {
                    if (actualBoundsWorld == null ||
                        provider.AreBoundsChanged() ||
                        internalBoundsUpdate ||
                        IsChangedPosition(Position, lastPositionActualBounds) ||
                        IsChangedRotation(Rotation, lastRotationActualBounds) ||
                        IsChangedScale(WorldScale, lastWorldScaleActualBounds))
                    {
                        var bounds = new Bounds(ActualCenter, Vector3.zero);
                        foreach (var item in GetFromLinePoints())
                        {
                            bounds.Encapsulate(item);
                        }

                        actualBoundsWorld = bounds;
                    }

                    return actualBoundsWorld.Value;
                }
            }

            private Quaternion lastRotationBoundsStandart = Quaternion.identity;
            private Vector3 worldScaleBoundsStandart = Vector3.one;
            private Vector3 worldPosBoundsStandart = Vector3.zero;
            private Bounds? boundsStandart = null;
            private Vector3 changeIn = Vector3.one;
            private bool internalBoundsUpdate = false;

            /// <summary>
            /// Smart Update only when changed. Note - you should always prefer to use Actual Size and Actual Center!
            /// </summary>
            /// <value></value>
            public Bounds BoundsStandart
            {
                get
                {
                    if (boundsStandart == null ||
                        provider.AreBoundsChanged() ||
                        internalBoundsUpdate ||
                        IsChangedPosition(Position, worldPosBoundsStandart) ||
                        IsChangedRotation(Rotation, lastRotationBoundsStandart) ||
                        IsChangedScale(WorldScale, worldScaleBoundsStandart))
                    {
                        if (operateOn == null && actualSize != null)
                        {
                            // Size should be setted beforehand
                            changeIn = Vector3.one;
                            if ((WorldScale - worldScaleBoundsStandart).sqrMagnitude >= EPSILON)
                            {
                                changeIn = new Vector3((WorldScale.x / worldScaleBoundsStandart.x),
                                    (WorldScale.y / worldScaleBoundsStandart.y),
                                    (WorldScale.z / worldScaleBoundsStandart.z));
                            }

                            var direction = OffsetFromCenter.normalized;
                            var magnitude = OffsetFromCenter.magnitude;
                            var center = Position + magnitude * Vector3.Scale(changeIn, direction);
                            Vector3 newBoundsSize = Vector3.Scale(actualSize.Value, changeIn);

                            // position is pivot
                            // Debug.LogWarning(" Change offset from : " + OffsetFromCenter.ToString("F4") + " to " + newWorldOffset.ToString("F4"));
                            boundsStandart = new Bounds(center, newBoundsSize);
                        }
                        else
                        {
                            boundsStandart = provider.GetBounds();
                        }

                        lastRotationBoundsStandart = Rotation;
                        worldPosBoundsStandart = Position;
                        worldScaleBoundsStandart = WorldScale;
                    }

                    return boundsStandart.Value;
                }
            }

            /// <summary>
            /// Setter only works if there was no tranform from the begining
            /// </summary>
            /// <value> X - right left, Y - Top Bottom, Z - Front Back </value>
            public Vector3 ActualSize
            {
                get
                {
                    if (actualSize == null ||
                        provider.AreBoundsChanged() ||
                        IsChangedPosition(Position, worldPosSize) ||
                        IsChangedRotation(Rotation, lastRotationSize) ||
                        IsChangedScale(WorldScale, worldScaleSize))
                    {
                        var currentPlanes = Planes;
                        float onX = (currentPlanes[PlanesLeftRight[0]].Item1 - currentPlanes[PlanesLeftRight[1]].Item1)
                            .magnitude; // left right
                        float onY = (currentPlanes[PlanesTopBottom[0]].Item1 - currentPlanes[PlanesTopBottom[1]].Item1)
                            .magnitude; // top bottom
                        float onZ = (currentPlanes[PlanesFrontBack[0]].Item1 - currentPlanes[PlanesFrontBack[1]].Item1)
                            .magnitude; // front back
                        actualSize = new Vector3(onX, onY, onZ);
                        worldPosSize = Position;
                        worldScaleSize = WorldScale;
                        lastRotationSize = Rotation;
                    }

                    return actualSize.Value;
                }
            }

            public Vector3 ActualCenter
            {
                get
                {
                    Vector3[] points = GetActualPoints();
                    return Math3d.AverageVector(points);
                }
            }

            public ValueTuple<Vector3, Vector3>[] LineSegments
            {
                get
                {
                    if (lineSegments == null ||
                        provider.AreBoundsChanged() ||
                        IsChangedPosition(Position, worldPosLines) ||
                        IsChangedRotation(Rotation, lastRotationLines) ||
                        IsChangedScale(WorldScale, worldScaleLines))
                    {
                        UpdateLines();
                        lastRotationLines = Rotation;
                        worldScaleLines = WorldScale;
                        worldPosLines = Position;
                    }

                    return lineSegments;
                }
            }

            private static bool IsChangedPosition(Vector3 current, Vector3 old)
            {
                return (current - old).sqrMagnitude >= EPSILON;
            }

            private static bool IsChangedRotation(Quaternion current, Quaternion old)
            {
                return (current.eulerAngles - old.eulerAngles).sqrMagnitude >= EPSILON;
            }

            private static bool IsChangedScale(Vector3 current, Vector3 old)
            {
                return (current - old).sqrMagnitude >= EPSILON;
            }

            private static Plane GetPlaneFrom(Vector3 inNormal, Vector3 center)
            {
                return new Plane(inNormal, center);
            }


            private void UpdateLines()
            {
                if (lineSegments == null)
                {
                    lineSegments = new ValueTuple<Vector3, Vector3>[12];
                }

                Vector3[] points = GetActualPoints();

                // points[0] = // Front top left corner
                // points[1] = // Front top right corner
                // points[2] = // Front bottom left corner
                // points[3] = // Front bottom right corner
                // points[4] = // Back top left corner
                // points[5] = // Back top right corner
                // points[6] = // Back bottom left corner
                // points[7] = // Back bottom right corner

                // Front top left to Front top right
                lineSegments[0] = new ValueTuple<Vector3, Vector3>(points[0], points[1]);
                // Front top left to Front bottom left
                lineSegments[1] = new ValueTuple<Vector3, Vector3>(points[0], points[2]);
                // Front top right to Front bottom right
                lineSegments[2] = new ValueTuple<Vector3, Vector3>(points[1], points[3]);
                // Front bottom left to Front bottom right
                lineSegments[3] = new ValueTuple<Vector3, Vector3>(points[2], points[3]);

                // Back top left to Back top right
                lineSegments[4] = new ValueTuple<Vector3, Vector3>(points[4], points[5]);
                // Back top left to Back bottom left
                lineSegments[5] = new ValueTuple<Vector3, Vector3>(points[4], points[6]);
                // Back top right to Back bottom right
                lineSegments[6] = new ValueTuple<Vector3, Vector3>(points[5], points[7]);
                // Back bottom left to Back bottom right
                lineSegments[7] = new ValueTuple<Vector3, Vector3>(points[6], points[7]);


                // Front top left to Back top Left
                lineSegments[8] = new ValueTuple<Vector3, Vector3>(points[0], points[4]);
                // Front bottom left to Back bottom left
                lineSegments[9] = new ValueTuple<Vector3, Vector3>(points[2], points[6]);
                // Front top right to Back top right
                lineSegments[10] = new ValueTuple<Vector3, Vector3>(points[1], points[5]);
                // Front bottom right to Back bottom right
                lineSegments[11] = new ValueTuple<Vector3, Vector3>(points[3], points[7]);
            }

            public ValueTuple<Vector3, Plane>[] Planes
            {
                get
                {
                    if (planes == null ||
                        provider.AreBoundsChanged() ||
                        (lastRotationPlanes.eulerAngles - Rotation.eulerAngles).sqrMagnitude >= EPSILON ||
                        (Position - worldPosPlanes).sqrMagnitude >= EPSILON ||
                        (worldScalePlanes - WorldScale).sqrMagnitude >= EPSILON)
                    {
                        UpdatePLanes();
                        lastRotationPlanes = Rotation;
                        worldScalePlanes = WorldScale;
                        worldPosPlanes = Position;
                    }

                    return planes;
                }
            }

            private void UpdatePLanes()
            {
                if (planes == null)
                {
                    planes = new ValueTuple<Vector3, Plane>[6];
                }

                Vector3[] points = GetActualPoints();
                var Center = Math3d.AverageVector(points);
                // points[0] = // Front top left corner
                // points[1] = // Front top right corner
                // points[2] = // Front bottom left corner
                // points[3] = // Front bottom right corner
                // points[4] = // Back top left corner
                // points[5] = // Back top right corner
                // points[6] = // Back bottom left corner
                // points[7] = // Back bottom right corner
                List<Vector3> pointsForPlane = new List<Vector3>(5);
                Vector3 currentCenter;
                Vector3 newNormal;

                // Top
                pointsForPlane.Add(points[0]);
                pointsForPlane.Add(points[1]);
                pointsForPlane.Add(points[4]);
                pointsForPlane.Add(points[5]);
                currentCenter = Math3d.AverageVector(pointsForPlane.ToArray());
                newNormal = Center - currentCenter;
                if (newNormal.sqrMagnitude <= EPSILON)
                {
                    HelperParent.position = currentCenter;
                    HelperChild.rotation = Rotation;
                    newNormal = HelperChild.up;
                }

                planes[0] = new ValueTuple<Vector3, Plane>();
                planes[0].Item1 = currentCenter;
                planes[0].Item2 = GetPlaneFrom(newNormal, currentCenter);
                pointsForPlane.Clear();

                // Right
                pointsForPlane.Add(points[1]);
                pointsForPlane.Add(points[3]);
                pointsForPlane.Add(points[5]);
                pointsForPlane.Add(points[7]);
                currentCenter = Math3d.AverageVector(pointsForPlane.ToArray());
                newNormal = Center - currentCenter;
                if (newNormal.sqrMagnitude <= EPSILON)
                {
                    HelperParent.position = currentCenter;
                    HelperChild.rotation = Rotation;
                    newNormal = HelperChild.right;
                }

                planes[1] = new ValueTuple<Vector3, Plane>();
                planes[1].Item1 = currentCenter;
                planes[1].Item2 = GetPlaneFrom(newNormal, currentCenter);
                pointsForPlane.Clear();

                // Front
                pointsForPlane.Add(points[0]);
                pointsForPlane.Add(points[1]);
                pointsForPlane.Add(points[2]);
                pointsForPlane.Add(points[3]);
                currentCenter = Math3d.AverageVector(pointsForPlane.ToArray());
                newNormal = Center - currentCenter;
                if (newNormal.sqrMagnitude <= EPSILON)
                {
                    HelperParent.position = currentCenter;
                    HelperChild.rotation = Rotation;
                    newNormal = HelperChild.forward;
                }

                planes[2] = new ValueTuple<Vector3, Plane>();
                planes[2].Item1 = currentCenter;
                planes[2].Item2 = GetPlaneFrom(newNormal, currentCenter);
                pointsForPlane.Clear();

                // Bottom
                pointsForPlane.Add(points[2]);
                pointsForPlane.Add(points[3]);
                pointsForPlane.Add(points[6]);
                pointsForPlane.Add(points[7]);
                currentCenter = Math3d.AverageVector(pointsForPlane.ToArray());
                newNormal = Center - currentCenter;
                if (newNormal.sqrMagnitude <= EPSILON)
                {
                    HelperParent.position = currentCenter;
                    HelperChild.rotation = Rotation;
                    newNormal = -HelperChild.up;
                }

                planes[3] = new ValueTuple<Vector3, Plane>();
                planes[3].Item1 = currentCenter;
                planes[3].Item2 = GetPlaneFrom(newNormal, currentCenter);
                pointsForPlane.Clear();

                // Left
                pointsForPlane.Add(points[0]);
                pointsForPlane.Add(points[2]);
                pointsForPlane.Add(points[4]);
                pointsForPlane.Add(points[6]);
                currentCenter = Math3d.AverageVector(pointsForPlane.ToArray());
                newNormal = Center - currentCenter;
                if (newNormal.sqrMagnitude <= EPSILON)
                {
                    HelperParent.position = currentCenter;
                    HelperChild.rotation = Rotation;
                    newNormal = -HelperChild.right;
                }

                planes[4] = new ValueTuple<Vector3, Plane>();
                planes[4].Item1 = currentCenter;
                planes[4].Item2 = GetPlaneFrom(newNormal, currentCenter);
                pointsForPlane.Clear();

                // Back
                pointsForPlane.Add(points[4]);
                pointsForPlane.Add(points[5]);
                pointsForPlane.Add(points[6]);
                pointsForPlane.Add(points[7]);
                currentCenter = Math3d.AverageVector(pointsForPlane.ToArray());
                newNormal = Center - currentCenter;
                if (newNormal.sqrMagnitude <= EPSILON)
                {
                    HelperParent.position = currentCenter;
                    HelperChild.rotation = Rotation;
                    newNormal = -HelperChild.forward;
                }

                planes[5] = new ValueTuple<Vector3, Plane>();
                planes[5].Item1 = currentCenter;
                planes[5].Item2 = GetPlaneFrom(newNormal, currentCenter);
                pointsForPlane.Clear();
                foreach (var item in planes)
                {
                    item.Item2.normal.Normalize();
                }
            }

            private Vector3[] GetActualPoints()
            {
                Vector3[] points;
                internalBoundsUpdate = true;
                Quaternion initial = Rotation;
                Rotation = Quaternion.identity;
                // Physics.SyncTransforms(); // Update collider bounds
                points = BoundsStandart.BoundsAsPoints();
                Rotation = initial;

                // Get the pivot!
                HelperParent.position = Position;
                for (int i = 0; i < points.Length; i++)
                {
                    HelperParent.rotation = Quaternion.identity;
                    HelperChild.SetPositionAndRotation(points[i], Quaternion.identity);
                    HelperChild.SetParent(HelperParent, true);
                    HelperParent.rotation = initial;
                    points[i] = HelperChild.position;
                }

                HelperChild.SetParent(null, true);
                return points;
            }

            private ExtendedBounds()
            {
            }

            public static ExtendedBounds ExtendedBoundsFactory(Transform target, Func<Bounds> GetCurrentBounds)
            {
                var boundsCurrent = GetCurrentBounds();
                return new BoundsExtensions.ExtendedBounds(boundsCurrent.size,
                    target.position,
                    target.rotation,
                    Vector3.one,
                    new BoundsExtensions.BoundsProviderSimple(target, GetCurrentBounds,
                        0, Vector3.up));
            }

            public ExtendedBounds(GameObject gameObject, IBoundsProvider boundsProvider)
            {
                operateOn = gameObject;
                provider = boundsProvider;
            }

            public ExtendedBounds(
                Vector3 size,
                Vector3 position,
                Quaternion initialrotation,
                Vector3 initialScale,
                IBoundsProvider boundsProvider)
            {
                operateOn = null;
                provider = boundsProvider;
                Position = position;
                Rotation = initialrotation;
                WorldScale = initialScale;
                actualSize = new Vector3(size.x, size.y, size.z);
                worldScaleBoundsStandart = initialScale;
                worldScaleLines = initialScale;
                worldScalePlanes = initialScale;
                worldScaleSize = initialScale;
                lastRotationBoundsStandart = initialrotation;
                lastRotationLines = initialrotation;
                lastRotationPlanes = initialrotation;
                lastRotationSize = initialrotation;
                worldPosSize = position;
            }

            public ExtendedBounds Copy()
            {
                var copyOf = new ExtendedBounds(operateOn, provider);
                copyOf.Position = Position;
                copyOf.Rotation = Rotation;
                copyOf.WorldScale = WorldScale;
                copyOf.boundsStandart = boundsStandart;
                copyOf.worldPosBoundsStandart = worldPosBoundsStandart;
                copyOf.lastRotationBoundsStandart = lastRotationBoundsStandart;
                copyOf.worldScaleBoundsStandart = worldScaleBoundsStandart;
                copyOf.actualSize = actualSize;
                return copyOf;
            }

            public bool Intersects(Bounds other)
            {
                var result = ActualBoundsWorld.Intersects(other);
                if (result)
                {
                    // check closely 
                    result = false;

                    var myPoint = GetFromLinePoints();
                    // TODO: check actual intersecting with PLane Plane / Plane Line 
                    
                    // check if inside
                    for (int i = 0; i < myPoint.Count && !result; i++)
                    {
                        result |= other.Contains(myPoint[i]);
                    }
                }

                return result;
            }

            public bool Intersects(ExtendedBounds other)
            {
                bool result = ActualBoundsWorld.Intersects(other.ActualBoundsWorld);
                if (result)
                {
                    // check closely 
                    result = false;

                    var myPoint = GetFromLinePoints();
                    var otherPoints = other.GetFromLinePoints();

                    // check PLane Plane intersection
                    Vector3 linePoint = Vector3.zero;
                    Vector3 lineVector = Vector3.zero;
                    Vector3 intersection = Vector3.zero;
                    int lengthOfPlanes = Planes.Length;
                    float maxdist = LINE_INTERCEPT_MAX_DISTANCE;
                    // check actual intersecting
                    for (int i = 0; i < lengthOfPlanes && !result; i++)
                    {
                        for (int j = 0; j < lengthOfPlanes && !result; j++)
                        {
                            if (Math3d.PlanePlaneIntersection(out linePoint, out lineVector,
                                    Planes[i].Item2.normal, Planes[i].Item1,
                                    other.Planes[j].Item2.normal, other.Planes[j].Item1
                                )
                                && CheckIfPointIsNearPlanes(linePoint, maxdist) &&
                                other.CheckIfPointIsNearPlanes(linePoint, maxdist))
                            {
                                for (int k = 0; k < LineSegments.Length && !result; k++)
                                {
                                    result |= Math3d.LineLineIntersection(out intersection, linePoint, lineVector,
                                        LineSegments[i].Item1,
                                        LineSegments[i].Item1 - LineSegments[i].Item2);
                                }
                            }
                        }
                    }

                    if (!result)
                    {
                        result = false;
                        // check if inside
                        int lengthOfPoints = myPoint.Count;
                        for (int i = 0; i < lengthOfPoints && !result; i++)
                        {
                            result |= other.ContainsPoint(myPoint[i]) || ContainsPoint(otherPoints[i]);
                        }
                    }
                }

                return result;
            }

            public bool ContainsPoint(Vector3 point)
            {
                bool resultIsInside = true;
                for (int i = 0; i < Planes.Length && resultIsInside; i++)
                {
                    resultIsInside &= Planes[i].Item2.GetSide(point);
                }

                return resultIsInside;
            }

            private bool CheckIfPointIsNearPlanes(Vector3 point, float epsilonDistance = 0)
            {
                bool resultIsInside = true;
                for (int i = 0; i < Planes.Length && resultIsInside; i++)
                {
                    resultIsInside &= Planes[i].Item2.GetSide(point) || InRangePlane(i, point, epsilonDistance);
                }

                return resultIsInside;
            }

            private bool InRangePlane(int indexFromPLanes, Vector3 pointToCheck, float small)
            {
                bool result = false;
                var distanceToPoint = Planes[indexFromPLanes].Item2.GetDistanceToPoint(pointToCheck);
                result = Mathf.Abs(distanceToPoint) <= small;
                return result;
            }


            private Vector3? previousGetActualWOrldAxis = null;
            private float actualSizeOnWorldAxis = 0;

            /// <summary>
            /// Always return the actual size of the object aligned on the axis
            /// </summary>
            /// <param name="axis">World Axis</param>
            /// <returns> size of one side </returns>
            public float GetActualSizeOnWorldAxis(Vector3 axis)
            {
                if (previousGetActualWOrldAxis.HasValue && previousGetActualWOrldAxis.Value == axis)
                {
                    return actualSizeOnWorldAxis;
                }

                previousGetActualWOrldAxis = axis;
                var fromPlanes = Planes;
                Bounds newOne = new Bounds(fromPlanes[0].Item1, Vector3.zero);
                foreach (var item in fromPlanes)
                {
                    newOne.Encapsulate(item.Item1);
                }

                var fromActualCenter = newOne.center;
                Vector3 finalIntersection = GetIntersectionOfLineAndPlane(fromActualCenter, axis);
                actualSizeOnWorldAxis = (finalIntersection - fromActualCenter).magnitude * 2;
                return actualSizeOnWorldAxis;
            }

            private Vector3 GetIntersectionOfLineAndPlane(Vector3 point, Vector3 lineAxis)
            {
                Vector3 intersectsOn = Vector3.zero;
                Vector3 finalIntersection = Vector3.zero;
                float minValue = float.MaxValue;
                float temp = 0;
                for (int i = 0; i < Planes.Length; i++)
                {
                    if (Math3d.LinePlaneIntersection(out intersectsOn, point, lineAxis, Planes[i].Item2.normal,
                        Planes[i].Item1))
                    {
                        temp = (intersectsOn - point).sqrMagnitude;
                        if (temp <= minValue)
                        {
                            minValue = temp;
                            finalIntersection = intersectsOn;
                        }
                    }
                }

                return finalIntersection;
            }

            public bool IntersectsAnyLine(ExtendedBounds another)
            {
                bool any = false;
                any |= IntersectsLinesTop(another);
                any |= IntersectsLinesBottom(another);
                any |= IntersectsLinesLeft(another);
                any |= IntersectsLinesRight(another);
                any |= IntersectsLinesFront(another);
                any |= IntersectsLinesBack(another);
                return any;
            }

            public bool IntersectsLinesTop(ExtendedBounds another)
            {
#if UNITY_EDITOR
                if (IntersectsLines(TopLines, another))
                {
                    Debug.LogWarning(" Intersects TopLines");
                }
#endif
                return IntersectsLines(TopLines, another);
            }

            public bool IntersectsLinesBottom(ExtendedBounds another)
            {
#if UNITY_EDITOR

                if (IntersectsLines(BottomLines, another))
                {
                    Debug.LogWarning(" Intersects BottomLines");
                }
#endif
                return IntersectsLines(BottomLines, another);
            }

            public bool IntersectsLinesLeft(ExtendedBounds another)
            {
#if UNITY_EDITOR

                if (IntersectsLines(LeftLines, another))
                {
                    Debug.LogWarning(" Intersects LeftLines");
                }
#endif
                return IntersectsLines(LeftLines, another);
            }

            public bool IntersectsLinesRight(ExtendedBounds another)
            {
#if UNITY_EDITOR

                if (IntersectsLines(RightLines, another))
                {
                    Debug.LogWarning(" Intersects RightLines");
                }
#endif
                return IntersectsLines(RightLines, another);
            }

            public bool IntersectsLinesFront(ExtendedBounds another)
            {
#if UNITY_EDITOR

                if (IntersectsLines(FrontLines, another))
                {
                    Debug.LogWarning(" Intersects FrontLines");
                }
#endif
                return IntersectsLines(FrontLines, another);
            }

            public bool IntersectsLinesBack(ExtendedBounds another)
            {
#if UNITY_EDITOR

                if (IntersectsLines(BackLines, another))
                {
                    Debug.LogWarning(" Intersects BackLines");
                }
#endif
                return IntersectsLines(BackLines, another);
            }

            private bool IntersectsLines(int[] arrayOfIndices, ExtendedBounds another)
            {
                bool result = false;
                ValueTuple<Vector3, Vector3> lineSegment;
                for (int i = 0; i < arrayOfIndices.Length && !result; i++)
                {
                    lineSegment = LineSegments[arrayOfIndices[i]];
                    for (int j = 0; j < another.LineSegments.Length && !result; j++)
                    {
                        result |= Math3d.LineLineIntersection(out _, lineSegment.Item1, lineSegment.Item2,
                            another.LineSegments[j].Item1, another.LineSegments[j].Item2);
                    }
                }

                return result;
            }

            private List<Vector3> GetFromLinePoints()
            {
                List<Vector3> points = new List<Vector3>();
                points.Add(LineSegments[0].Item1); // Front top left  
                points.Add(LineSegments[0].Item2); // Front top right
                points.Add(LineSegments[3].Item1); // Front bottom left 
                points.Add(LineSegments[3].Item2); // Front bottom right
                points.Add(LineSegments[4].Item1); // Back top left 
                points.Add(LineSegments[4].Item2); // Back top right
                points.Add(LineSegments[7].Item1); // Back bottom left 
                points.Add(LineSegments[7].Item2); // Back bottom right
                return points;
            }

            public void Dispose()
            {
                if (!disposed)
                {
                    disposed = true;
                    Destroy();
                }
            }

            private void Destroy()
            {
            }
        }
    }
}