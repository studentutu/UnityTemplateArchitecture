using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace App.Core.Tools
{
    public static class UnityTimers
    {
        private const string TIMERS_PARENT = "[Timers]";
        private const string TIMER = "[Timer] ";

        private static GameObject _parentTimers;

        private static GameObject parentTimers
        {
            get
            {
                if (_parentTimers == null)
                {
                    _parentTimers = new GameObject(TIMERS_PARENT);
                    GameObjectExtensions.DontDestroyOnLoad(_parentTimers);
                }

                return _parentTimers;
            }
        }

        private static Dictionary<string, UnityTimer> activeTimers = new Dictionary<string, UnityTimer>();

        #region TIMERS MANAGE

        public static bool IsTimer(string id)
        {
            return activeTimers.ContainsKey(id);
        }

        public static UnityTimer GetTimer(string id)
        {
            if (activeTimers.ContainsKey(id)) return activeTimers[id];

            return null;
        }

        public static UnityTimer AddNewTimer(string id, long startTime, long endTime, Action<UnityTimer> OnComplete = null,
            Action<long> OnUpdate = null)
        {
            // Debug.Log(TIMERS_PARENT + " Add New Timer id = " + id + ", time = " + (endTime - startTime) + ", startTime" + startTime);
            if (!activeTimers.ContainsKey(id))
            {
                GameObject timer_go = new GameObject(TIMER + id);
                timer_go.transform.SetParent(parentTimers.transform, false);
                UnityTimer unityTimer = timer_go.AddComponent<UnityTimer>();
                unityTimer.Init(id);
                activeTimers.Add(id, unityTimer);
            }

            activeTimers[id].RestartTimer(startTime, endTime);
            if (OnComplete != null)
            {
                activeTimers[id].OnComplete += OnComplete;
            }

            if (OnUpdate != null)
            {
                activeTimers[id].OnUpdate += OnUpdate;
            }

            return activeTimers[id];
        }

        public static long AddTimeTo(long currentSecond, TimeSpan addedTime)
        {
            return currentSecond + (long) addedTime.TotalSeconds;
        }

        public static bool RemoveTimer(string id)
        {
            if (activeTimers.ContainsKey(id))
            {
                if (!activeTimers[id].IsComplete)
                {
                    activeTimers[id].Stop();
                }

                activeTimers.Remove(id);
                return true;
            }

            return false;
        }

        #endregion

        #region TIME AND DATE FORMATS

        public static void Example()
        {
            var getTime1 = GetNowTimestampSeconds();

            var getDiff = GetNowTimestampSeconds() - getTime1;
        }

        public static long GetNowTimestampTicks()
        {
            return (DateTime.UtcNow.Ticks - new DateTime(1970, 1, 1, 0, 0, 0).Ticks);
        }

        public static long GetNowTimestampSeconds()
        {
            return (long) (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }

        public static long GetNowTimestampMilliSeconds()
        {
            return (long) (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
        }

        public static string SecondsToHHMMSS(int seconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            return timeSpan.Hours > 0
                ? string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds)
                : string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
        }

        public static string SecondsToHHMMSS(long seconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            return timeSpan.Hours > 0
                ? string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds)
                : string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
        }

        #endregion
    }
}