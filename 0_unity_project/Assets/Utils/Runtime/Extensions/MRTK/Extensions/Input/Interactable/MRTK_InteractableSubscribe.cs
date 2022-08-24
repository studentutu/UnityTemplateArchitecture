using System;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine.Events;

public static class MRTK_InteractableSubscribe
{
    public class MRTKToggleEvents
    {
        public UnityEvent OnSelect;
        public UnityEvent OnDeselect;

        public MRTKToggleEvents(UnityEvent _select, UnityEvent _deselect)
        {
            OnSelect = _select;
            OnDeselect = _deselect;
        }
    }

    public class MRTKFocusEvents
    {
        public UnityEvent OnFocusGet;
        public UnityEvent OnFocusLost;

        public MRTKFocusEvents(UnityEvent _select, UnityEvent _deselect)
        {
            OnFocusGet = _select;
            OnFocusLost = _deselect;
        }
    }

    /// <summary>
    /// First item is Select, second one is deselect
    /// </summary>
    /// <param name="interactable"></param>
    /// <returns></returns>
    public static MRTKToggleEvents SubscribeToToggle(this Interactable interactable)
    {
        var findTOggle = interactable.GetReceiver<InteractableOnToggleReceiver>();
        if (findTOggle != null)
        {
            return new MRTKToggleEvents(findTOggle.OnSelect, findTOggle.OnDeselect);
        }

        return null;
    }

    /// <summary>
    /// First item is Select, second one is deselect
    /// </summary>
    /// <param name="interactable"></param>
    /// <returns></returns>
    public static MRTKFocusEvents SubscribeToFocus(this Interactable interactable)
    {
        var findTOggle = interactable.GetReceiver<InteractableOnFocusReceiver>();
        if (findTOggle != null)
        {
            return new MRTKFocusEvents(findTOggle.OnFocusOn, findTOggle.OnFocusOff);
        }

        return null;
    }
}