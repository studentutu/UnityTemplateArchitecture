//#define FR2_DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CBParams = System.Collections.Generic.List<System.Collections.Generic.List<string>>;
using Object = UnityEngine.Object;

namespace vietlabs.fr2
{
	//internal class FR2_DuplicateAsset : FR2_TreeItemUI
	//{
	//    public FR2_Asset asset;

	//    public FR2_DuplicateAsset(FR2_Asset asset)
	//    {
	//        this.asset = asset;
	//    }

	//    protected override void Draw(Rect r)
	//    {
	//        var drawR = r;
	//        drawR.xMin -= 16f;
	//        asset.Draw(drawR, false, true, window);

	//        var bRect = r;
	//        bRect.xMin += bRect.width - 50f;
	//        if (GUI.Button(bRect, "Use", EditorStyles.miniButton))
	//        {
	//            EditorGUIUtility.systemCopyBuffer = asset.guid;
	//            Debug.Log("guid: " + asset.guid + "  systemCopyBuffer " + EditorGUIUtility.systemCopyBuffer);
	//            Selection.objects = (parent as FR2_DuplicateFolder).children.Select(
	//                a => FR2_Unity.LoadAssetAtPath<Object>(((FR2_DuplicateAsset) a).asset.assetPath)
	//                ).ToArray();
	//            FR2_Export.MergeDuplicate();
	//        }

	//        //if (GUI.Button(bRect, "Remove Others", EditorStyles.miniButton))
	//        //{
	//        //    EditorGUIUtility.systemCopyBuffer = asset.guid;
	//        //    Selection.objects = (parent as FR2_DuplicateFolder).children.Select(
	//        //        a => FR2_Unity.LoadAssetAtPath<Object>(((FR2_DuplicateAsset)a).asset.assetPath)
	//        //    ).ToArray();
	//        //    FR2_Export.MergeDuplicate();
	//        //}
	//    }
	//}

	//internal class FR2_DuplicateFolder : FR2_TreeItemUI
	//{
	//    private static FR2_FileCompare comparer;
	//    public string assetPath;
	//    public string count;
	//    public string filesize;
	//    public string label;

	//    public FR2_DuplicateFolder(List<string> list)
	//    {
	//        list.Sort((item1, item2) => { return item1.CompareTo(item2); });

	//        var first = true;
	//        for (var i = 0; i < list.Count; i++)
	//        {
	//            var item = list[i];
	//            var asset = FR2_Cache.Api.Get(AssetDatabase.AssetPathToGUID(item));

	//            if (asset == null)
	//            {
	//                Debug.LogWarning("Something wrong, asset not found <" + item + ">");
	//                continue;
	//            }

	//            if (first)
	//            {
	//                first = false;
	//                label = asset.assetName;
	//                count = list.Count.ToString();
	//                filesize = GetfileSizeString(asset.fileSize);
	//                assetPath = item;
	//            }

	//            AddChild(new FR2_DuplicateAsset(asset));
	//        }
	//    }

	//    private string GetfileSizeString(long fileSize)
	//    {
	//        return fileSize <= 1024
	//            ? fileSize + " B"
	//            : fileSize <= 1024*1024
	//                ? Mathf.RoundToInt(fileSize/1024f) + " KB"
	//                : Mathf.RoundToInt(fileSize/1024f/1024f) + " MB";
	//    }

	//    protected override void Draw(Rect r)
	//    {
	//        var tex = AssetDatabase.GetCachedIcon(assetPath);
	//        var rect = r;

	//        if (tex != null)
	//        {
	//            rect.width = 16f;
	//            GUI.DrawTexture(rect, tex);
	//        }

	//        rect = r;
	//        rect.xMin += 16f;
	//        GUI.Label(rect, label, EditorStyles.boldLabel);

	//        rect = r;
	//        rect.xMin += rect.width - 50f;
	//        GUI.Label(rect, filesize, EditorStyles.miniLabel);

	//        rect = r;
	//        rect.xMin += rect.width - 70f;
	//        GUI.Label(rect, count, EditorStyles.miniLabel);

	//        rect = r;
	//        rect.xMin += rect.width - 70f;
	//    }
	//}
	internal class FR2_DuplicateTree2 : IRefDraw
	{
		private const float TimeDelayDelete = .5f;

		private static readonly FR2_FileCompare fc = new FR2_FileCompare();
		private readonly FR2_TreeUI2.GroupDrawer groupDrawer;
		private CBParams cacheAssetList;
		private bool caseSensitive;
		private Dictionary<string, List<FR2_Ref>> dicIndex; //index, list

		private bool dirty;
		private int excludeCount;
		private string guidPressDelete;
		private List<FR2_Ref> list;
		private Dictionary<string, FR2_Ref> refs;
		public int scanExcludeByIgnoreCount;
		public int scanExcludeByTypeCount;
		private string searchTerm = "";
		private bool showFilter;
		private bool showIgnore;
		private float TimePressDelete;

		public FR2_DuplicateTree2(IWindow window)
		{
			this.window = window;
			groupDrawer = new FR2_TreeUI2.GroupDrawer(DrawGroup, DrawAsset);
		}

		public IWindow window { get; set; }


		public bool Draw()
		{
			if (dirty)
			{
				RefreshView(cacheAssetList);
			}


			if (fc.nChunks2 > 0 && fc.nScaned < fc.nChunks2)
			{
				FR2_Cache api = FR2_Cache.Api;
				float w = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 70f;
				api.priority = EditorGUILayout.IntSlider("Priority", api.priority, 0, 5);
				EditorGUIUtility.labelWidth = w;

				Rect rect = GUILayoutUtility.GetRect(1, Screen.width, 18f, 18f);
				float p = fc.nScaned / (float) fc.nChunks2;

				EditorGUI.ProgressBar(rect, p, string.Format("Scanning {0} / {1}", fc.nScaned, fc.nChunks2));
				GUILayout.FlexibleSpace();
				return true;
			}

			DrawHeader();
			groupDrawer.Draw();
			return false;
		}

		public int ElementCount()
		{
			return list == null ? 0 : list.Count;
		}

		private void DrawAsset(Rect r, string guid)
		{
			FR2_Ref rf;
			if (!refs.TryGetValue(guid, out rf))
			{
				return;
			}

			if (rf.depth == 1) //mode != Mode.Dependency && 
			{
				Color c = GUI.color;
				GUI.color = Color.blue;
				GUI.DrawTexture(new Rect(r.x - 4f, r.y + 2f, 2f, 2f), EditorGUIUtility.whiteTexture);
				GUI.color = c;
			}

			rf.asset.Draw(r, false, FR2_Setting.GroupMode != FR2_RefDrawer.Mode.Folder, window, false);

			Texture tex = AssetDatabase.GetCachedIcon(rf.asset.assetPath);
			if (tex == null)
			{
				return;
			}

			Rect drawR = r;
			drawR.x = drawR.x + drawR.width - 60f; // (groupDrawer.TreeNoScroll() ? 60f : 70f) ;
			drawR.width = 40f;
			drawR.y += 1;
			drawR.height -= 2;

			if (GUI.Button(drawR, "Use", EditorStyles.miniButton))
			{
				if (FR2_Export.IsMergeProcessing)
				{
					Debug.LogWarning("Previous merge is processing");
				}
				else
				{
					AssetDatabase.SaveAssets();
					EditorGUIUtility.systemCopyBuffer = rf.asset.guid;
					EditorGUIUtility.systemCopyBuffer = rf.asset.guid;
					// Debug.Log("guid: " + rf.asset.guid + "  systemCopyBuffer " + EditorGUIUtility.systemCopyBuffer);
					int index = rf.index;
					Selection.objects = list.Where(x => x.index == index)
						.Select(x => FR2_Unity.LoadAssetAtPath<Object>(x.asset.assetPath)).ToArray();
					FR2_Export.MergeDuplicate();
				}
			}

			if (rf.asset.UsageCount() > 0)
			{
				return;
			}

			drawR.x -= 25;
			drawR.width = 20;
			if (wasPreDelete(guid))
			{
				Color col = GUI.color;
				GUI.color = Color.red;
				if (GUI.Button(drawR, "X", EditorStyles.miniButton))
				{
					guidPressDelete = null;
					AssetDatabase.DeleteAsset(rf.asset.assetPath);
				}

				GUI.color = col;
				window.WillRepaint = true;
			}
			else
			{
				if (GUI.Button(drawR, "X", EditorStyles.miniButton))
				{
					guidPressDelete = guid;
					TimePressDelete = Time.realtimeSinceStartup;
					window.WillRepaint = true;
				}
			}
		}

		private bool wasPreDelete(string guid)
		{
			if (guidPressDelete == null || guid != guidPressDelete)
			{
				return false;
			}

			if (Time.realtimeSinceStartup - TimePressDelete < TimeDelayDelete)
			{
				return true;
			}

			guidPressDelete = null;
			return false;
		}

		private void DrawGroup(Rect r, string label, int childCount)
		{
			// GUI.Label(r, label + " (" + childCount + ")", EditorStyles.boldLabel);
			FR2_Asset asset = dicIndex[label][0].asset;

			Texture tex = AssetDatabase.GetCachedIcon(asset.assetPath);
			Rect rect = r;

			if (tex != null)
			{
				rect.width = 16f;
				GUI.DrawTexture(rect, tex);
			}

			rect = r;
			rect.xMin += 16f;
			GUI.Label(rect, asset.assetName, EditorStyles.boldLabel);

			rect = r;
			rect.xMin += rect.width - 50f;
			GUI.Label(rect, FR2_Helper.GetfileSizeString(asset.fileSize), EditorStyles.miniLabel);

			rect = r;
			rect.xMin += rect.width - 70f;
			GUI.Label(rect, childCount.ToString(), EditorStyles.miniLabel);

			rect = r;
			rect.xMin += rect.width - 70f;
		}


		// private List<FR2_DuplicateFolder> duplicated;

		public void Reset(CBParams assetList)
		{
			fc.Reset(assetList, OnUpdateView, RefreshView);
		}

		private void OnUpdateView(CBParams assetList) { }

		public bool isExclueAnyItem()
		{
			return excludeCount > 0 || scanExcludeByTypeCount > 0;
		}

		public bool isExclueAnyItemByIgnoreFolder()
		{
			return scanExcludeByIgnoreCount > 0;
		}

		// void OnActive
		private void RefreshView(CBParams assetList)
		{
			cacheAssetList = assetList;
			dirty = false;
			list = new List<FR2_Ref>();
			refs = new Dictionary<string, FR2_Ref>();
			dicIndex = new Dictionary<string, List<FR2_Ref>>();
			if (assetList == null)
			{
				return;
			}

			int minScore = searchTerm.Length;
			string term1 = searchTerm;
			if (!caseSensitive)
			{
				term1 = term1.ToLower();
			}

			string term2 = term1.Replace(" ", string.Empty);
			excludeCount = 0;

			for (var i = 0; i < assetList.Count; i++)
			{
				var lst = new List<FR2_Ref>();
				for (var j = 0; j < assetList[i].Count; j++)
				{
					string guid = AssetDatabase.AssetPathToGUID(assetList[i][j]);
					if (string.IsNullOrEmpty(guid))
					{
						continue;
					}

					if (refs.ContainsKey(guid))
					{
						continue;
					}

					FR2_Asset asset = FR2_Cache.Api.Get(guid);
					if (asset == null)
					{
						continue;
					}

					var fr2 = new FR2_Ref(i, 0, asset, null);

					if (FR2_Setting.IsTypeExcluded(fr2.type))
					{
						excludeCount++;
						continue; //skip this one
					}

					if (string.IsNullOrEmpty(searchTerm))
					{
						fr2.matchingScore = 0;
						list.Add(fr2);
						lst.Add(fr2);
						refs.Add(guid, fr2);
						continue;
					}

					//calculate matching score
					string name1 = fr2.asset.assetName;
					if (!caseSensitive)
					{
						name1 = name1.ToLower();
					}

					string name2 = name1.Replace(" ", string.Empty);

					int score1 = FR2_Unity.StringMatch(term1, name1);
					int score2 = FR2_Unity.StringMatch(term2, name2);

					fr2.matchingScore = Mathf.Max(score1, score2);
					if (fr2.matchingScore > minScore)
					{
						list.Add(fr2);
						lst.Add(fr2);
						refs.Add(guid, fr2);
					}
				}

				dicIndex.Add(i.ToString(), lst);
			}

			ResetGroup();
		}

		private void ResetGroup()
		{
			//folderDrawer.GroupByAssetType(list);
			groupDrawer.Reset(list,
				rf => rf.asset.guid
				, GetGroup, SortGroup);
			if (window != null)
			{
				window.Repaint();
			}
		}

		private string GetGroup(FR2_Ref rf)
		{
			return rf.index.ToString();
		}

		private void SortGroup(List<string> groups)
		{
			// groups.Sort( (item1, item2) =>
			// {
			// 	if (item1 == "Others" || item2 == "Selection") return 1;
			// 	if (item2 == "Others" || item1 == "Selection") return -1;
			// 	return item1.CompareTo(item2);
			// });
		}

		public void SetDirty()
		{
			dirty = true;
		}

		public void RefreshSort() { }

		private void DrawHeader()
		{
//draw filter
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				DrawSearch();
				GUILayout.Space(5);
				Color col = GUI.color;
				GUI.color = FR2_Cache.Api.setting.ScanColor;
				if (GUILayout.Button("Scan", EditorStyles.toolbarButton, GUILayout.Width(60)))
				{
					FR2_Cache.onReady -= OnCacheReady;
					FR2_Cache.onReady += OnCacheReady;
					FR2_Cache.Api.Check4Changes(true, true);
				}

				GUI.color = col;
			}
			EditorGUILayout.EndHorizontal();
		}

		private void OnCacheReady()
		{
			scanExcludeByTypeCount = 0;
			Reset(FR2_Cache.Api.ScanSimilar(IgnoreTypeWhenScan, IgnoreFolderWhenScan));
			FR2_Cache.onReady -= OnCacheReady;
		}

		private void IgnoreTypeWhenScan()
		{
			scanExcludeByTypeCount++;
		}

		private void IgnoreFolderWhenScan()
		{
			scanExcludeByIgnoreCount++;
		}

		private void DrawSearch(string searchLable = "")
		{
			if (FR2_RefDrawer.toolbarSearchField == null)
			{
				FR2_RefDrawer.InitSearchStyle();
			}

			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				bool v = GUILayout.Toggle(caseSensitive, "Aa", EditorStyles.toolbarButton, GUILayout.Width(24f));
				if (v != caseSensitive)
				{
					caseSensitive = v;
					dirty = true;
				}

				GUILayout.Space(2f);
				string value = GUILayout.TextField(searchTerm, FR2_RefDrawer.toolbarSearchField);
				if (searchTerm != value)
				{
					searchTerm = value;
					dirty = true;
				}

				GUIStyle style = string.IsNullOrEmpty(searchTerm)
					? FR2_RefDrawer.toolbarSearchFieldCancelButtonEmpty
					: FR2_RefDrawer.toolbarSearchFieldCancelButton;
				if (GUILayout.Button("Cancel", style))
				{
					searchTerm = string.Empty;
					dirty = true;
				}

				GUILayout.Space(2f);
			}
			GUILayout.EndHorizontal();
		}
	}

	internal class FR2_FileCompare
	{
		public static HashSet<FR2_Chunk> HashChunksNotComplete;
		internal static int streamClosedCount;
		private CBParams cacheList;
		public List<FR2_Head> deads = new List<FR2_Head>();
		public List<FR2_Head> heads = new List<FR2_Head>();

		public int nChunks;
		public int nChunks2;
		public int nScaned;
		public Action<CBParams> OnCompareComplete;

		public Action<CBParams> OnCompareUpdate;
		private int streamCount;

		public void Reset(CBParams list, Action<CBParams> onUpdate, Action<CBParams> onComplete)
		{
			nChunks = 0;
			nScaned = 0;
			nChunks2 = 0;
			streamCount = streamClosedCount = 0;
			HashChunksNotComplete = new HashSet<FR2_Chunk>();

			if (heads.Count > 0)
			{
				for (var i = 0; i < heads.Count; i++)
				{
					heads[i].CloseChunk();
				}
			}

			deads.Clear();
			heads.Clear();

			OnCompareUpdate = onUpdate;
			OnCompareComplete = onComplete;
			if (list.Count <= 0)
			{
				OnCompareComplete(new CBParams());
				return;
			}

			cacheList = list;
			for (var i = 0; i < list.Count; i++)
			{
				var file = new FileInfo(list[i][0]);
				int nChunk = Mathf.CeilToInt(file.Length / (float) FR2_Head.chunkSize);
				nChunks2 += nChunk;
			}

			// for(int i =0;i< list.Count;i++)
			// {
			//     AddHead(list[i]);
			// }
			AddHead(cacheList[cacheList.Count - 1]);
			cacheList.RemoveAt(cacheList.Count - 1);

			EditorApplication.update -= ReadChunkAsync;
			EditorApplication.update += ReadChunkAsync;
		}

		public FR2_FileCompare AddHead(List<string> files)
		{
			if (files.Count < 2)
			{
				Debug.LogWarning("Something wrong ! head should not contains < 2 elements");
			}

			var chunkList = new List<FR2_Chunk>();
			for (var i = 0; i < files.Count; i++)
			{
				streamCount++;
				//  Debug.Log("new stream " + files[i]);
				chunkList.Add(new FR2_Chunk
				{
					file = files[i],
					stream = new FileStream(files[i], FileMode.Open, FileAccess.Read),
					buffer = new byte[FR2_Head.chunkSize]
				});
			}

			var file = new FileInfo(files[0]);
			int nChunk = Mathf.CeilToInt(file.Length / (float) FR2_Head.chunkSize);

			heads.Add(new FR2_Head
			{
				fileSize = file.Length,
				currentChunk = 0,
				nChunk = nChunk,
				chunkList = chunkList
			});

			nChunks += nChunk;

			return this;
		}

		private bool checkCompleteAllCurFile()
		{
			return streamClosedCount + HashChunksNotComplete.Count >= streamCount; //-1 for safe
		}

		private void ReadChunkAsync()
		{
			bool alive = ReadChunk();
			HashChunksNotComplete.RemoveWhere(x => x.stream == null || !x.stream.CanRead);
			if (cacheList.Count > 0 && checkCompleteAllCurFile()) //complete all chunk
			{
				int numCall = FR2_Cache.Api.priority; // - 2;
				if (numCall <= 0)
				{
					numCall = 1;
				}

				for (var i = 0; i < numCall; i++)
				{
					if (cacheList.Count <= 0)
					{
						break;
					}

					AddHead(cacheList[cacheList.Count - 1]);
					cacheList.RemoveAt(cacheList.Count - 1);
				}
			}

			var update = false;

			for (int i = heads.Count - 1; i >= 0; i--)
			{
				FR2_Head h = heads[i];
				if (h.isDead)
				{
					h.CloseChunk();
					heads.RemoveAt(i);
					if (h.chunkList.Count > 1)
					{
						update = true;
						deads.Add(h);
					}
				}
			}

			if (update)
			{
				Trigger(OnCompareUpdate);
			}

			if (!alive && cacheList.Count <= 0 && checkCompleteAllCurFile()
			) //&& cacheList.Count <= 0 complete all chunk and cache list empty
			{
				foreach (FR2_Chunk item in HashChunksNotComplete)
				{
					if (item.stream != null && item.stream.CanRead)
					{
						item.stream.Close();
						item.stream = null;
					}
				}

				HashChunksNotComplete.Clear();
				// Debug.Log("complete ");
				nScaned = nChunks;
				EditorApplication.update -= ReadChunkAsync;
				Trigger(OnCompareComplete);
			}
		}

		private void Trigger(Action<CBParams> cb)
		{
			if (cb == null)
			{
				return;
			}

			CBParams list = deads.Select(item => item.GetFiles()).ToList();

//#if FR2_DEBUG
//        Debug.Log("Callback ! " + deads.Count + ":" + heads.Count);
//#endif
			cb(list);
		}

		private bool ReadChunk()
		{
			var alive = false;
			for (var i = 0; i < heads.Count; i++)
			{
				FR2_Head h = heads[i];
				if (h.isDead)
				{
					Debug.LogWarning("Should never be here : " + h.chunkList[0].file);
					continue;
				}

				nScaned++;
				alive = true;
				h.ReadChunk();
				h.CompareChunk(heads);
				break;
			}

			//if (!alive) return false;

			//alive = false;
			//for (var i = 0; i < heads.Count; i++)
			//{
			//    var h = heads[i];
			//    if (h.isDead) continue;

			//    h.CompareChunk(heads);
			//    alive |= !h.isDead;
			//}

			return alive;
		}
	}

	internal class FR2_Head
	{
		public const int chunkSize = 10240;

		public List<FR2_Chunk> chunkList;
		public int currentChunk;

		public long fileSize;

		public int nChunk;
		public int size; //last stream read size

		public bool isDead
		{
			get { return currentChunk == nChunk || chunkList.Count == 1; }
		}

		public List<string> GetFiles()
		{
			return chunkList.Select(item => item.file).ToList();
		}

		public void AddToDict(byte b, FR2_Chunk chunk, Dictionary<byte, List<FR2_Chunk>> dict)
		{
			List<FR2_Chunk> list;
			if (!dict.TryGetValue(b, out list))
			{
				list = new List<FR2_Chunk>();
				dict.Add(b, list);
			}

			list.Add(chunk);
		}

		public void CloseChunk()
		{
			for (var i = 0; i < chunkList.Count; i++)
			{
				// Debug.Log("stream close");
				FR2_FileCompare.streamClosedCount++;
				chunkList[i].stream.Close();
				chunkList[i].stream = null;
			}
		}

		public void ReadChunk()
		{
#if FR2_DEBUG
        if (currentChunk == 0) Debug.LogWarning("Read <" + chunkList[0].file + "> " + currentChunk + ":" + nChunk);
#endif
			if (currentChunk == nChunk)
			{
				Debug.LogWarning("Something wrong, should dead <" + isDead + ">");
				return;
			}

			int from = currentChunk * chunkSize;
			size = (int) Mathf.Min(fileSize - from, chunkSize);

			for (var i = 0; i < chunkList.Count; i++)
			{
				FR2_Chunk chunk = chunkList[i];
				chunk.size = size;
				chunk.stream.Read(chunk.buffer, 0, size);
			}

			currentChunk++;
		}

		public void CompareChunk(List<FR2_Head> heads)
		{
			int idx = chunkList.Count;
			byte[] buffer = chunkList[idx - 1].buffer;

			while (--idx >= 0)
			{
				FR2_Chunk chunk = chunkList[idx];
				int diff = FirstDifferentIndex(buffer, chunk.buffer, size);
				if (diff == -1)
				{
					continue;
				}
#if FR2_DEBUG
            Debug.Log(string.Format(
                " --> Different found at : idx={0} diff={1} size={2} chunk={3}",
            idx, diff, size, currentChunk));
#endif

				byte v = buffer[diff];
				var d = new Dictionary<byte, List<FR2_Chunk>>(); //new heads
				chunkList.RemoveAt(idx);
				FR2_FileCompare.HashChunksNotComplete.Add(chunk);

				AddToDict(chunk.buffer[diff], chunk, d);

				for (int j = idx - 1; j >= 0; j--)
				{
					FR2_Chunk tChunk = chunkList[j];
					byte tValue = tChunk.buffer[diff];
					if (tValue == v)
					{
						continue;
					}

					idx--;
					FR2_FileCompare.HashChunksNotComplete.Add(tChunk);
					chunkList.RemoveAt(j);
					AddToDict(tChunk.buffer[diff], tChunk, d);
				}

				foreach (KeyValuePair<byte, List<FR2_Chunk>> item in d)
				{
					List<FR2_Chunk> list = item.Value;
					if (list.Count == 1)
					{
#if FR2_DEBUG
                    Debug.Log(" --> Dead head found for : " + list[0].file);
#endif
					}
					else if (list.Count > 1) // 1 : dead head
					{
#if FR2_DEBUG
                    Debug.Log(" --> NEW HEAD : " + list[0].file);
#endif
						heads.Add(new FR2_Head
						{
							nChunk = nChunk,
							fileSize = fileSize,
							currentChunk = currentChunk - 1,
							chunkList = list
						});
					}
				}
			}
		}

		internal static int FirstDifferentIndex(byte[] arr1, byte[] arr2, int maxIndex)
		{
			for (var i = 0; i < maxIndex; i++)
			{
				if (arr1[i] != arr2[i])
				{
					return i;
				}
			}

			return -1;
		}
	}

	internal class FR2_Chunk
	{
		public byte[] buffer;
		public string file;
		public long size;
		public FileStream stream;
	}
}