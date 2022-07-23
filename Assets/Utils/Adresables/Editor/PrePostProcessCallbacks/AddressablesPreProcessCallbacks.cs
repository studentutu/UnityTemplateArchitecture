#if UNITASK_ADDRESSABLE_SUPPORT
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using System.Collections;
using App.Core.Editor;

namespace App.Core.Addresables.Editor
{
    /// <summary>
    /// The script gives you choice to whether to build addressable bundles when clicking the build button.
    /// For custom build script, call PreExport method yourself.
    /// For cloud build, put BuildAddressablesProcessor.PreExport as PreExport command.
    /// Discussion: https://forum.unity.com/threads/how-to-trigger-build-player-content-when-build-unity-project.689602/
    /// </summary>
    class AddressablesPreProcessCallbacks
    {
        /// <summary>
        /// Run a clean build before export.
        /// </summary>
        static public void PreExport()
        {
            Debug.Log("BuildAddressablesProcessor.PreExport start");
            AddressableAssetSettings.CleanPlayerContent(
                AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
            AddressableAssetSettings.BuildPlayerContent();
            Debug.Log("BuildAddressablesProcessor.PreExport done");
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            BuildPreProcess.RemoveActions(ActionFactory);
            BuildPreProcess.AddAction(ActionFactory);
        }

        private static BuildPreProcess.BuildPreProcessAction ActionFactory()
        {
            return new BuildPreProcess.BuildPreProcessAction
            {
                CallbackOrder = 10000,
                Action = BuildPlayerHandler
            };
        }

        private static void BuildPlayerHandler(BuildPlayerOptions options)
        {
            if (!Application.isBatchMode)
            {
                if (EditorUtility.DisplayDialog("Build with Addressables",
                    "Do you want to build a clean addressables before export?",
                    "Build with Addressables", "Skip"))
                {
                    PreExport();
                }
            }
        }
    }
}
#endif