using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_5_3_OR_NEWER
#endif

namespace vietlabs.fr2
{
	// filter, ignore anh huong ket qua thi hien mau do
	// optimize lag duplicate khi use
	public class FR2_Window : FR2_WindowBase, IHasCustomMenu
	{
		[NonSerialized] internal FR2_DuplicateTree2 Duplicated;

		private string[] ids;

		internal int level;


		private Object[] objs;
		[NonSerialized] internal FR2_RefDrawer RefUnUse;
		[NonSerialized] internal FR2_RefDrawer SceneToAssetDrawer;
		private Vector2 scrollPos;


		private string tempGUID;
		private Object tempObject;
		[NonSerialized] internal FR2_RefDrawer UsedByDrawer;
		[NonSerialized] internal FR2_UsedInBuild UsedInBuild;


		[NonSerialized] internal FR2_RefDrawer UsesDrawer;

		public override bool ShowScene
		{
			get { return false; }
		}

		//[MenuItem("Window/Find Reference 2/Assets")]
		private static void ShowWindow()
		{
			var _window = CreateInstance<FR2_Window>();
			_window.InitIfNeeded();
			FR2_Unity.SetWindowTitle(_window, "FR2 Assets");
			_window.Show();
		}


		private void OnEnable()
		{
			resizerStyle = new GUIStyle();
			resizerStyle.normal.background = EditorGUIUtility.Load("icons/d_AvatarBlendBackground.png") as Texture2D;
		}

		protected override void InitIfNeeded()
		{
			{
				if (UsesDrawer != null)
				{
					return;
				}

				UsesDrawer = new FR2_RefDrawer(this)
				{
					Lable = "Assets"
				};
				UsedByDrawer = new FR2_RefDrawer(this)
				{
					Lable = "Assets"
				};
				Duplicated = new FR2_DuplicateTree2(this);
				SceneToAssetDrawer = new FR2_RefDrawer(this)
				{
					Lable = "Assets",
					isDrawRefreshSceneCache = true
				};
				RefUnUse = new FR2_RefDrawer(this)
				{
					Lable = "Unsed Asset",
					isDrawRefreshSceneCache = false
				};

				UsedInBuild = new FR2_UsedInBuild(this);
			}


			FR2_Cache.onReady -= OnReady;
			FR2_Cache.onReady += OnReady;

			FR2_Setting.OnIgnoreChange -= OnSelectionChange;
			FR2_Setting.OnIgnoreChange += OnSelectionChange;
			//Debug.Log(" FR2_Window ---> Init");
		}

		protected override void OnReady()
		{
			OnSelectionChange();
			if (IsFocusingUnused)
			{
				RefUnUse.ResetUnusedAsset();
			}
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

			if (UsesDrawer == null)
			{
				InitIfNeeded();
			}

			ids = FR2_Unity.Selection_AssetGUIDs;

			//ignore selection on asset when selected any object in scene
			if (Selection.gameObjects.Length > 0 && !FR2_Unity.IsInAsset(Selection.gameObjects[0]))
			{
				ids = new string[0];
			}

			level = 0;
			if (IsFocusingSceneToAsset)
			{
				SceneToAssetDrawer.Reset(Selection.gameObjects, true, true);
			}

			if (IsFocusingUses)
			{
				UsesDrawer.Reset(ids, true);
			}

			if (IsFocusingUsedBy)
			{
				UsedByDrawer.Reset(ids, false);
			}


			if (IsFocusingGUIDs)
			{
				objs = new Object[ids.Length];
				for (var i = 0; i < ids.Length; i++)
				{
					objs[i] = FR2_Unity.LoadAssetAtPath<Object>
					(
						AssetDatabase.GUIDToAssetPath(ids[i])
					);
				}
			}

			if (IsFocusingUsedInBuild) { }

			EditorApplication.delayCall += Repaint;
		}


		// public static Rect windowRect;


		//return true if draw tool along


		protected override void OnGUI()
		{
			if (!CheckDrawHeader())
			{
				Debug.Log("return");
				return;
			}

			//if (Selected.Count == 0){
			//  GUILayout.Label("Nothing selected");
			//} 
			Rect rectTop = GetTopPanelRect();
			Rect rectBot = GetBotPanelRect();

			RefDrawConfig config1 = null, config2 = null, config3 = null;
			var drawTool = false;
			var drawCount = 0;
			if (IsFocusingUses || IsFocusingSceneToAsset)
			{
				Debug.Log("draw 1");
				drawTool = GetDrawConfig(UsesDrawer, SceneToAssetDrawer, out config1, out config2);

				//drawTool = GetDrawConfig(UsesDrawer, SceneToAssetDrawer, out config1, out config2);
			}
			else if (IsFocusingSceneInScene || IsFocusingFindInScene || IsFocusingUsedBy)
			{
				Debug.Log("draw 2");
				drawTool = GetDrawConfig(UsedByDrawer, out config2);
			}
			else if (IsFocusingDuplicate)
			{
				Debug.Log("draw 3");
				drawTool = GetDrawConfig(Duplicated, out config1);
			}
			else if (IsFocusingGUIDs)
			{
				Debug.Log("draw 4");
				drawCount++;
				if (AnyToolInBot)
				{
					GUILayout.BeginArea(rectTop);
					DrawGUIDs();
					GUILayout.EndArea();
					drawTool = true;
				}
				else
				{
					DrawGUIDs();
					drawTool = false;
				}
			}
			else if (IsFocusingUnused)
			{
				Debug.Log("draw 5");
				drawCount++;
				if (AnyToolInBot)
				{
					GUILayout.BeginArea(rectTop);
					RefUnUse.Draw();
					GUILayout.EndArea();
					drawTool = true;
				}
				else
				{
					RefUnUse.Draw();
					drawTool = false;
				}
			}
			else if (IsFocusingUsedInBuild)
			{
				Debug.Log("draw :" + IsFocusingUsedInBuild);
				drawTool = GetDrawConfig(UsedInBuild, out config1);
			}

			if (!IsFocusingGUIDs && !IsFocusingUnused)
			{
				Debug.Log("!IsFocusingGUIDs");
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
			UsedByDrawer.SetDirty();
			UsesDrawer.SetDirty();
			Duplicated.SetDirty();
			SceneToAssetDrawer.SetDirty();
			RefUnUse.SetDirty();

			UsedInBuild.SetDirty();
		}

		protected override void RefreshSort()
		{
			UsedByDrawer.RefreshSort();
			UsesDrawer.RefreshSort();
			Duplicated.RefreshSort();
			SceneToAssetDrawer.RefreshSort();
			RefUnUse.RefreshSort();
			UsedInBuild.RefreshSort();
		}
		// public bool isExcludeByFilter;

		protected override bool checkNoticeFilter()
		{
			var rsl = false;

			if (IsFocusingUsedBy && !rsl)
			{
				rsl = UsedByDrawer.isExclueAnyItem();
			}

			if (IsFocusingDuplicate)
			{
				return Duplicated.isExclueAnyItem();
			}

			if (IsFocusingUses && rsl == false)
			{
				rsl = UsesDrawer.isExclueAnyItem();
			}

			if (IsFocusingSceneToAsset && rsl == false)
			{
				rsl = SceneToAssetDrawer.isExclueAnyItem();
			}

			//tab use by
			return rsl;
		}

		protected override bool checkNoticeIgnore()
		{
			bool rsl = isNoticeIgnore;
			return rsl;
		}

		private void DrawGUIDs()
		{
			GUILayout.Label("GUID to Object", EditorStyles.boldLabel);
			GUILayout.BeginHorizontal();
			{
				string guid = EditorGUILayout.TextField(tempGUID ?? string.Empty);
				EditorGUILayout.ObjectField(tempObject, typeof(Object), false, GUILayout.Width(120f));

				if (GUILayout.Button("Paste", EditorStyles.miniButton, GUILayout.Width(70f)))
				{
					guid = EditorGUIUtility.systemCopyBuffer;
				}

				if (guid != tempGUID && !string.IsNullOrEmpty(guid))
				{
					tempGUID = guid;

					tempObject = FR2_Unity.LoadAssetAtPath<Object>
					(
						AssetDatabase.GUIDToAssetPath(tempGUID)
					);
				}
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(10f);
			if (objs == null || ids == null)
			{
				return;
			}

			//GUILayout.Label("Selection", EditorStyles.boldLabel);
			if (ids.Length == objs.Length)
			{
				scrollPos = GUILayout.BeginScrollView(scrollPos);
				{
					for (var i = 0; i < ids.Length; i++)
					{
						GUILayout.BeginHorizontal();
						{
							EditorGUILayout.ObjectField(objs[i], typeof(Object), false);
							string idi = ids[i];
							GUILayout.TextField(idi, GUILayout.Width(240f));
							if (GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(50f)))
							{
								EditorGUIUtility.systemCopyBuffer = tempGUID = idi;

								if (!string.IsNullOrEmpty(tempGUID))
								{
									tempObject = FR2_Unity.LoadAssetAtPath<Object>
									(
										AssetDatabase.GUIDToAssetPath(tempGUID)
									);
								}

								//Debug.Log(EditorGUIUtility.systemCopyBuffer);  
							}
						}
						GUILayout.EndHorizontal();
					}
				}
				GUILayout.EndScrollView();
			}

			// GUILayout.FlexibleSpace();
			// GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Merge Selection To"))
			{
				FR2_Export.MergeDuplicate();
			}

			EditorGUILayout.ObjectField(tempObject, typeof(Object), false, GUILayout.Width(120f));
			GUILayout.EndHorizontal();
			GUILayout.Space(5f);
		}
	}
}