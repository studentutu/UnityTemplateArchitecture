using System.Collections.Generic;
using App.Core.CommonPatterns;
using App.Core.Tools;
using JetBrains.Annotations;
using UniRx;

namespace App.Core
{
    /// <summary>
    /// Receives RuntimeCullingItem and whether or not it needs to be added or removed
    /// </summary>
    [UsedImplicitly]
    public class SubScribeToRuntimeUpdateCulling : EventBus<RuntimeCullingItem, bool>
    {
        public override void Init()
        {
        }
    }

    [ExecutionOrder(-103)]
    public class RuntimeCullingUpdater : Singleton<RuntimeCullingUpdater>
    {
        private readonly List<RuntimeCullingItem> _allItems = new List<RuntimeCullingItem>();
        private RuntimeCullingItem _tempData;

        protected override void InitInstance()
        {
            EventBus.Subscription<SubScribeToRuntimeUpdateCulling>()
                .Add(OnChangeListWithItem);
        }

        private void OnChangeListWithItem(RuntimeCullingItem itemToAdd, bool addToList)
        {
            ClearUpNulls();
            if (itemToAdd == null)
            {
                return;
            }

            if (addToList)
            {
                AddToList(itemToAdd);
            }
            else
            {
                RemoveFromList(itemToAdd);
            }
        }

        private void AddToList(RuntimeCullingItem itemToAdd)
        {
            if (_allItems.Contains(itemToAdd))
            {
                return;
            }

            _allItems.Add(itemToAdd);
        }

        private void RemoveFromList(RuntimeCullingItem itemToRemove)
        {
            _allItems.Remove(itemToRemove);
        }

        private void ClearUpNulls()
        {
            var toArray = _allItems.ToArray();
            _allItems.Clear();
            for (var i = 0; i < toArray.Length; i++)
            {
                if (toArray[i] != null)
                {
                    _allItems.Add(toArray[i]);
                }
            }
        }

        private void OnEnable()
        {
            Observable.EveryGameObjectUpdate()
                .TakeUntilDisable(this)
                .Subscribe(OnUpdateHandler);
        }

        private void OnUpdateHandler(long time)
        {
            var takeEach = time % 3;
            if (takeEach == 0)
            {
                CheckEachRuntimeItem();
            }
        }

        private void CheckEachRuntimeItem()
        {
            for (var i = 0; i < _allItems.Count; i++)
            {
                _tempData = _allItems[i];
                if (_tempData != null)
                {
                    if (_tempData.IsVisibleCustom())
                    {
                        _tempData.OnNeedsToBeEnabled();
                    }
                    else
                    {
                        _tempData.OnNeedsToBeDisabled();
                    }
                }
            }

            _tempData = null;
        }
    }
}