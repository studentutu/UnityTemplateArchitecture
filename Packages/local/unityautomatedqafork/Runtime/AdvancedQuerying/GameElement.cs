using System;
using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Unity.AutomatedQA
{
    // This class can be attached to a GameObject to give it unique identifiers and mark it as interactable so that it
    // will be picked up by recorded playback object detection.
    [DisallowMultipleComponent]
    public class GameElement : MonoBehaviour
    {
        #region Attributes
        public string HierarchyPath; //Automatically set on playmode start. Essentially a readonly field, but readonly fields cannot be serialized.
        public string Id;
        public string[] Classes;
        
        [SerializeField]
        [Unity.AutomatedQA.ReadOnly]
        private string autoId;
        /// <summary>
        /// Automatically generated GUID.
        /// </summary>
        public string AutoId => autoId;
        #endregion

        // Add any custom key and value property pairs. Search for them by selector like "[key=value]" with Driver APIs.
        #region Properties
        public Properties[] Properties;
        #endregion

        internal void ResetAutoID()
        {
            autoId = Guid.NewGuid().ToString().Replace("-","");
            #if UNITY_EDITOR
            EditorSceneManager.MarkAllScenesDirty();
            #endif
        }
        
        /// <summary>
        /// Called in editor when component is modified
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(AutoId))
            {
                ResetAutoID();
            }
            
            // Reset the AutoId if this component is duplicated from another object
            var elements = FindObjectsOfType<GameElement>();
            foreach (var e in elements)
            {
                if (e != this && e.AutoId == this.AutoId)
                {
                    ResetAutoID();
                    break;
                }
            }
        }

        private IEnumerator Start()
        {
            HierarchyPath = $"{string.Join("/", AutomatedQaTools.GetHierarchy(gameObject))}/{gameObject.name}";
            ElementQuery.Instance.RegisterElement(this);
            yield return null;
            ElementQuery.Instance.ValidatePropertiesAndAttributes(this);
        }

        // This method will be called during playback when both a press and release happen back to back on the same
        // object and can be used to manually invoke actions like OnMouseUpAsButton that are not currently supported.
        public virtual void OnClickAction() {}
    }

    [Serializable]
    public class Properties
    {
        public string PropertyName;
        public string PropertyValue;
    }
}