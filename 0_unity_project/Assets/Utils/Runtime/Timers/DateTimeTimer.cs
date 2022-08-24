using System;
using UnityEngine;

namespace App.Core.Tools
{
    /// <summary>
    /// Use it within using statement new DateTimeTimer("message")
    /// </summary>
    public class DateTimeTimer : IDisposable
    {
        private DateTime _timeWas;
        private TimeSpan? AdditionalTimeSPan = null;
        private readonly string _message;

        public DateTimeTimer(string messageToUse)
        {
            _timeWas = DateTime.UtcNow;
            _message = messageToUse;
        }

        public void Pause()
        {
            if (AdditionalTimeSPan == null)
            {
                AdditionalTimeSPan = (DateTime.UtcNow - _timeWas);
            }
            else
            {
                AdditionalTimeSPan = (DateTime.UtcNow - _timeWas) + AdditionalTimeSPan.Value;
            }
            var totalMinutes = (int) AdditionalTimeSPan.Value.TotalMinutes;
            var totalSeconds = (float) AdditionalTimeSPan.Value.TotalSeconds;
            var totalMs = (long) AdditionalTimeSPan.Value.TotalMilliseconds;
            Debug.LogWarning($"TIMER Paused {_message} m:{totalMinutes} s:{totalSeconds.ToString("F")} ms:{totalMs}");
        }

        public void Resume()
        {
            _timeWas = DateTime.UtcNow;
        }

        public void Dispose()
        {
            var timeSpan = (DateTime.UtcNow - _timeWas);
            if (AdditionalTimeSPan != null)
            {
                timeSpan += AdditionalTimeSPan.Value;
            }

            var totalMinutes = (int) timeSpan.TotalMinutes;
            var totalSeconds = (float) timeSpan.TotalSeconds;
            var totalMs = (long) timeSpan.TotalMilliseconds;
            Debug.LogWarning($"TIMER {_message} m:{totalMinutes} s:{totalSeconds.ToString("F")} ms:{totalMs}");
        }
    }
}