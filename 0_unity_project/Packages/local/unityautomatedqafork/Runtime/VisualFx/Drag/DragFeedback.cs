using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.RecordingInputModule;

public class DragFeedback : MonoBehaviour
{
    float time = 1;
    bool mouseDown { get; set; }
    TrailRenderer trail { get; set; }
    Vector3 target;
    private void Start()
    {
        trail = gameObject.GetComponent<TrailRenderer>();
        trail.time = time;
        trail.emitting = true;
    }

    public void Activate(Vector3 position)
    {
        if (mouseDown) return; //Already activated.
        target = position;
        mouseDown = true;
        StartCoroutine(RenderTrail());
    }

    public void Move(Vector3 position)
    {
        target = position;
        StartCoroutine(RenderTrail());
    }

    public void DeActivate(Vector3 position)
    {
        if (!mouseDown) return; // Mouse release event without a drag start.
        target = position;
        mouseDown = false;
        StartCoroutine(RenderTrail());
    }

    IEnumerator RenderTrail()
    {
        if (!Camera.main.orthographic)
        {
            // A z value of zero or less will result in the Camera's position being returned instea of the ScreenToWorldPoint.
            if (target.z <= 0)
            {
                target.z = Camera.main.gameObject.transform.position.z - 1;
            }
        }
        Vector3 newPos = Camera.main.ScreenToWorldPoint(target, Camera.MonoOrStereoscopicEye.Mono);
        if (!Camera.main.orthographic)
        {
            newPos.x = -newPos.x;
            newPos.y = -newPos.y;
        }
        gameObject.transform.position = new Vector3(newPos.x, newPos.y, 0);
        yield return null;
        if (!mouseDown)
        {
            Destroy(gameObject, time);
        }
        else 
        {
            List<TouchData> data = Instance.GetTouchData();
            yield return new WaitForSeconds(Instance.GetCurrentIndex() + 1 >= data.Count ? 1f : data[Instance.GetCurrentIndex()].timeDelta);
        }
    }
}
