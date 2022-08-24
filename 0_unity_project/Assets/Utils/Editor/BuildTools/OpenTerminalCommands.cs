using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace App.Core.Editor
{
    public class OpenTerminalCommands
    {
        public static string OpenAdbLogcatForUnity = "adb.exe logcat -s Unity DEBUG";

        private static string ShellApp
        {
            get
            {
                string app = "powershell.exe";
                app = "cmd.exe";
#if UNITY_EDITOR_OSX
			    app = "bash";
#endif
                return app;
            }
        }

        [MenuItem("Tools/GetAdb")]
        public static void FindPathToUnityAdb()
        {
            var editorPrefs =
                EditorApplication.applicationContentsPath; // Does not work -> EditorPrefs.GetString("AndroidSdkRoot");
            if (string.IsNullOrEmpty(editorPrefs))
            {
                Debug.LogError("Please Install Android Support for Unity with SDK/OpenJDK!");
                return;
            }

            var pathToSDKBuildTools = "PlaybackEngines/AndroidPlayer/SDK/platform-tools";
            var pathToAdb = Path.Combine(editorPrefs, pathToSDKBuildTools);
            pathToAdb = pathToAdb.Replace("\\", "/");
            if (!Directory.Exists(pathToAdb))
            {
                Debug.LogError("Can't find directory");
                return;
            }

            Task.Run(() => { Shell.RunProcess(ShellApp, OpenAdbLogcatForUnity, pathToAdb); });
        }
    }
}