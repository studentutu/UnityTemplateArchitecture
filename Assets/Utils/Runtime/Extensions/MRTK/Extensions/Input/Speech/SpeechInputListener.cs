using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;

public class SpeechInputListener : MonoBehaviour, IMixedRealitySpeechHandler
{
    private static AudioSource ASource;

    private AudioSource Source
    {
        get
        {
            if (ASource == null)
            {
                ASource = GameObject.Instantiate(sourcePrefab);
            }

            return ASource;
        }
    }

    [SerializeField] private AudioSource sourcePrefab;
    [SerializeField] private Interactable currentInteractable;

    [Tooltip("Assign SpeechConfirmationTooltip.prefab here to display confirmation label. Optional.")] 
    [SerializeField] private SpeechConfirmationTooltip SpeechConfirmationTooltipPrefab = null;

    [NonSerialized] private string _keyword;
    [NonSerialized] private bool _isInFocus;

    public void OnSpeechKeywordRecognized(SpeechEventData eventData)
    {
        _keyword = eventData.Command.Keyword;
    }

    [UnityEngine.Scripting.Preserve]
    public void SetFocus(bool focusOn)
    {
        _isInFocus = focusOn;
    }

    [UnityEngine.Scripting.Preserve]
    public void SubmitIfInFocus()
    {
        if (_isInFocus)
        {
#pragma warning disable
            WaitForMainThread();
#pragma warning restore
        }
    }

    private async Task WaitForMainThread()
    {
        await UniTask.SwitchToMainThread();
        // From Far only
        if (currentInteractable != null)
        {
            currentInteractable.TriggerOnClick(true);
        }

        // Instantiate the Speech Confirmation Tooltip prefab if assigned
        // Ignore "Select" keyword since OS will display the tooltip. 
        if (SpeechConfirmationTooltipPrefab != null)
        {
            var speechConfirmationTooltipPrefabInstance = Instantiate(SpeechConfirmationTooltipPrefab);

            // Update the text label with recognized keyword
            speechConfirmationTooltipPrefabInstance.SetText(_keyword);

            // Trigger animation of the Speech Confirmation Tooltip prefab
            speechConfirmationTooltipPrefabInstance.TriggerConfirmedAnimation();

            // Tooltip prefab instance will be destroyed on animation complete 
            // by DestroyOnAnimationComplete.cs in the SpeechConfirmationTooltip.prefab
        }

        Source.transform.position = Camera.main.transform.position;
        Source.PlayOneShot(Source.clip);
    }

    private void OnEnable()
    {
        CoreServices.InputSystem?.RegisterHandler<IMixedRealitySpeechHandler>(this);
    }

    private void OnDisable()
    {
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealitySpeechHandler>(this);
    }
}