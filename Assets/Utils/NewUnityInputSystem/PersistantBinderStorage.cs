#if NEW_UI_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace App.Core
{
    /// <summary>
    /// Use it to serialize/deserialize New Input System Action Maps (overriden action bindings)
    /// </summary>
    [Serializable]
    public class BindingWrapperClass
    {
        public List<BindingSerializable> bindiingList = new List<BindingSerializable>();

        public string StoreControllerOverrider(List<UnityEngine.InputSystem.InputActionMap> currentActionMaps)
        {
            bindiingList.Clear();
            foreach (var map in currentActionMaps)
            {
                foreach (var binding in map.bindings)
                {
                    if (!string.IsNullOrEmpty(binding.overridePath))
                    {
                        bindiingList.Add(new BindingSerializable(binding.id.ToString(), binding.overridePath));
                    }
                }
            }

            return JsonUtility.ToJson(this);
        }

        public void LoadControllerOverrides(string fromJson,
            List<UnityEngine.InputSystem.InputActionMap> currentActionMaps)
        {
            var deserialize = JsonUtility.FromJson<BindingWrapperClass>(fromJson);
            Dictionary<System.Guid, string> overrides = new Dictionary<Guid, string>();
            foreach (var item in deserialize.bindiingList)
            {
                overrides.Add(new System.Guid(item.id), item.path);
            }

            // walk through action maps and check overrides
            foreach (var map in currentActionMaps)
            {
                var bindings = map.bindings;
                for (int i = 0; i < bindings.Count; i++)
                {
                    if (overrides.TryGetValue(bindings[i].id, out var overridePath))
                    {
                        // apply override
                        map.ApplyBindingOverride(i, new InputBinding(overridePath));
                    }
                }
            }
        }
    }

    [Serializable]
    public class BindingSerializable
    {
        public string id;
        public string path;

        public BindingSerializable(string bindingId, string bindingPath)
        {
            id = bindingId;
            path = bindingPath;
        }
    }
}
#endif