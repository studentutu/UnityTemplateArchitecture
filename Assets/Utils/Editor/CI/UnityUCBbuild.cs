#if UNITY_CLOUD_BUILD
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine.CloudBuild;

namespace App.Core.Editor.CI
{
	/// See examples here : https://github.com/Arvtesh/UnityFx.BuildTools/tree/master/Unity/Assets/UnityFx.BuildTools/Scripts/Editor
    public class UnityUCBbuild
    {
		/// <summary>
		/// 
		/// </summary>
		public const string DefaultBuildPath = "../../Builds";
        
		[PostProcessBuild]
        public static void PostprocessBuildOnUCB(BuildTarget buildTarget, string pathToBuiltProject)
        {
            
        }
    }
}
#endif