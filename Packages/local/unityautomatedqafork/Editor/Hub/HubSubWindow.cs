using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.RecordedPlayback.Editor
{
    /// <summary>
    /// A sub section of the AQA Hub Window.
    /// </summary>
    [Serializable]
    public abstract class HubSubWindow
    {
        internal AQAHubState State => HubWindow.Instance != null ? HubWindow.Instance.State : null;

        /// <summary>
        /// Set up this HubSubWindow window. Called when the HubSubWindow is created.
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// Render this HubSubWindow.
        /// </summary>
        /// <param name="baseRoot">The Base Root VisualElement of the main Hub Window.</param>
        public abstract void SetUpView(ref VisualElement baseRoot);

        /// <summary>
        /// Called from the main Hub Editor Window OnGUI method.
        /// </summary>
        public abstract void OnGUI();

        /// <summary>
        /// Called from the main Hub Editor Window Update method.
        /// </summary>
        internal virtual void Update() {}
    }
}