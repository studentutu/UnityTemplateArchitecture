using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace vietlabs.fr2
{
	public interface IWindow
	{
		bool IsFocusingDuplicate { get; }
		bool WillRepaint { get; set; }
		void Repaint();
		void OnSelectionChange();
		Dictionary<string, FR2_Ref> GetUsedByRefs();
	}

	internal interface IRefDraw
	{
		IWindow window { get; }
		int ElementCount();
		bool Draw();
	}

	internal class RefDrawConfig
	{
		public bool drawGlobal;
		public bool drawInTop;
		public bool isDraw;
		public bool isDrawTool;
		public IRefDraw refDrawer;

		public RefDrawConfig(IRefDraw drawer)
		{
			refDrawer = drawer;
		}
	}
	// internal class ShareData 
	// {
	//     //internal static Dictionary<string, FR2_Ref> UsedBy;
	// }

	public abstract class FR2_WindowBase : EditorWindow, IWindow
	{
		public virtual bool ShowScene { get; }

		internal class Icon
		{
			private static Icon _icon;
			public GUIContent Lock;
			public GUIContent LockOn;

			public GUIContent Refresh;
			public GUIContent Setting;

			public static Icon icons
			{
				get
				{
					if (_icon == null)
					{
						_icon = new Icon();
						try
						{
							_icon.Refresh = EditorGUIUtility.IconContent("d_Refresh");
							_icon.LockOn = EditorGUIUtility.IconContent("LockIcon-On");
							_icon.Lock = EditorGUIUtility.IconContent("LockIcon");
							_icon.Setting = EditorGUIUtility.IconContent("SettingsIcon", "Settings");
						}
						catch (Exception)
						{
						}
					}

					return _icon;
				}
			}
		}

		protected int selectedTab;

		protected bool IsFocusingUses
		{
			get { return selectedTab == 0; }
		}

		protected bool IsFocusingUsedBy
		{
			get { return selectedTab == 1; }
		}

		public bool IsFocusingDuplicate
		{
			get { return selectedTab == 2; }
		}

		protected bool IsFocusingUnused  
		{
			get { return selectedTab == 4; }
		}

		protected bool IsFocusingGUIDs
		{
			get { return selectedTab == 3; }
		}

		protected bool IsFocusingUsedInBuild
		{
			get { return selectedTab == 5; }
		}

		protected bool IsFocusingFindInScene
		{
			get { return selectedTab == 1; }
		}

		protected bool IsFocusingSceneToAsset
		{
			get { return selectedTab == 0; }
		}

		protected bool IsFocusingSceneInScene
		{
			get { return selectedTab == 1; }
		}

		public bool WillRepaint { get; set; }
		protected bool showFilter, showSetting, showIgnore;

		protected static GUIContent[] TOOLBARS =
		{
			new GUIContent("Uses"),
			new GUIContent("Used By"),
			new GUIContent("Duplicate"),
			new GUIContent("GUIDs"),
			new GUIContent("Unused Assets"),
			new GUIContent("Uses In Build")
		};

		[NonSerialized] protected bool lockSelection;
		[NonSerialized] internal List<FR2_Asset> Selected;
		public float sizeRatio = 0.5f;
		protected Rect resizer;
		protected bool isResizing;
		protected GUIStyle resizerStyle;

		public static bool isNoticeIgnore;

		public void AddItemsToMenu(GenericMenu menu)
		{
			FR2_Cache api = FR2_Cache.Api;
			if (api == null)
			{
				return;
			}

			menu.AddDisabledItem(new GUIContent("FR2 - v2.4.3"));
			menu.AddSeparator(string.Empty);

			menu.AddItem(new GUIContent("Enable"), !api.disabled, () => { api.disabled = !api.disabled; });

			menu.AddItem(new GUIContent("Clear Selection"), false, () => { FR2_Selection.ClearSelection(); });

			menu.AddItem(new GUIContent("Commit Selection (" + FR2_Selection.SelectionCount + ")"), false,
				FR2_Selection.Commit);

			menu.AddItem(new GUIContent("Refresh"), false, () =>
			{
				FR2_Asset.lastRefreshTS = Time.realtimeSinceStartup;
				FR2_Cache.Api.Check4Changes(true, true);
				FR2_SceneCache.Api.SetDirty();
			});

#if FR2_DEV
            menu.AddItem(new GUIContent("Refresh Usage"), false, () => FR2_Cache.Api.Check4Usage());
            menu.AddItem(new GUIContent("Refresh Selected"), false, ()=> FR2_Cache.Api.RefreshSelection());
            menu.AddItem(new GUIContent("Clear Cache"), false, () => FR2_Cache.Api.Clear());
#endif
		}

		protected virtual void OnTabChanged() { }

		protected abstract void InitIfNeeded();
		public abstract void OnSelectionChange();
		protected abstract void OnGUI();
		protected abstract void RefreshSort();
		protected abstract void maskDirty();
		protected abstract bool checkNoticeFilter();
		protected abstract bool checkNoticeIgnore();

		protected abstract void OnReady();


#if UNITY_2018_OR_NEWER
        protected void OnSceneChanged(Scene arg0, Scene arg1)
        {
            if (IsFocusingFindInScene || IsFocusingSceneToAsset || IsFocusingSceneInScene)
            {
                OnSelectionChange();
            }
        }
#endif

		protected bool willRepaint;

		protected bool CheckDrawHeader()
		{
			if (EditorApplication.isCompiling)
			{
				EditorGUILayout.HelpBox("Compiling scripts, please wait!", MessageType.Warning);
				Repaint();
				return false;
			}

			if (EditorApplication.isUpdating)
			{
				EditorGUILayout.HelpBox("Importing assets, please wait!", MessageType.Warning);
				Repaint();
				return false;
			}

			InitIfNeeded();

			if (EditorSettings.serializationMode != SerializationMode.ForceText)
			{
				EditorGUILayout.HelpBox("FR2 requires serialization mode set to FORCE TEXT!", MessageType.Warning);
				if (GUILayout.Button("FORCE TEXT"))
				{
					EditorSettings.serializationMode = SerializationMode.ForceText;
				}

				return false;
			}

			if (FR2_Cache.hasCache && !FR2_Cache.CheckSameVersion())
			{
				EditorGUILayout.HelpBox("Incompatible cache version found, need a full refresh may take time!",
					MessageType.Warning);
				if (GUILayout.Button("Scan project"))
				{
					FR2_Cache.DeleteCache();
					FR2_Cache.CreateCache();
				}

				return false;
			}

			if (!FR2_Cache.isReady)
			{
				if (!FR2_Cache.hasCache)
				{
					EditorGUILayout.HelpBox(
						"FR2 cache not found!\nFirst scan may takes quite some time to finish but you would be able to work normally while the scan works in background...",
						MessageType.Warning);
					if (GUILayout.Button("Scan project"))
					{
						FR2_Cache.CreateCache();
					}

					return false;
				}

				if (!DrawEnable())
				{
					return false;
				}

				FR2_Cache api = FR2_Cache.Api;
				string text = "Refreshing ... " + (int) (api.progress * api.workCount) + " / " + api.workCount;
				Rect rect = GUILayoutUtility.GetRect(1f, Screen.width, 18f, 18f);
				EditorGUI.ProgressBar(rect, api.progress, text);
				Repaint();
				return false;
			}


			if (!DrawEnable())
			{
				return false;
			}

			willRepaint = Event.current.type == EventType.ScrollWheel;
			int newTab = selectedTab;

			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				Color color = GUI.contentColor;
				GUI.contentColor = EditorGUIUtility.isProSkin
					? new Color(0.9f, 0.9f, 0.9f, 1f)
					: new Color(0.1f, 0.1f, 0.1f, 1f);

				GUIContent icon = Icon.icons.Lock;
				if (lockSelection)
				{
					icon = Icon.icons.LockOn;
				}

				bool v = GUILayout.Toggle(lockSelection, icon, EditorStyles.toolbarButton, GUILayout.Width(21f));

				GUI.contentColor = color;

				if (v != lockSelection)
				{
					lockSelection = v;
					if (lockSelection == false)
					{
						OnSelectionChange();
					}

					willRepaint = true;
				}


				for (var i = 0; i < TOOLBARS.Length; i++)
				{
					if (ShowScene && i > 1)
					{
						break;
					}

					bool isSelected = selectedTab == i;
					bool b = GUILayout.Toggle(isSelected, TOOLBARS[i], EditorStyles.toolbarButton);
					if (b != isSelected)
					{
						newTab = i;
					}
				}

				// newTab = GUILayout.Toolbar(selectedTab, TOOLBARS);
			}
			GUILayout.EndHorizontal();

			if (newTab != selectedTab)
			{
				selectedTab = newTab;
				OnTabChanged();
				// Check4Changes means delay calls to OnReady :: Refresh !
				//if (FR2_Cache.Api.isReady) FR2_Cache.Api.Check4Changes();
				OnReady();
			}

			if (Selected == null)
			{
				Selected = new List<FR2_Asset>();
			}

			return true;
		}

		protected void OnAfterGUI(int drawCount)
		{
			if (willRepaint || WillRepaint || !FR2_SceneCache.ready)
			{
				WillRepaint = false;
				Repaint();
			}

			if (drawCount > 1)
			{
				DrawResizer();
				ProcessEvents(Event.current);
			}

			if (GUI.changed)
			{
				Repaint();
			}
		}

		protected bool DrawEnable()
		{
			FR2_Cache api = FR2_Cache.Api;
			if (api == null)
			{
				return false;
			}

			bool v = api.disabled;

			if (v)
			{
				EditorGUILayout.HelpBox("Find References 2 is disabled!", MessageType.Warning);
				if (GUILayout.Button("Enable"))
				{
					api.disabled = !api.disabled;
					Repaint();
				}

				return !api.disabled;
			}

			if (!api.ready)
			{
				float w = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 70f;
				api.priority = EditorGUILayout.IntSlider("Priority", api.priority, 0, 5);
				EditorGUIUtility.labelWidth = w;
			}

			return !api.disabled;
		}

		protected bool AnyToolInBot
		{
			get { return FR2_Setting.showSettings || showIgnore || showFilter; }
		}

		internal bool GetDrawConfig(IRefDraw refDrawer, out RefDrawConfig config)
		{
			bool drawTool = AnyToolInBot;
			config = new RefDrawConfig(refDrawer);
			config.drawGlobal = !drawTool;
			config.drawInTop = drawTool;
			config.isDraw = true;
			return drawTool;
		}

		internal bool GetDrawConfig(IRefDraw refTop, IRefDraw refBot, out RefDrawConfig config1,
			out RefDrawConfig config2)
		{
			var drawTool = true;
			config1 = new RefDrawConfig(refTop);
			config2 = new RefDrawConfig(refBot);
			config1.isDraw = config1.refDrawer.ElementCount() > 0;
			config2.isDraw = config2.refDrawer.ElementCount() > 0;

			if (AnyToolInBot)
			{
				if (refTop.ElementCount() > 0)
				{
					config1.drawInTop = true;
					if (refBot.ElementCount() > 0)
					{
						drawTool = false;
						config2.isDrawTool = true;
					}
				}
				else
				{
					config2.drawInTop = true;
				}
			}
			else
			{
				drawTool = false;
				if (refTop.ElementCount() > 0)
				{
					config1.drawInTop = true;
					if (refBot.ElementCount() > 0)
					{
						config2.drawInTop = false;
					}
					else
					{
						config1.drawGlobal = true;
					}
				}
				else
				{
					// config2.drawInTop = true;
					config2.drawGlobal = true;
				}
			}

			return drawTool;
		}

		internal int DrawConfig(RefDrawConfig config, Rect rectTop, Rect rectBot, ref bool WillRepaint)
		{
			if (config == null)
			{
				return 0;
			}

			if (config.refDrawer == null)
			{
				return 0;
			}

			if (!config.isDraw)
			{
				return 0;
			}

			var willRepaint = false;
			if (config.drawGlobal)
			{
				willRepaint = config.refDrawer.Draw();
			}
			else
			{
				if (config.drawInTop)
				{
					GUILayout.BeginArea(rectTop);
					willRepaint = config.refDrawer.Draw();
					GUILayout.EndArea();
				}
				else if (config.isDrawTool)
				{
					GUILayout.BeginArea(rectBot);
					willRepaint = config.refDrawer.Draw();
					DrawTool();
					GUILayout.EndArea();
				}
				else
				{
					GUILayout.BeginArea(rectBot);
					willRepaint = config.refDrawer.Draw();
					GUILayout.EndArea();
				}
			}

			if (willRepaint)
			{
				WillRepaint = true;
			}

			return 1;
		}

		protected void DrawTool()
		{
			if (showFilter)
			{
				if (AssetType.DrawSearchFilter())
				{
					maskDirty();
				}
			}
			else if (showIgnore)
			{
				GUILayout.BeginHorizontal(EditorStyles.toolbar);
				GUILayout.Label("Ignores");
				// showContent = EditorGUILayout.Foldout(showContent, searchLable);
				GUILayout.EndHorizontal();

				if (AssetType.DrawIgnoreFolder())
				{
					maskDirty();
				}
			}
			else if (FR2_Setting.showSettings)
			{
				FR2_Setting.s.DrawSettings();
			}
		}

		public Rect GetTopPanelRect()
		{
			return new Rect(0, 17, position.width, position.height * sizeRatio - 20);
		}

		public Rect GetBotPanelRect()
		{
			return new Rect(0, position.height * sizeRatio + 5, position.width,
				position.height * (1 - sizeRatio) - 5 - paddingBot);
		}

		private void DrawResizer()
		{
			resizer = new Rect(0, position.height * sizeRatio - 5f, position.width, 10f);

			Vector2 a = resizer.position + Vector2.up * 5f;
			GUILayout.BeginArea(new Rect(a.x, a.y, position.width, 1), resizerStyle);
			// GUILayout.BeginArea(new Rect(resizer.position + (Vector2.up * 5f), new Vector2(position.width, 1)), resizerStyle);
			GUILayout.EndArea();

			EditorGUIUtility.AddCursorRect(resizer, MouseCursor.ResizeVertical);
		}

		protected void ProcessEvents(Event e)
		{
			switch (e.type)
			{
				case EventType.MouseDown:
					if (e.button == 0 && resizer.Contains(e.mousePosition))
					{
						isResizing = true;
					}

					break;
#if UNITY_5_3_OR_NEWER
				case EventType.MouseLeaveWindow:
#endif
				case EventType.MouseUp:
					isResizing = false;
					break;
			}

			Resize(e);
		}

		private void Resize(Event e)
		{
			if (isResizing)
			{
				sizeRatio = e.mousePosition.y / position.height;
				normalizeSize();
				Repaint();
			}
		}

		private void normalizeSize()
		{
			if (sizeRatio * position.height < paddingTop)
			{
				sizeRatio = paddingTop / position.height;
			}

			if (position.height - sizeRatio * position.height < paddingBot)
			{
				sizeRatio = (position.height - paddingBot) * 1f / position.height;
			}
		}

		private const float paddingTop = 70;
		private const float paddingBot = 25;

		protected void DrawFooter()
		{
			GUILayout.FlexibleSpace();


			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				Color color = GUI.contentColor;
				GUI.contentColor = EditorGUIUtility.isProSkin
					? new Color(0.9f, 0.9f, 0.9f, 1f)
					: new Color(0.1f, 0.1f, 0.1f, 1f);

				if (FR2_Unity.DrawToggleToolbar(ref FR2_Setting.showSettings, Icon.icons.Setting, 21f))
				{
					maskDirty();
					if (FR2_Setting.showSettings)
					{
						showFilter = showIgnore = false;
					}
				}

				GUI.contentColor = color;

				bool v = checkNoticeFilter();
				string content = !FR2_Setting.IsIncludeAllType() ? "*Filter" : "Filter";
				if (v)
				{
					Color oc = GUI.backgroundColor;
					GUI.backgroundColor = Color.red;
					v = GUILayout.Toggle(showFilter, content, EditorStyles.toolbarButton, GUILayout.Width(50f));
					GUI.backgroundColor = oc;
				}
				else
				{
					v = GUILayout.Toggle(showFilter, content, EditorStyles.toolbarButton, GUILayout.Width(50f));
				}

				if (v != showFilter)
				{
					showFilter = v;
					if (showFilter)
					{
						FR2_Setting.showSettings = showIgnore = false;
					}
				}

				v = checkNoticeIgnore();
				content = FR2_Setting.IgnoreAsset.Count > 0 ? "*Ignore" : "Ignore";
				if (v)
				{
					Color oc = GUI.backgroundColor;
					GUI.backgroundColor = Color.red;
					v = GUILayout.Toggle(showIgnore, content, EditorStyles.toolbarButton, GUILayout.Width(50f));
					GUI.backgroundColor = oc;
				}
				else
				{
					v = GUILayout.Toggle(showIgnore, content, EditorStyles.toolbarButton, GUILayout.Width(50f));
				}

				// var i = GUILayout.Toggle(showIgnore, content, EditorStyles.toolbarButton, GUILayout.Width(50f));
				if (v != showIgnore)
				{
					showIgnore = v;
					if (v)
					{
						showFilter = FR2_Setting.showSettings = false;
					}
				}

				bool ss = FR2_Setting.ShowSelection;
				v = GUILayout.Toggle(ss, "Selection", EditorStyles.toolbarButton, GUILayout.Width(60f));
				if (v != ss)
				{
					FR2_Setting.ShowSelection = v;
					maskDirty();
				}

				if (FR2_Selection.SelectionCount > 0)
				{
					if (GUILayout.Button("Commit Selection [" + FR2_Selection.SelectionCount + "]",
						EditorStyles.toolbarButton))
					{
						FR2_Selection.Commit();
					}

					if (GUILayout.Button("Clear Selection", EditorStyles.toolbarButton))
					{
						FR2_Selection.ClearSelection();
					}
				}


				GUILayout.FlexibleSpace();


				if (!IsFocusingDuplicate && !IsFocusingGUIDs)
				{
					float o = EditorGUIUtility.labelWidth;
					EditorGUIUtility.labelWidth = 42f;

					FR2_RefDrawer.Mode ov = FR2_Setting.GroupMode;
					var vv = (FR2_RefDrawer.Mode) EditorGUILayout.EnumPopup("Group", ov, GUILayout.Width(122f));
					if (vv != ov)
					{
						FR2_Setting.GroupMode = vv;
						maskDirty();
					}

					GUILayout.Space(4f);
					EditorGUIUtility.labelWidth = 30f;

					FR2_RefDrawer.Sort s = FR2_Setting.SortMode;
					var vvv = (FR2_RefDrawer.Sort) EditorGUILayout.EnumPopup("Sort", s, GUILayout.Width(100f));
					if (vvv != s)
					{
						FR2_Setting.SortMode = vvv;
						RefreshSort();
					}

					EditorGUIUtility.labelWidth = o;
				}
			}

			GUILayout.EndHorizontal();
		}

		public Dictionary<string, FR2_Ref> GetUsedByRefs()
		{
			return null;
		}
	}
}