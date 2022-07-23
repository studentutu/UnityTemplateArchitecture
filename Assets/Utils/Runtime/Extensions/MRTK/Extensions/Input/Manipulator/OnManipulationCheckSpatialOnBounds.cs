using System;
using System.Collections;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class OnManipulationCheckSpatialOnBounds : MonoBehaviour
{
    [SerializeField] private ObjectManipulator _manipulator;
    [SerializeField] private Collider _colliderToCheckForSpatial;
    [SerializeField] private LayerMask _castToLayermask;
    private Coroutine _ckeckCoroutine = null;

    private static Collider[] overlaps = new Collider[2];

    private void OnEnable()
    {
        _manipulator.OnManipulationStarted.AddListener(OnStartChecking);
        _manipulator.OnManipulationEnded.AddListener(OnEndManipulation);
    }

    private void OnEndManipulation(ManipulationEventData arg0)
    {
        if (CheckIfColliderIsInSpatialZone())
        {
            ForceToPreviosPosition();
        }

        StopCheckCoroutine();
    }

    private void OnStartChecking(ManipulationEventData arg0)
    {
        _ckeckCoroutine = StartCoroutine(OnEachFrameCheckSpatial());
    }

    private void OnDisable()
    {
        _manipulator.OnManipulationStarted.RemoveListener(OnStartChecking);
        _manipulator.OnManipulationEnded.RemoveListener(OnEndManipulation);
        StopCheckCoroutine();
    }

    public void ForceCheck()
    {
        if (CheckIfColliderIsInSpatialZone())
        {
            ForceToPreviosPosition();
        }
    }

    private void StopCheckCoroutine()
    {
        if (_ckeckCoroutine != null)
        {
            StopCoroutine(_ckeckCoroutine);
        }

        _ckeckCoroutine = null;
    }

    private IEnumerator OnEachFrameCheckSpatial()
    {
        while (true)
        {
            yield return null;
            if (CheckIfColliderIsInSpatialZone())
            {
                ForceToPreviosPosition();
            }
        }
    }


    private bool CheckIfColliderIsInSpatialZone()
    {
        var bounds = _colliderToCheckForSpatial.bounds;
        Transform transform1 = _colliderToCheckForSpatial.transform;
        float depthTOCheck = bounds.extents.y - 0.02f;
        if (depthTOCheck < 0)
        {
            depthTOCheck = 0.001f;
        }

        var extends = bounds.extents;
        extends = extends.SetY(depthTOCheck);
        var size = Physics.OverlapBoxNonAlloc(bounds.center, extends, overlaps, transform1.rotation,
            _castToLayermask);
        bool result = size > 0;
        return result;
    }

    private void ForceToPreviosPosition()
    {
        var getOffset = _colliderToCheckForSpatial.bounds.extents.y;
        var transform1 = _colliderToCheckForSpatial.transform;
        // Move Parent also
        var lerp = transform1.position + (transform1.up * getOffset);

        transform1.position = Vector3.Lerp(transform1.position, lerp, Time.deltaTime * 56);
    }
}