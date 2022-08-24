using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace App.Core
{
    public static class GoToInitialScene
    {
        [MenuItem("Tools/Open First Scene _%h")]
        public static void RunMainScene()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            var scene = EditorBuildSettings.scenes;
            if (scene.Length == 0)
            {
                return;
            }

            string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(0);
            EditorSceneManager.OpenScene(scenePath);
            // EditorApplication.OpenScene(getFirstScene.path); // obsolete
            // EditorSceneManager.LoadScene(getFirstScene.path); // during playmode only
        }
    }
}