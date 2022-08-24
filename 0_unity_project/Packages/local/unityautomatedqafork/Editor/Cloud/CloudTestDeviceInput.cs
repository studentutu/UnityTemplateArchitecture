using System;
using System.Collections.Generic;
using UnityEngine;

namespace TestPlatforms.Cloud
{
    [CreateAssetMenu(fileName = "CloudTestSubmission", menuName = "Automated QA/Cloud Test Device Input", order = 3)]
    public class CloudTestDeviceInput : ScriptableObject
    {
        
        public string deviceOSVersion = "10";
        
        public List<string> deviceNames = new List<string>();
        
    }
    
    [Serializable]
    public class DeviceSelectionInformation
    {
        public DeviceSelectionInformation(CloudTestDeviceInput cloudTestDeviceInput)
        {
            this.osVersion = cloudTestDeviceInput.deviceOSVersion;
            this.modelNames = cloudTestDeviceInput.deviceNames;
            this.maxDevices = cloudTestDeviceInput.deviceNames.Count;
        }
        
        public string osVersion;
        public int maxDevices;
        public List<string> modelNames;
    }
}