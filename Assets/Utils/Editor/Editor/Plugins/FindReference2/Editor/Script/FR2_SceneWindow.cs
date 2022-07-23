using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_5_3_OR_NEWER
#endif

namespace vietlabs.fr2
{
	public class FR2_SceneWindow : FR2_WindowBase, IHasCustomMenu
	{
		public override bool ShowScene
		{
			get { return true; }
		}

		//[MenuItem("Window/Find Reference 2/Scene")] 
		private static void ShowWindow()
		{
			var _window = CreateInstance<FR2_SceneWindow>();
			_window.InitIfNeeded();
			FR2_Unity.SetWindowTitle(_window, "FR2 Scene");
			_window.Show();
		}

		[NonSerialized] internal FR2_RefDrawer RefInScene;

		[NonSerialized] internal FR2_RefDrawer RefSceneInScene;

		internal int level;
		private Vector2 scrollPos;
		private string tempGUID;
		private Object tempObject;

		[NonSerialized] internal FR2_RefDrawer SceneUsesDrawer; //scene use another scene objects


		private void OnEnable()
		{
			resizerStyle = new GUIStyle();
			resizerStyle.normal.background = EditorGUIUtility.Load("icons/d_AvatarBlendBackground.png") as Texture2D;
		}

		protected override void InitIfNeeded()
		{
			if (SceneUsesDrawer != null)
			{
				return;
			}

			SceneUsesDrawer = new FR2_RefDrawer(this)
			{
				Lable = "Scene Objects",
				isDrawRefreshSceneCache = true
			};
			RefInScene = new FR2_RefDrawer(this)
			{
				Lable = "Scene Objects",
				isDrawRefreshSceneCache = true
			};
			RefSceneInScene = new FR2_RefDrawer(this)
			{
				Lable = "Scene Objects",
				isDrawRefreshSceneCache = true
			};


			FR2_Cache.onReady -= OnReady;
			FR2_Cache.onReady += OnReady;

			FR2_Setting.OnIgnoreChange -= OnSelectionChange;
			FR2_Setting.OnIgnoreChange += OnSelectionChange;

#if UNITY_2018_OR_NEWER
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
#elif UNITY_2017_OR_NEWER
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged -= OnSceneChanged;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged += OnSceneChanged;
#endif
			//Debug.Log(" FR2_Window ---> Init");
		}
#if UNITY_2018_OR_NEWER
        private void OnSceneChanged(Scene arg0, Scene arg1)
        {
            if (IsFocusingFindInScene || IsFocusingSceneToAsset || IsFocusingSceneInScene)
            {
                OnSelectionChange();
            }
        }
#endif
		protected override void OnReady()
		{
			OnSelectionChange();
		}


		public override void OnSelectionChange()
		{
			isNoticeIgnore = false;
			if (!FR2_Cache.isReady)
			{
				return;
			}

			if (lockSelection)
			{
				return;
			}

			if (focusedWindow == null)
			{
				return;
			}

			if (SceneUsesDrawer == null)
			{
				InitIfNeeded();
			}

			ids = FR2_Unity.Selection_AssetGUIDs;

			//ignore selection on asset when selected any object in scene
			if (Selection.gameObjects.Length > 0 && !FR2_Unity.IsInAsset(Selection.gameObjects[0]))
			{
				ids = new string[0];
			}

			if (IsFocusingSceneInScene)
			{
				RefSceneInScene.ResetSceneInScene(Selection.gameObjects);
			}

			if (IsFocusingUses)
			{
				SceneUsesDrawer.ResetSceneUseSceneObjects(Selection.gameObjects);
			}

			if (IsFocusingFindInScene)
			{
				RefInScene.Reset(ids, this as IWindow);
			}

			level = 0;

			EditorApplication.delayCall += Repaint;
		}


		protected override void OnGUI()
		{
			if (!CheckDrawHeader())
			{
				return;
			}

			Rect rectTop = GetTopPanelRect();
			Rect rectBot = GetBotPanelRect();

			RefDrawConfig config1 = null, config2 = null, config3 = null;
			var drawTool = false;
			var drawCount = 0;
			if (IsFocusingUses || IsFocusingSceneToAsset)
			{
				drawTool = GetDrawConfig(SceneUsesDrawer, out config1);
			}
			else if (IsFocusingSceneInScene || IsFocusingFindInScene || IsFocusingUsedBy)
			{
				if (RefSceneInScene.ElementCount() <= 0)
				{
					drawTool = GetDrawConfig(RefInScene, out config1);
				}
				else
				{
					drawTool = GetDrawConfig(RefSceneInScene, out config2);
				}
			}

			if (!IsFocusingGUIDs && !IsFocusingUnused)
			{
				drawCount += DrawConfig(config1, rectTop, rectBot, ref willRepaint);
				drawCount += DrawConfig(config2, rectTop, rectBot, ref willRepaint);
				drawCount += DrawConfig(config3, rectTop, rectBot, ref willRepaint);
			}

			if (drawTool)
			{
				drawCount++;
				GUILayout.BeginArea(rectBot);
				DrawTool();
				GUILayout.EndArea();
			}

			DrawFooter();
			OnAfterGUI(drawCount);
		}

		protected override void maskDirty()
		{
			RefInScene.SetDirty();
			RefSceneInScene.SetDirty();
			SceneUsesDrawer.SetDirty();
		}

		protected override void RefreshSort()
		{
			RefInScene.RefreshSort();
			RefSceneInScene.RefreshSort();
			SceneUsesDrawer.RefreshSort();
		}

		protected override bool checkNoticeFilter()
		{
			var rsl = false;
			if (IsFocusingFindInScene && !rsl)
			{
				rsl = RefInScene.isExclueAnyItem();
			}

			if (IsFocusingSceneInScene && !rsl)
			{
				rsl = RefSceneInScene.isExclueAnyItem();
			}

			return rsl;
		}

		protected override bool checkNoticeIgnore()
		{
			bool rsl = isNoticeIgnore;
			return rsl;
		}

		private string[] ids;
	}
}