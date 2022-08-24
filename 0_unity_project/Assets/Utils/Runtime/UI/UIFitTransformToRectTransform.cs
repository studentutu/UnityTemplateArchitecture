using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
public class UIFitTransformToRectTransform : UIBehaviour, ICanvasElement
{
    [SerializeField] private RectTransform sourceRectTransform;
    [SerializeField] private Transform targetTransform;
    [Tooltip("Toggle it to update in editor. This toggle works only in edit mode")]
    [SerializeField] private bool editorUpdate;
    
#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (sourceRectTransform == null)
        {
            sourceRectTransform = GetComponent<RectTransform>();
        }
        if (sourceRectTransform == null)
        {
            targetTransform = GetComponent<Transform>();
        }

        if (!Application.isPlaying && sourceRectTransform != null && targetTransform != null)
        {
            if (!editorUpdate)
            {
                editorUpdate = true;
                SetLocalWorldWidthHeight();
            }
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
        SetLocalWorldWidthHeight();
    }

    private void SetLocalWorldWidthHeight()
    {
        var sourceSize = sourceRectTransform.GetSize();
        targetTransform.localScale = new Vector3(sourceSize.x, sourceSize.y, sourceRectTransform.localScale.z);
    targetTransform.localPosition = new Vector3( sourceRectTransform.localPosition.x,sourceRectTransform.localPosition.y,targetTransform.localPosition.z);
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