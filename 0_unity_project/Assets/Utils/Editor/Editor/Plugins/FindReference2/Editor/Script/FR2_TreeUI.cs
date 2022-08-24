#define DEV_MODE

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace vietlabs.fr2
{
	public class FR2_TreeUI
	{
		public float itemH = 16f;
		private Vector2 position;

		private FR2_TreeItemUI root;
		public int selected;
		public Color selectedColor = new Color32(0, 0, 255, 100);
		private Rect visibleRect;

		public void Draw(FR2_TreeItemUI root, bool drawRoot = true)
		{
			EventType evtType = Event.current.type;
			Rect r = GUILayoutUtility.GetRect(1f, Screen.width, itemH, Screen.height);

			if (evtType != EventType.Layout)
			{
				visibleRect = r;
			}

			int n = root.GetNVisible(Mathf.Max(root._treeStamp, 1));
			var contentRect = new Rect(0f, 0f, 1f, n * itemH);
			int nVisible = Mathf.RoundToInt(visibleRect.height / itemH) + 1;
			int min = Mathf.Max(0, Mathf.FloorToInt(position.y / itemH));
			int max = Mathf.Min(min + nVisible, n);
			bool noScroll = contentRect.height < visibleRect.height;

			//Debug.Log("Drawing :: " + min + ":" + max + ":" + n);

			if (noScroll)
			{
				position = Vector2.zero;
			}

			position = GUI.BeginScrollView(visibleRect, position, contentRect);
			{
				var rect = new Rect(0, 0, r.width - (noScroll ? 4f : itemH), itemH);
				var current = 0;
				if (drawRoot)
				{
					root.Draw(min, max, ref rect, ref current);
				}
				else
				{
					root.DrawChildren(min, max, ref rect, ref current);
				}
			}

			GUI.EndScrollView();
		}

		public void Draw<T>(List<T> list) where T : FR2_TreeItemUI
		{
			if (root == null)
			{
				root = new FR2_TreeItemUI();
				for (var i = 0; i < list.Count; i++)
				{
					root.AddChild(list[i]);
				}
			}

			Draw(root, false);
		}

		public void Draw<T>(List<T> objList, Action<int, Rect, T> itemDrawer)
		{
			Draw(objList.Count, (idx, r, hasMouse) => { itemDrawer(idx, r, objList[idx]); });

			//var evtType = Event.current.type;
			//Rect r = GUILayoutUtility.GetRect(1f, Screen.width, 16f, Screen.height);

			//if (evtType != EventType.layout){
			//	visibleRect = r;
			//}

			//var n			= objList.Count;
			//var contentRect	= new Rect(0f, 0f, 1f, n * itemH);
			//var nVisible	= Mathf.RoundToInt(visibleRect.height/itemH) + 1;
			//var min			= Mathf.Max(0, Mathf.FloorToInt(position.y / itemH));
			//var max 		= Mathf.Min(min + nVisible, n);
			//var noScroll	= contentRect.height < visibleRect.height;

			//if (noScroll) position = Vector2.zero;

			//position = GUI.BeginScrollView(visibleRect, position, contentRect);
			//{
			//	var rect	= new Rect (0, 0, r.width - (noScroll ? 4f : 16f), itemH);
			//	for (var i = min; i<max; i++){
			//		rect.y = i * itemH;
			//		itemDrawer(i, rect, objList[i]);
			//	}
			//}

			//GUI.EndScrollView();
		}

		public void Draw(int n, Action<int, Rect, bool> drawer, bool repaintOnly = false)
		{
			EventType evtType = Event.current.type;
			Rect r = GUILayoutUtility.GetRect(1f, Screen.width, itemH, Screen.height);

			if (evtType != EventType.Layout)
			{
				visibleRect = r;
			}

			if (!repaintOnly || evtType == EventType.Repaint)
			{
				var contentRect = new Rect(0f, 0f, 1f, n * itemH);
				int nVisible = Mathf.RoundToInt(visibleRect.height / itemH) + 1;
				int min = Mathf.Max(0, Mathf.FloorToInt(position.y / itemH));
				int max = Mathf.Min(min + nVisible, n);
				bool noScroll = contentRect.height < visibleRect.height;

				if (noScroll)
				{
					position = Vector2.zero;
				}

				position = GUI.BeginScrollView(visibleRect, position, contentRect);

				//Debug.Log(min + ":" + max + ":" + n);

				for (int i = min; i < max; i++)
				{
					var rr = new Rect(0, itemH * i, Screen.width, itemH);

					if (i == selected)
					{
						Color c = GUI.color;
						GUI.color = selectedColor;
						{
							GUI.DrawTexture(rr, EditorGUIUtility.whiteTexture);
						}
						GUI.color = c;
					}

					bool containsMouse = rr.Contains(Event.current.mousePosition);
					bool hasMouse = Event.current.type == EventType.MouseDown && containsMouse;

					drawer(i, rr, hasMouse);

					if (Event.current.type == EventType.MouseUp && containsMouse)
					{
						selected = i;
						Event.current.Use();
					}
				}

				GUI.EndScrollView();
			}
		}
	}

	public class FR2_TreeItemUI
	{
		private const float arrowWidth = 14f;

		// -------------------------- DRAWER ----------------------------
		private static GUIStyle foldStyle;

		// -------------------------- PARENT / CHILDREN ----------------------------

		protected bool _expand = true;

		protected int _itemHeight = 16;
		protected int _nFakeSize;
		protected int _nVisible;
		internal int _treeStamp;
		internal List<FR2_TreeItemUI> children = new List<FR2_TreeItemUI>();

		// -------------------------- PARENT / CHILDREN ----------------------------

		internal FR2_TreeItemUI parent;

		public virtual int GetNVisible(int stamp)
		{
			if (!_expand)
			{
				return 1 + _nFakeSize;
			}

			if (_treeStamp == stamp)
			{
				return _nVisible + _nFakeSize;
			}

			RefreshVisibleCount(stamp);
			return _nVisible + _nFakeSize;
		}

		public void ToggleExpand()
		{
			int delta = _nVisible - 1;
			_expand = !_expand;
			SetDeltaVisible(_expand ? delta : -delta, _treeStamp);
		}

		private void RefreshVisibleCount(int stamp)
		{
			_treeStamp = stamp;
			_nVisible = 1;

			for (var i = 0; i < children.Count; i++)
			{
				FR2_TreeItemUI c = children[i];
				_nVisible += c.GetNVisible(stamp);
			}
		}

		private void SetDeltaVisible(int delta, int stamp)
		{
			_treeStamp = stamp;
			_nVisible += delta;

			if (parent != null)
			{
				parent.SetDeltaVisible(delta, stamp);
			}
			else
			{
				GetNVisible(++stamp);
			}
		}

		public FR2_TreeItemUI AddChild(FR2_TreeItemUI child)
		{
#if DEV_MODE
			{
				if (!IsValidChild(child))
				{
					return this;
				}

				if (child.parent == this)
				{
					Debug.LogWarning("Child.parent already == this");
					return this;
				}

				if (children.Contains(child))
				{
					Debug.LogWarning("Something broken, child already in this.children list <" + child +
					                 "> but its parent not set to this");
					return this;
				}
			}
#endif

			if (child.parent != null)
			{
				child.parent.RemoveChild(child);
			}

			child.parent = this;
			children.Add(child);

			int delta = child.GetNVisible(_treeStamp);
			SetDeltaVisible(delta, _treeStamp);
			return this;
		}

		public FR2_TreeItemUI RemoveChild(FR2_TreeItemUI child)
		{
#if DEV_MODE
			{
				if (!IsValidChild(child))
				{
					return this;
				}

				if (child.parent != this)
				{
					Debug.LogWarning("child.parent != this, can not remove");
					return this;
				}

				if (!children.Contains(child))
				{
					Debug.LogWarning(
						"Something broken, child.parent == this but this.children does not contains child");
					return this;
				}
			}
#endif


			child.parent = null;
			children.Remove(child);

			//Recursive update nVIsible items up the tree
			int delta = child.GetNVisible(_treeStamp);
			SetDeltaVisible(-delta, _treeStamp);

			return this;
		}

#if DEV_MODE
		private bool IsValidChild(FR2_TreeItemUI child)
		{
			if (child == null)
			{
				Debug.LogWarning("Child should not be null <" + child + ">");
				return false;
			}

			//if (child.target == null){
			//	Debug.LogWarning("Child's target should not be null <" + child + ">");
			//	return false;
			//}
			return true;
		}
#endif

		public virtual void DrawChildren(int from, int to, ref Rect r, ref int current)
		{
			for (var i = 0; i < children.Count; i++)
			{
				children[i].Draw(from, to, ref r, ref current);
				if (current >= to)
				{
					return;
				}
			}
		}

		public virtual void Draw(int from, int to, ref Rect r, ref int current)
		{
			if (current >= to)
			{
				Debug.LogWarning("Out of view " + current + ":" + from + ":" + to + ":" + this);
				return; //finish drawing
			}

			int n = _nFakeSize + 1;
			int h = _itemHeight * n;

			if (current + n >= from)
			{
				//partially / fully visible ? just draw

				if (children.Count > 0)
				{
					// DrawTexture expand / collapse arrow
					Rect arrowRect = r;
					arrowRect.width = arrowWidth;
					EditorGUI.BeginChangeCheck();

					if (foldStyle == null)
					{
						foldStyle = "IN Foldout";
					}

					GUI.Toggle(arrowRect, _expand, GUIContent.none, foldStyle);

					if (EditorGUI.EndChangeCheck())
					{
						ToggleExpand();
					}
				}

				Rect drawRect = r;
				drawRect.x += arrowWidth;
				drawRect.width -= arrowWidth;
				drawRect.height = h;
				Draw(drawRect);
			}

			current += n;
			r.y += h;

			if (!_expand || current >= to)
			{
				return;
			}

			r.x += arrowWidth;
			r.width -= arrowWidth;

			for (var i = 0; i < children.Count; i++)
			{
				children[i].Draw(from, to, ref r, ref current);
				if (current >= to)
				{
					return;
				}
			}

			r.x -= arrowWidth;
			r.width += arrowWidth;
		}

		protected virtual void Draw(Rect r) { }
	}
}