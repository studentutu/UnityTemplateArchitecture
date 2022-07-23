using TestPlatforms.Cloud;
using Unity.CloudTesting.Editor;
using UnityEditor;
using UnityEditor.TestTools;

[assembly:TestPlayerBuildModifier(typeof(CloudTestRunner))]


namespace Unity.CloudTesting.Editor
{
    public class CloudTestRunner: ITestPlayerBuildModifier
    {
        public BuildPlayerOptions ModifyOptions(BuildPlayerOptions playerOptions)
        {
            if (CloudTestPipeline.IsRunningOnCloud())
            {
                playerOptions.options &= ~(BuildOptions.AutoRunPlayer);
#if UNITY_IOS
                playerOptions.locationPathName = CloudTestConfig.IOSBuildDir;
#else
                playerOptions.locationPathName = CloudTestConfig.BuildPath;
#endif

                return playerOptions;    
            }

            return playerOptions;
        }
    }
}
