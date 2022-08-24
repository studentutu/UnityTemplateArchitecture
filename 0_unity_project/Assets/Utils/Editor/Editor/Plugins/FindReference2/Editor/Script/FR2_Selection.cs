using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace vietlabs.fr2
{
	public class FR2_Selection
	{
		public static HashSet<string> h = new HashSet<string>();

		public static HashSet<Object> HScene = new HashSet<Object>();
		private static bool dirty;
		private static HashSet<string> __cache;

		private static HashSet<string> cache
		{
			get
			{
				if (__cache == null || dirty)
				{
					dirty = false;
					__cache = new HashSet<string>();

					foreach (string item in h)
					{
						__cache.Add(item);
					}

					foreach (Object item in HScene)
					{
						__cache.Add(item.GetInstanceID().ToString());
					}
				}

				return __cache;
			}
		}

		public static int SelectionCount
		{
			get { return h.Count + HScene.Count; }
		}

		public static void Commit()
		{
			var list = new List<Object>();
			foreach (string guid in h)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				// var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
				Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
				list.Add(obj);
			}

			foreach (Object obj in HScene)
			{
				list.Add(obj);
			}

			Selection.objects = list.ToArray();
		}


		public static bool IsSelect(string guid)
		{
			return cache.Contains(guid);

			//return h.Contains(guid);
		}

		public static void RemoveSelection(string guid)
		{
			if (!h.Remove(guid))
			{
				dirty = true;
				HScene.RemoveWhere(x => x.GetInstanceID().ToString() == guid);
			}
		}

		private static bool IsSelect(Object obj)
		{
			return HScene.Contains(obj);
		}

		public static void ClearSelection()
		{
			dirty = true;
			h.Clear();
			HScene.Clear();
		}

		public static void AppendSelection(string guid)
		{
			if (IsSelect(guid))
			{
				return;
			}

			dirty = true;
			h.Add(guid);
		}

		public static void AppendSelection(FR2_Ref rf)
		{
			if (rf.isSceneRef)
			{
				HScene.Add(rf.component);
			}
			else
			{
				h.Add(rf.asset.guid);
			}

			dirty = true;
		}

		public static void RemoveSelection(FR2_Ref rf)
		{
			if (rf.isSceneRef)
			{
				HScene.Remove(rf.component);
			}
			else
			{
				h.Remove(rf.asset.guid);
			}

			dirty = true;
		}
	}
}