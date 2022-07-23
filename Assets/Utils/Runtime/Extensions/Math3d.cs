using UnityEngine;
using System.Collections;
using System;

public static class Math3d
{
    private static Transform tempChild = null;
    private static Transform tempParent = null;

    public static void Init()
    {
        tempChild = (new GameObject("Math3d_TempChild")).transform;
        tempParent = (new GameObject("Math3d_TempParent")).transform;

        tempChild.gameObject.hideFlags = HideFlags.HideAndDontSave;
        GameObjectExtensions.DontDestroyOnLoad(tempChild.gameObject);

        tempParent.gameObject.hideFlags = HideFlags.HideAndDontSave;
        GameObjectExtensions.DontDestroyOnLoad(tempParent.gameObject);

        //set the parent
        tempChild.parent = tempParent;
    }


    //increase or decrease the length of vector by size
    public static Vector3 AddVectorLength(Vector3 vector, float size)
    {
        //get the vector length
        float magnitude = Vector3.Magnitude(vector);

        //change the length
        magnitude += size;

        //normalize the vector
        Vector3 vectorNormalized = Vector3.Normalize(vector);

        //scale the vector
        return Vector3.Scale(vectorNormalized, new Vector3(magnitude, magnitude, magnitude));
    }

    //create a vector of direction "vector" with length "size"
    public static Vector3 SetVectorLength(Vector3 vector, float size)
    {
        //normalize the vector
        Vector3 vectorNormalized = Vector3.Normalize(vector);

        //scale the vector
        return vectorNormalized *= size;
    }


    //calculate the rotational difference from A to B
    public static Quaternion SubtractRotation(Quaternion B, Quaternion A)
    {
        Quaternion C = Quaternion.Inverse(A) * B;
        return C;
    }

    //Find the line of intersection between two planes. The planes are defined by a normal and a point on that plane.
    //The outputs are a point on the line and a vector which indicates it's direction. If the planes are not parallel, 
    //the function outputs true, otherwise false.
    public static bool PlanePlaneIntersection(out Vector3 linePoint, out Vector3 lineVec, Vector3 plane1Normal,
        Vector3 plane1Position, Vector3 plane2Normal, Vector3 plane2Position)
    {
        linePoint = Vector3.zero;
        lineVec = Vector3.zero;

        //We can get the direction of the line of intersection of the two planes by calculating the 
        //cross product of the normals of the two planes. Note that this is just a direction and the line
        //is not fixed in space yet. We need a point for that to go with the line vector.
        lineVec = Vector3.Cross(plane1Normal, plane2Normal);

        //Next is to calculate a point on the line to fix it's position in space. This is done by finding a vector from
        //the plane2 location, moving parallel to it's plane, and intersecting plane1. To prevent rounding
        //errors, this vector also has to be perpendicular to lineDirection. To get this vector, calculate
        //the cross product of the normal of plane2 and the lineDirection.      
        Vector3 lineDir = Vector3.Cross(plane2Normal, lineVec);

        float denominator = Vector3.Dot(plane1Normal, lineDir);

        //Prevent divide by zero and rounding errors by requiring about 5 degrees angle between the planes.
        if (Mathf.Abs(denominator) > 0.006f)
        {
            Vector3 plane1ToPlane2 = plane1Position - plane2Position;
            float t = Vector3.Dot(plane1Normal, plane1ToPlane2) / denominator;
            linePoint = plane2Position + t * lineDir;

            return true;
        }

        //output not valid
        else
        {
            return false;
        }
    }

    //Get the intersection between a line and a plane. 
    //If the line and plane are not parallel, the function outputs true, otherwise false.
    public static bool LinePlaneIntersection(out Vector3 intersection, Vector3 linePoint, Vector3 lineVec,
        Vector3 planeNormal, Vector3 planePoint)
    {
        float length;
        float dotNumerator;
        float dotDenominator;
        Vector3 vector;
        intersection = Vector3.zero;

        //calculate the distance between the linePoint and the line-plane intersection point
        dotNumerator = Vector3.Dot((planePoint - linePoint), planeNormal);
        dotDenominator = Vector3.Dot(lineVec, planeNormal);

        //line and plane are not parallel
        if (dotDenominator != 0.0f)
        {
            length = dotNumerator / dotDenominator;

            //create a vector from the linePoint to the intersection point
            vector = SetVectorLength(lineVec, length);

            //get the coordinates of the line-plane intersection point
            intersection = linePoint + vector;

            return true;
        }

        //output not valid
        else
        {
            return false;
        }
    }

    //Calculate the intersection point of two lines. Returns true if lines intersect, otherwise false.
    //Note that in 3d, two lines do not intersect most of the time. So if the two lines are not in the 
    //same plane, use ClosestPointsOnTwoLines() instead.
    public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1,
        Vector3 linePoint2, Vector3 lineVec2)
    {
        intersection = Vector3.zero;

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //Lines are not coplanar. Take into account rounding errors.
        if ((planarFactor >= 0.00001f) || (planarFactor <= -0.00001f))
        {
            return false;
        }

        //Note: sqrMagnitude does x*x+y*y+z*z on the input vector.
        float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;

        if ((s >= 0.0f) && (s <= 1.0f))
        {
            intersection = linePoint1 + (lineVec1 * s);
            return true;
        }

        else
        {
            return false;
        }
    }

    //Two non-parallel lines which may or may not touch each other have a point on each line which are closest
    //to each other. This function finds those two points. If the lines are not parallel, the function 
    //outputs true, otherwise false.
    public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2,
        Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {
        closestPointLine1 = Vector3.zero;
        closestPointLine2 = Vector3.zero;

        float a = Vector3.Dot(lineVec1, lineVec1);
        float b = Vector3.Dot(lineVec1, lineVec2);
        float e = Vector3.Dot(lineVec2, lineVec2);

        float d = a * e - b * b;

        //lines are not parallel
        if (d != 0.0f)
        {
            Vector3 r = linePoint1 - linePoint2;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);

            float s = (b * f - c * e) / d;
            float t = (a * f - c * b) / d;

            closestPointLine1 = linePoint1 + lineVec1 * s;
            closestPointLine2 = linePoint2 + lineVec2 * t;

            return true;
        }

        else
        {
            return false;
        }
    }

    //Changes the sign of the quaternion components. This is not the same as the inverse.
    public static Quaternion InverseSignQuaternion(Quaternion q)
    {
        return new Quaternion(-q.x, -q.y, -q.z, -q.w);
    }

    //Returns true if the two input quaternions are close to each other. This can
    //be used to check whether or not one of two quaternions which are supposed to
    //be very similar but has its component signs reversed (q has the same rotation as
    //-q)
    public static bool AreQuaternionsClose(Quaternion q1, Quaternion q2)
    {
        float dot = Quaternion.Dot(q1, q2);
        if (dot < 0.0f)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    //This function returns a point which is a projection from a point to a line.
    //The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
    public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point)
    {
        //get vector from point on line to point in space
        Vector3 linePointToPoint = point - linePoint;

        float t = Vector3.Dot(linePointToPoint, lineVec);

        return linePoint + lineVec * t;
    }

    //This function returns a point which is a projection from a point to a line segment.
    //If the projected point lies outside of the line segment, the projected point will 
    //be clamped to the appropriate line edge.
    //If the line is infinite instead of a segment, use ProjectPointOnLine() instead.
    public static Vector3 ProjectPointOnLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
    {
        Vector3 vector = linePoint2 - linePoint1;

        Vector3 projectedPoint = ProjectPointOnLine(linePoint1, vector.normalized, point);

        int side = PointOnWhichSideOfLineSegment(linePoint1, linePoint2, projectedPoint);

        //The projected point is on the line segment
        if (side == 0)
        {
            return projectedPoint;
        }

        if (side == 1)
        {
            return linePoint1;
        }

        if (side == 2)
        {
            return linePoint2;
        }

        //output is invalid
        return Vector3.zero;
    }

    //This function returns a point which is a projection from a point to a plane.
    public static Vector3 ProjectPointOnPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
    {
        float distance;
        Vector3 translationVector;

        //First calculate the distance from the point to the plane:
        distance = SignedDistancePlanePoint(planeNormal, planePoint, point);

        //Reverse the sign of the distance
        distance *= -1;

        //Get a translation vector
        translationVector = SetVectorLength(planeNormal, distance);

        //Translate the point to form a projection
        return point + translationVector;
    }

    //Projects a vector onto a plane. The output is not normalized.
    public static Vector3 ProjectVectorOnPlane(Vector3 planeNormal, Vector3 vector)
    {
        return vector - (Vector3.Dot(vector, planeNormal) * planeNormal);
    }

    //Get the shortest distance between a point and a plane. The output is signed so it holds information
    //as to which side of the plane normal the point is.
    public static float SignedDistancePlanePoint(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
    {
        return Vector3.Dot(planeNormal, (point - planePoint));
    }

    //This function calculates a signed (+ or - sign instead of being ambiguous) dot product. It is basically used
    //to figure out whether a vector is positioned to the left or right of another vector. The way this is done is
    //by calculating a vector perpendicular to one of the vectors and using that as a reference. This is because
    //the result of a dot product only has signed information when an angle is transitioning between more or less
    //then 90 degrees.
    public static float SignedDotProduct(Vector3 vectorA, Vector3 vectorB, Vector3 normal)
    {
        Vector3 perpendicularVector;
        float dot;

        //Use the geometry object normal and one of the input vectors to calculate the perpendicular vector
        perpendicularVector = Vector3.Cross(normal, vectorA);

        //Now calculate the dot product between the perpendicular vector (perpendicularVector) and the other input vector
        dot = Vector3.Dot(perpendicularVector, vectorB);

        return dot;
    }

    /// <summary>
    /// Calculate the angle between a vector and a plane. The plane is made by a normal vector.
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="normal"></param>
    /// <returns></returns>
    public static float AngleVectorPlane(Vector3 vector, Vector3 normal)
    {
        float dot;
        float angle;

        //calculate the the dot product between the two input vectors. This gives the cosine between the two vectors
        dot = Vector3.Dot(vector, normal);

        //this is in radians
        angle = (float) Math.Acos(dot);

        return (1.570796326794897f - angle) * Mathf.Rad2Deg; //90 degrees - angle
    }

    //Calculate the dot product as an angle
    public static float DotProductAngle(Vector3 vec1, Vector3 vec2)
    {
        double dot;
        double angle;

        //get the dot product
        dot = Vector3.Dot(vec1, vec2);

        //Clamp to prevent NaN error. Shouldn't need this in the first place, but there could be a rounding error issue.
        if (dot < -1.0f)
        {
            dot = -1.0f;
        }

        if (dot > 1.0f)
        {
            dot = 1.0f;
        }

        //Calculate the angle. The output is in radians
        //This step can be skipped for optimization...
        angle = Math.Acos(dot);

        return (float) angle;
    }

    /// <summary>
    /// Angle From -180, 180
    /// </summary>
    /// <param name="vec1"></param>
    /// <param name="vec2"></param>
    /// <returns></returns>
    public static float AngleBetweenVector2(Vector2 vec1, Vector2 vec2)
    {
        Vector2 vec1Rotated90 = new Vector2(-vec1.y, vec1.x);
        float sign = (Vector2.Dot(vec1Rotated90, vec2) < 0) ? -1.0f : 1.0f;
        return Vector2.Angle(vec1, vec2) * sign;
    }

    //Convert a plane defined by 3 points to a plane defined by a vector and a point. 
    //The plane point is the middle of the triangle defined by the 3 points.
    public static void PlaneFrom3Points(out Vector3 planeNormal, out Vector3 planePoint, Vector3 pointA, Vector3 pointB,
        Vector3 pointC)
    {
        planeNormal = Vector3.zero;
        planePoint = Vector3.zero;

        //Make two vectors from the 3 input points, originating from point A
        Vector3 AB = pointB - pointA;
        Vector3 AC = pointC - pointA;

        //Calculate the normal
        planeNormal = Vector3.Normalize(Vector3.Cross(AB, AC));

        //Get the points in the middle AB and AC
        Vector3 middleAB = pointA + (AB / 2.0f);
        Vector3 middleAC = pointA + (AC / 2.0f);

        //Get vectors from the middle of AB and AC to the point which is not on that line.
        Vector3 middleABtoC = pointC - middleAB;
        Vector3 middleACtoB = pointB - middleAC;

        //Calculate the intersection between the two lines. This will be the center 
        //of the triangle defined by the 3 points.
        //We could use LineLineIntersection instead of ClosestPointsOnTwoLines but due to rounding errors 
        //this sometimes doesn't work.
        Vector3 temp;
        ClosestPointsOnTwoLines(out planePoint, out temp, middleAB, middleABtoC, middleAC, middleACtoB);
    }

    //Returns the forward vector of a quaternion
    public static Vector3 GetForwardVector(Quaternion q)
    {
        return q * Vector3.forward;
    }

    //Returns the up vector of a quaternion
    public static Vector3 GetUpVector(Quaternion q)
    {
        return q * Vector3.up;
    }

    //Returns the right vector of a quaternion
    public static Vector3 GetRightVector(Quaternion q)
    {
        return q * Vector3.right;
    }

    //Gets a quaternion from a matrix
    public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }

    //Gets a position from a matrix
    public static Vector3 PositionFromMatrix(Matrix4x4 m)
    {
        Vector4 vector4Position = m.GetColumn(3);
        return new Vector3(vector4Position.x, vector4Position.y, vector4Position.z);
    }

    //This is an alternative for Quaternion.LookRotation. Instead of aligning the forward and up vector of the game 
    //object with the input vectors, a custom direction can be used instead of the fixed forward and up vectors.
    //alignWithVector and alignWithNormal are in world space.
    //customForward and customUp are in object space.
    //Usage: use alignWithVector and alignWithNormal as if you are using the default LookRotation function.
    //Set customForward and customUp to the vectors you wish to use instead of the default forward and up vectors.
    public static void LookRotationExtended(ref GameObject gameObjectInOut, Vector3 alignWithVector,
        Vector3 alignWithNormal, Vector3 customForward, Vector3 customUp)
    {
        //Set the rotation of the destination
        Quaternion rotationA = Quaternion.LookRotation(alignWithVector, alignWithNormal);

        //Set the rotation of the custom normal and up vectors. 
        //When using the default LookRotation function, this would be hard coded to the forward and up vector.
        Quaternion rotationB = Quaternion.LookRotation(customForward, customUp);

        //Calculate the rotation
        gameObjectInOut.transform.rotation = rotationA * Quaternion.Inverse(rotationB);
    }

    //This function transforms one object as if it was parented to the other.
    //Before using this function, the Init() function must be called
    //Input: parentRotation and parentPosition: the current parent transform.
    //Input: startParentRotation and startParentPosition: the transform of the parent object at the time the objects are parented.
    //Input: startChildRotation and startChildPosition: the transform of the child object at the time the objects are parented.
    //Output: childRotation and childPosition.
    //All transforms are in world space.
    public static void TransformWithParent(out Quaternion childRotation, out Vector3 childPosition,
        Quaternion parentRotation, Vector3 parentPosition, Quaternion startParentRotation, Vector3 startParentPosition,
        Quaternion startChildRotation, Vector3 startChildPosition)
    {
        childRotation = Quaternion.identity;
        childPosition = Vector3.zero;

        //set the parent start transform
        tempParent.SetPositionAndRotation(startParentPosition, startParentRotation);
        tempParent.localScale = Vector3.one; //to prevent scale wandering

        //set the child start transform
        tempChild.SetPositionAndRotation(startChildPosition, startChildRotation);
        tempChild.localScale = Vector3.one; //to prevent scale wandering

        //translate and rotate the child by moving the parent
        tempParent.SetPositionAndRotation(parentPosition, parentRotation);

        //get the child transform
        childRotation = tempChild.rotation;
        childPosition = tempChild.position;
    }

    //With this function you can align a triangle of an object with any transform.
    //Usage: gameObjectInOut is the game object you want to transform.
    //alignWithVector, alignWithNormal, and alignWithPosition is the transform with which the triangle of the object should be aligned with.
    //triangleForward, triangleNormal, and trianglePosition is the transform of the triangle from the object.
    //alignWithVector, alignWithNormal, and alignWithPosition are in world space.
    //triangleForward, triangleNormal, and trianglePosition are in object space.
    //trianglePosition is the mesh position of the triangle. The effect of the scale of the object is handled automatically.
    //trianglePosition can be set at any position, it does not have to be at a vertex or in the middle of the triangle.
    public static void PreciseAlign(ref GameObject gameObjectInOut, Vector3 alignWithVector, Vector3 alignWithNormal,
        Vector3 alignWithPosition, Vector3 triangleForward, Vector3 triangleNormal, Vector3 trianglePosition)
    {
        //Set the rotation.
        LookRotationExtended(ref gameObjectInOut, alignWithVector, alignWithNormal, triangleForward, triangleNormal);

        //Get the world space position of trianglePosition
        Vector3 trianglePositionWorld = gameObjectInOut.transform.TransformPoint(trianglePosition);

        //Get a vector from trianglePosition to alignWithPosition
        Vector3 translateVector = alignWithPosition - trianglePositionWorld;

        //Now transform the object so the triangle lines up correctly.
        gameObjectInOut.transform.Translate(translateVector, Space.World);
    }


    //Convert a position, direction, and normal vector to a transform
    private static void VectorsToTransform(ref GameObject gameObjectInOut, Vector3 positionVector,
        Vector3 directionVector, Vector3 normalVector)
    {
        gameObjectInOut.transform.SetPositionAndRotation(positionVector, Quaternion.LookRotation(directionVector, normalVector));
    }

    //This function finds out on which side of a line segment the point is located.
    //The point is assumed to be on a line created by linePoint1 and linePoint2. If the point is not on
    //the line segment, project it on the line using ProjectPointOnLine() first.
    //Returns 0 if point is on the line segment.
    //Returns 1 if point is outside of the line segment and located on the side of linePoint1.
    //Returns 2 if point is outside of the line segment and located on the side of linePoint2.
    public static int PointOnWhichSideOfLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
    {
        Vector3 lineVec = linePoint2 - linePoint1;
        Vector3 pointVec = point - linePoint1;

        float dot = Vector3.Dot(pointVec, lineVec);

        //point is on side of linePoint2, compared to linePoint1
        if (dot > 0)
        {
            //point is on the line segment
            if (pointVec.magnitude <= lineVec.magnitude)
            {
                return 0;
            }

            //point is not on the line segment and it is on the side of linePoint2
            else
            {
                return 2;
            }
        }

        //Point is not on side of linePoint2, compared to linePoint1.
        //Point is not on the line segment and it is on the side of linePoint1.
        else
        {
            return 1;
        }
    }


    //Returns the pixel distance from the mouse pointer to a line.
    //Alternative for HandleUtility.DistanceToLine(). Works both in Editor mode and Play mode.
    //Do not call this function from OnGUI() as the mouse position will be wrong.
    public static float MouseDistanceToLine(Vector3 linePoint1, Vector3 linePoint2)
    {
        Camera currentCamera;
        Vector3 mousePosition;

#if UNITY_EDITOR
        if (Camera.current != null)
        {
            currentCamera = Camera.current;
        }

        else
        {
            currentCamera = Camera.main;
        }

        //convert format because y is flipped
        mousePosition = new Vector3(Event.current.mousePosition.x,
            currentCamera.pixelHeight - Event.current.mousePosition.y, 0f);

#else
        currentCamera = Camera.main;
        mousePosition = Input.mousePosition;
#endif

        Vector3 screenPos1 = currentCamera.WorldToScreenPoint(linePoint1);
        Vector3 screenPos2 = currentCamera.WorldToScreenPoint(linePoint2);
        Vector3 projectedPoint = ProjectPointOnLineSegment(screenPos1, screenPos2, mousePosition);

        //set z to zero
        projectedPoint = new Vector3(projectedPoint.x, projectedPoint.y, 0f);

        Vector3 vector = projectedPoint - mousePosition;
        return vector.magnitude;
    }


    //Returns the pixel distance from the mouse pointer to a camera facing circle.
    //Alternative for HandleUtility.DistanceToCircle(). Works both in Editor mode and Play mode.
    //Do not call this function from OnGUI() as the mouse position will be wrong.
    //If you want the distance to a point instead of a circle, set the radius to 0.
    public static float MouseDistanceToCircle(Vector3 point, float radius)
    {
        Camera currentCamera;
        Vector3 mousePosition;

#if UNITY_EDITOR
        if (Camera.current != null)
        {
            currentCamera = Camera.current;
        }

        else
        {
            currentCamera = Camera.main;
        }

        //convert format because y is flipped
        mousePosition = new Vector3(Event.current.mousePosition.x,
            currentCamera.pixelHeight - Event.current.mousePosition.y, 0f);
#else
        currentCamera = Camera.main;
        mousePosition = Input.mousePosition;
#endif

        Vector3 screenPos = currentCamera.WorldToScreenPoint(point);

        //set z to zero
        screenPos = new Vector3(screenPos.x, screenPos.y, 0f);

        Vector3 vector = screenPos - mousePosition;
        float fullDistance = vector.magnitude;
        float circleDistance = fullDistance - radius;

        return circleDistance;
    }

    //Returns true if a line segment (made up of linePoint1 and linePoint2) is fully or partially in a rectangle
    //made up of RectA to RectD. The line segment is assumed to be on the same plane as the rectangle. If the line is 
    //not on the plane, use ProjectPointOnPlane() on linePoint1 and linePoint2 first.
    public static bool IsLineInRectangle(Vector3 linePoint1, Vector3 linePoint2, Vector3 rectA, Vector3 rectB,
        Vector3 rectC, Vector3 rectD)
    {
        bool pointAInside = false;
        bool pointBInside = false;

        pointAInside = IsPointInRectangle(linePoint1, rectA, rectC, rectB, rectD);

        if (!pointAInside)
        {
            pointBInside = IsPointInRectangle(linePoint2, rectA, rectC, rectB, rectD);
        }

        //none of the points are inside, so check if a line is crossing
        if (!pointAInside && !pointBInside)
        {
            bool lineACrossing = AreLineSegmentsCrossing(linePoint1, linePoint2, rectA, rectB);
            bool lineBCrossing = AreLineSegmentsCrossing(linePoint1, linePoint2, rectB, rectC);
            bool lineCCrossing = AreLineSegmentsCrossing(linePoint1, linePoint2, rectC, rectD);
            bool lineDCrossing = AreLineSegmentsCrossing(linePoint1, linePoint2, rectD, rectA);

            if (lineACrossing || lineBCrossing || lineCCrossing || lineDCrossing)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return true;
        }
    }

    //Returns true if "point" is in a rectangle mad up of RectA to RectD. The line point is assumed to be on the same 
    //plane as the rectangle. If the point is not on the plane, use ProjectPointOnPlane() first.
    public static bool IsPointInRectangle(Vector3 point, Vector3 rectA, Vector3 rectC, Vector3 rectB, Vector3 rectD)
    {
        Vector3 vector;
        Vector3 linePoint;

        //get the center of the rectangle
        vector = rectC - rectA;
        float size = -(vector.magnitude / 2f);
        vector = AddVectorLength(vector, size);
        Vector3 middle = rectA + vector;

        Vector3 xVector = rectB - rectA;
        float width = xVector.magnitude / 2f;

        Vector3 yVector = rectD - rectA;
        float height = yVector.magnitude / 2f;

        linePoint = ProjectPointOnLine(middle, xVector.normalized, point);
        vector = linePoint - point;
        float yDistance = vector.magnitude;

        linePoint = ProjectPointOnLine(middle, yVector.normalized, point);
        vector = linePoint - point;
        float xDistance = vector.magnitude;

        if ((xDistance <= width) && (yDistance <= height))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //Returns true if line segment made up of pointA1 and pointA2 is crossing line segment made up of
    //pointB1 and pointB2. The two lines are assumed to be in the same plane.
    public static bool AreLineSegmentsCrossing(Vector3 pointA1, Vector3 pointA2, Vector3 pointB1, Vector3 pointB2)
    {
        Vector3 closestPointA;
        Vector3 closestPointB;
        int sideA;
        int sideB;

        Vector3 lineVecA = pointA2 - pointA1;
        Vector3 lineVecB = pointB2 - pointB1;

        bool valid = ClosestPointsOnTwoLines(out closestPointA, out closestPointB, pointA1, lineVecA.normalized,
            pointB1, lineVecB.normalized);

        //lines are not parallel
        if (valid)
        {
            sideA = PointOnWhichSideOfLineSegment(pointA1, pointA2, closestPointA);
            sideB = PointOnWhichSideOfLineSegment(pointB1, pointB2, closestPointB);

            if ((sideA == 0) && (sideB == 0))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }


    //Get an average (mean) from more then two quaternions (with two, slerp would be used).
    //Note: this only works if all the quaternions are relatively close together.
    //Usage: 
    //-Cumulative is an external Vector4 which holds all the added x y z and w components.
    //-newRotation is the next rotation to be added to the average pool
    //-firstRotation is the first quaternion of the array to be averaged
    //-addAmount holds the total amount of quaternions which are currently added
    //This function returns the current average quaternion
    public static void AverageQuaternion(ref Vector4 cumulative, Quaternion newRotation, Quaternion firstRotation,
        int addAmount)
    {
        float w = 0.0f;
        float x = 0.0f;
        float y = 0.0f;
        float z = 0.0f;

        //Before we add the new rotation to the average (mean), we have to check whether the quaternion has to be inverted. Because
        //q and -q are the same rotation, but cannot be averaged, we have to make sure they are all the same.
        if (!Math3d.AreQuaternionsClose(newRotation, firstRotation))
        {
            newRotation = Math3d.InverseSignQuaternion(newRotation);
        }

        //Average the values
        float addDet = 1f / (float) addAmount;
        cumulative.w += newRotation.w;
        w = cumulative.w * addDet;
        cumulative.x += newRotation.x;
        x = cumulative.x * addDet;
        cumulative.y += newRotation.y;
        y = cumulative.y * addDet;
        cumulative.z += newRotation.z;
        z = cumulative.z * addDet;

        //note: if speed is an issue, you can skip the normalization step
        //return NormalizeQuaternion(x, y, z, w);
    }

    public static Vector3 AverageVector(Vector3[] arrayOfPoints)
    {
        Bounds temporal = new Bounds(arrayOfPoints[0], Vector3.zero);
        foreach (var item in arrayOfPoints)
        {
            temporal.Encapsulate(item);
        }

        return temporal.center;
    }

    public static Quaternion NormalizeQuaternion(float x, float y, float z, float w)
    {
        float lengthD = 1.0f / (w * w + x * x + y * y + z * z);
        w *= lengthD;
        x *= lengthD;
        y *= lengthD;
        z *= lengthD;
        return new Quaternion(x, y, z, w);
    }

    //Usage: 
    //-position current world position
    //-rotation current world rotation
    //-center (pass in 0 if not needed)
    //-axis world position to rotate around! - As if the object orbits the axis!
    //-angle angle to orbit around
    // returns Tuple of new World Position and new World Rotation
    public static System.ValueTuple<Vector3, Quaternion> RotateAround(Vector3 position, Quaternion rotation,
        Vector3 center, Vector3 axis, float angle)
    {
        var pos = position;
        // axis are world position to rotate around! - As if the object orbits the axis!
        var rot = Quaternion.AngleAxis(angle, axis); // get the desired rotation
        var dir = pos - center; // find current direction relative to center
        dir = rot * dir; // rotate the direction
        position = center + dir; // define new position
        // rotate object to keep looking at the center:
        var myRot = rotation;
        rotation *= Quaternion.Inverse(myRot) * rot * myRot;

        return new System.ValueTuple<Vector3, Quaternion>(position, rotation);
    }

    public static System.ValueTuple<Vector3, Quaternion> RotateAround(Vector3 position, Quaternion rotation,
        Vector3 center, Quaternion targetRotation)
    {
        Vector3 Axis;
        float angleAxis = 0;
        targetRotation.ToAngleAxis(out angleAxis, out Axis);
        return RotateAround(position, rotation, center, Axis, angleAxis);
    }

    // Blue Axis. Don't forget to multiple by the Rotation!
    public static Quaternion RotateToFaceOnY(Transform toUse, Vector3 WorldPos)
    {
        Quaternion result;
        Vector3 relative = toUse.InverseTransformPoint(WorldPos);
        //  Y
        float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
        result = Quaternion.Euler(0, angle, 0);

        return result;
    }

    // Green Axis. Don't forget to multiple by the Rotation!
    public static Quaternion RotateToFaceOnX(Transform toUse, Vector3 WorldPos)
    {
        Quaternion result;
        Vector3 relative = toUse.InverseTransformPoint(WorldPos);
        //  X
        float angle = Mathf.Atan2(relative.z, relative.y) * Mathf.Rad2Deg;
        result = Quaternion.Euler(angle, 0, 0);

        return result;
    }

    // Red Axis. Don't forget to multiple by the Rotation!
    public static Quaternion RotateToFaceOnZ(Transform toUse, Vector3 WorldPos)
    {
        Quaternion result;
        Vector3 relative = toUse.InverseTransformPoint(WorldPos);
        // Z
        float angle = Mathf.Atan2(relative.y, relative.x) * Mathf.Rad2Deg;
        result = Quaternion.Euler(0, 0, angle);

        return result;
    }

    public static Vector3 GetDownFrom(Transform getFrom)
    {
        return Quaternion.Euler(180, 0, 0) * getFrom.up;
    }

    /// <summary>
    ///   <para>PingPongs the value t, so that it is never larger than length and never smaller than 0.</para>
    /// </summary>
    /// <param name="t"></param>
    /// <param name="length"></param>
    public static float PingPong(float t, float length)
    {
        t = Mathf.Repeat(t, length * 2f);
        return length - Mathf.Abs(t - length);
    }

    /// <summary>
    /// Gets the angle between <see cref="Vector3"/> a and b when the z value of a and b are zero.
    /// </summary>
    /// <param name="a">The <see cref="Vector3"/> a.</param>
    /// <param name="b">The <see cref="Vector3"/> b.</param>
    /// <returns>The angle between <see cref="Vector3"/> a and b.</returns>
    public static float Get2DAngle(Vector3 a, Vector3 b)
    {
        var isRight = Vector3.Cross(a, b).z < 0;
        var angle = Vector3.Angle(a, b);

        if (isRight)
            angle = -angle;

        return angle;
    }

    /// <summary>
    /// Gets the angle between <see cref="Vector3"/> a and b.
    /// </summary>
    /// <param name="a">The <see cref="Vector3"/> a.</param>
    /// <param name="b">The <see cref="Vector3"/> b.</param>
    /// <returns>The angle between <see cref="Vector3"/> a and b.</returns>
    public static float GetAngle(Vector3 a, Vector3 b)
    {
        return Mathf.Acos(Vector3.Dot(a.normalized, b.normalized)) * Mathf.Rad2Deg;
    }


    // swing-twist decomposition & interpolation
    // get the angle around the given axis
    // ------------------------------------------------------------------------
    // get angle around the arbitrary axis!
    // swing is going from up/down - not the same as twist!
    // twist is how mush of a degrees is around the given axis!
    // works only in range 0-180
    public static void DecomposeSwingTwist(Quaternion inRotation, Vector3 twistAxis, out Quaternion swingPivot,
        out Quaternion angleAroundAxis)
    {
        Vector3 rotationVector =
            new Vector3(inRotation.x, inRotation.y, inRotation.z); // (rotation axis) * cos(angle / 2)


        // singularity: rotation by 180 degree
        var Epsilon = 1.0e-9f; // -9
        if (rotationVector.sqrMagnitude < Epsilon)
        {
            Vector3 rotatedTwistAxis = inRotation * twistAxis;
            Vector3 swingAxis = Vector3.Cross(twistAxis, rotatedTwistAxis);

            if (swingAxis.sqrMagnitude > Epsilon)
            {
                float swingAngle = Vector3.Angle(twistAxis, rotatedTwistAxis);
                swingPivot = Quaternion.AngleAxis(swingAngle, swingAxis);
            }
            else
            {
                // more singularity: rotation axis parallel to twist axis
                swingPivot = Quaternion.identity; // no swing
            }

            // always twist 180 degree on singularity
            angleAroundAxis = Quaternion.AngleAxis(180.0f, twistAxis);
            return;
        }

        // formula & proof: 
        // http://www.euclideanspace.com/maths/geometry/rotations/for/decomposition/
        Vector3 p = Vector3.Project(rotationVector, twistAxis);
        // possible to fix with check if more than 180 - rotate -360
        angleAroundAxis = new Quaternion(p.x, p.y, p.z, inRotation.w);
        angleAroundAxis = Normalize(angleAroundAxis);
        swingPivot = inRotation * Quaternion.Inverse(angleAroundAxis);
        // rotatedAround always a small inaccuracy in one radian - 2,67324
    }

    private static Vector3 NormalizeNegatives(Vector3 inVector)
    {
        return new Vector3(
            inVector.x < 0 ? 360 - inVector.x : inVector.x,
            inVector.y < 0 ? 360 - inVector.y : inVector.y,
            inVector.z < 0 ? 360 - inVector.z : inVector.z
        );
    }

    private static Quaternion Normalize(Quaternion q)
    {
        float magInv = 1.0f / Magnitude(q);
        return new Quaternion(magInv * q.x, magInv * q.y, magInv * q.z, magInv * q.w);
    }

    private static float Magnitude(Quaternion q)
    {
        return Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
    }
    
    /// <summary>
    /// Remap values from input range to target range
    /// </summary>
    /// <param name="value"> current value </param>
    /// <param name="rangemin1"> from range begins</param>
    /// <param name="rangemax1"> from range begins</param>
    /// <param name="rangemin2"> target range ends</param>
    /// <param name="rangemax2">target range ends</param>
    /// <returns></returns>
    public static float Remap(float value, float rangemin1, float rangemax1, float rangemin2, float rangemax2)
    {
        return (value - rangemin1) / (rangemax1 - rangemin1) * (rangemax2 - rangemin2) + rangemin2;
    }
}