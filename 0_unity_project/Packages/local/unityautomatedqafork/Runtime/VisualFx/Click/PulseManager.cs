using System.Collections;
using UnityEngine;

namespace Unity.AutomatedQA
{
    public class PulseManager : MonoBehaviour
    {
        private bool _killEarly;
        private bool _killImmediate;
        public bool IsMouseDown;
        public void Init(Vector3 target, bool isMouseDown) 
        {
            IsMouseDown = isMouseDown;
            _killImmediate = _killEarly = false;
            StartCoroutine(Pulse(target));
        }

        public void KillEarly(bool killImmediate = false)
        {
            if (_killEarly && !killImmediate) return; //We are already speeding this pulse up. Only execute if requested to kill immediately.
            _killEarly = true;
            _killImmediate = killImmediate;
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).GetComponent<Pulse>().SpeedUp(_killImmediate);
            }
            if (_killImmediate)
            {
                StopAllCoroutines();
                VisualFxManager.ReturnVisualFxCanvas(gameObject);
            }
        }

        private IEnumerator Pulse(Vector3 target)
        {
            if (IsMouseDown)
            {
                GameObject ring = VisualFxManager.PulseRings[0];
                Pulse pulse = ring.GetComponent<Pulse>();
                VisualFxManager.PulseRings.RemoveAt(0);
                ring.transform.SetParent(transform);
                ring.transform.position = target;
                ring.SetActive(true);
                pulse.Init(IsMouseDown, false);
                yield return null;
            }
            else
            {                
                // Trigger any "hold down mouse" pulses that are not animating.
                for (int i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).GetComponent<Pulse>().Continue();
                }
                float thisDuration = VisualFxManager.PulseDuration;
                while (thisDuration >= 0)
                {
                    GameObject ring = VisualFxManager.PulseRings[0];
                    Pulse pulse = ring.GetComponent<Pulse>();
                    VisualFxManager.PulseRings.RemoveAt(0);
                    ring.transform.SetParent(transform);
                    ring.transform.position = target;
                    ring.SetActive(true);
                    pulse.Init(IsMouseDown, _killEarly);
                    yield return new WaitForSeconds(VisualFxManager.PulseInterval);
                    thisDuration -= VisualFxManager.PulseInterval;
                }
                VisualFxManager.ReturnVisualFxCanvas(gameObject);
            }
        }
    }
}