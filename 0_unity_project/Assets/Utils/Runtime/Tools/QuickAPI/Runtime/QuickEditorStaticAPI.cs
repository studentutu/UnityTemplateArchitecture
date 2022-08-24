#if UNITY_EDITOR
namespace QuickEditor.Core
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public static class QuickEditorStaticAPI
    {
        public static void SetAppIcon(BuildTargetGroup group, Texture2D texture)
        {
            var count = PlayerSettings.GetIconSizesForTargetGroup(group).Length;

            var textures = new List<Texture2D>();
            for (int i = 0; i < count; i++)
            {
                textures.Add(texture);
            }
            PlayerSettings.SetIconsForTargetGroup(group, textures.ToArray());
        }

        public static bool SetSplashScreen(string name, Texture2D texture)
        {
            if (texture == null) { return false; }
            var property = typeof(PlayerSettings).Invoke("FindProperty", name) as SerializedProperty;
            property.serializedObject.Update();
            property.objectReferenceValue = texture;
            property.serializedObject.ApplyModifiedProperties();
            return texture != null;
        }
        
        // 拷贝Project视图中的某个Prefab
        private static void ProjectDuplicate()
        {
            var type = typeof(Editor).Assembly.GetType("UnityEditor.ProjectWindowUtil");

            var duplicate = type.GetMethod("DuplicateSelectedAssets",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            duplicate.Invoke(null, null);
        }

        // 拷贝Hierarchy视图中的某个Prefab，优点：（1）速度快（2）保持引用关系。在代码中Instantiate会丢失引用，并且速度很慢
        private static void HierarchyDuplicate()
        {
            var type = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            var window = EditorWindow.GetWindow(type);
            var duplicateSelectedFunc = type.GetMethod("DuplicateGO",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            duplicateSelectedFunc.Invoke(window, null);
        }

        // 手动设置Game视图的分辨率，可以用来在编辑态下动态改变runtime分辨率
        public static void SetGameView(int index)
        {
            var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(type);
            var SizeSelectionCallback = type.GetMethod("SizeSelectionCallback",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            SizeSelectionCallback.Invoke(window, new object[] { index, null });
        }
    }
}
#endif