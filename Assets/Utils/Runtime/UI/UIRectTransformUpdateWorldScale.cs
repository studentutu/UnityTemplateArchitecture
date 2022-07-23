using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIRectTransformUpdateWorldScale : UIBehaviour, ICanvasElement
{
    [SerializeField] private RectTransform currentRectTransform;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (currentRectTransform == null)
        {
            currentRectTransform = GetComponent<RectTransform>();
        }
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
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(WaitForFrame());
        }
    }

    private IEnumerator WaitForFrame()
    {
        yield return null;
        var worldwidth = currentRectTransform.GetSize();
        currentRectTransform.localScale = new Vector3(worldwidth.x, worldwidth.y, currentRectTransform.localScale.z);
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