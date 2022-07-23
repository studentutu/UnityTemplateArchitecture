using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using UnityEngine;

namespace App.PocFactory
{
    public class ClippingPrimitivesList : MonoBehaviour
    {
        [SerializeField] private List<Renderer> listOfClippingRenderers = new List<Renderer>();
        [SerializeField] private ClippingPrimitive _clippingPrimitive;
        [SerializeField] private List<GameObject> objectWithRenderers = new List<GameObject>();

        private void OnValidate()
        {
            listOfClippingRenderers.Clear();
            foreach (var o in objectWithRenderers)
            {
                if (o == null) continue;
                var allRenderer = o.GetComponentsInChildren<Renderer>(true);
                // IgnoreClippingPrimiteItem
                foreach (var item in allRenderer)
                {
                    if (item.GetComponent<IgnoreClippingPrimiteItem>() == null &&
                        item.GetComponent<TMP_Text>() == null)
                    {
                        listOfClippingRenderers.Add(item);
                    }
                }
            }
        }

        private void OnEnable()
        {
            foreach (var rend in listOfClippingRenderers)
            {
                _clippingPrimitive.AddRenderer(rend);
            }
        }
    }
}