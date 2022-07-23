using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
public class UIRectTransformUpdateWorldCollider : UIBehaviour, ICanvasElement
{
    [SerializeField] private RectTransform currentRectTransform;
    [SerializeField] private Vector3 sizeOffset;
    [SerializeField] private BoxCollider colliderBox;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (!enabled)
        {
            return;
        }

        if (Application.isPlaying)
        {
            return;
        }

        if (currentRectTransform == null)
        {
            currentRectTransform = GetComponent<RectTransform>();
        }

        UpdateContentFitter();
    }
#endif

    protected override void OnEnable()
    {
        base.OnEnable();
        CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
        CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
    }

    protected override void OnCanvasGroupChanged()
    {
        base.OnCanvasGroupChanged();
        UpdateContentFitter();
    }

    protected override void OnTransformParentChanged()
    {
        base.OnTransformParentChanged();
        UpdateContentFitter();
    }

    protected override void OnCanvasHierarchyChanged()
    {
        base.OnCanvasHierarchyChanged();
        UpdateContentFitter();
    }

    private void UpdateContentFitter()
    {
        var worldwidth = currentRectTransform.GetSize();
        colliderBox.size = new Vector3(worldwidth.x + sizeOffset.x, worldwidth.y + sizeOffset.y,
            colliderBox.size.z + sizeOffset.z);
    }

    public void Rebuild(CanvasUpdate executing)
    {
        UpdateContentFitter();
    }

    public void LayoutComplete()
    {
        UpdateContentFitter();
    }

    public void GraphicUpdateComplete()
    {
        UpdateContentFitter();
    }
}