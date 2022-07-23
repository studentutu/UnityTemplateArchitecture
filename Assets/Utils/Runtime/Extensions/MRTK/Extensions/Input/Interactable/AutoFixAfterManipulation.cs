using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using UnityEngine;
using UnityEngine.Scripting;

public class AutoFixAfterManipulation : MonoBehaviour
{
    [SerializeField] private bool test= false;

    private void OnValidate()
    {
        if (test)
        {
            test = false;
            OnRestartInout();
        }
    }

    // [SerializeField] private MixedRealityToolkit mainToolkit;
    [Preserve]
    public void OnRestartInout()
    {
        MixedRealityToolkit.InputSystem.Enable();
    }
}