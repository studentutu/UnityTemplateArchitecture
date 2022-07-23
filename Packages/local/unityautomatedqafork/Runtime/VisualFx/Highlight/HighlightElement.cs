using System;
using UnityEngine;

// TODO: When linerender bug is fixed, implement (https://fogbugz.unity3d.com/f/cases/1331916/)
public class HighlightElement : MonoBehaviour
{
    public void Init(GameObject target)
    {

        // Take 4 points around an object's dimensions (with padding) and draw a rectangle with LineRenderer.
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 5;
        lineRenderer.startWidth = lineRenderer.endWidth = 0.04f;

        float halfWidth = target.GetComponent<RectTransform>().sizeDelta.x / 2f;
        float halfHeight = target.GetComponent<RectTransform>().sizeDelta.y / 2f;
        float padding = 10f;
        Vector3 topLeft, topRight, bottomLeft, bottomRight, lineCompletor;
        topLeft = Camera.main.ScreenToWorldPoint(new Vector3(target.transform.position.x - halfWidth - padding, target.transform.position.y + halfHeight + padding, 0));
        topRight = Camera.main.ScreenToWorldPoint(new Vector3(target.transform.position.x + halfWidth + padding, target.transform.position.y + halfHeight + padding, 0));
        bottomLeft = Camera.main.ScreenToWorldPoint(new Vector3(target.transform.position.x - halfWidth - padding, target.transform.position.y - halfHeight - padding, 0));
        bottomRight = Camera.main.ScreenToWorldPoint(new Vector3(target.transform.position.x + halfWidth + padding, target.transform.position.y - halfHeight - padding, 0));
        bottomLeft.z = bottomRight.z = topRight.z = topLeft.z = 0;

        lineCompletor = topLeft;
        lineRenderer.SetPositions(new Vector3[] { topLeft, topRight, bottomRight, bottomLeft, lineCompletor });

    }
}
