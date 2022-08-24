using System.Reflection;
using UnityEngine;

namespace Unity.AutomatedQA
{
    public class BuildFlagAttribute : PropertyAttribute
    {
        public string buildFlag;

        public BuildFlagAttribute(string flag)
        {
            this.buildFlag = flag;
        }
    }
    
    public enum BuildType
    {
        [BuildFlag("AQA_BUILD_TYPE_UTR")]
        UnityTestRunner,
        
        [BuildFlag("AQA_BUILD_TYPE_FULL")]
        FullBuild
    }

    public enum HostPlatform
    {
        [BuildFlag("AQA_PLATFORM_LOCAL")]
        Local,
        
        [BuildFlag("AQA_PLATFORM_CLOUD")]
        Cloud
    }
    
    public enum RecordingFileStorage
    {
        [BuildFlag("AQA_RECORDING_STORAGE_LOCAL")]
        Local,
        
        [BuildFlag("AQA_RECORDING_STORAGE_CLOUD")]
        Cloud
    }
    
 
 
        

}