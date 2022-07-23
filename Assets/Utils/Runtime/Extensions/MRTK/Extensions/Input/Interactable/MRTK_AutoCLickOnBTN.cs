using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.Scripting;

public class MRTK_AutoCLickOnBTN : MonoBehaviour
{
    [SerializeField] private Interactable _interactable;

    [Preserve]
    public void ForceCLick()
    {
        _interactable.OnClick?.Invoke();
    }
}