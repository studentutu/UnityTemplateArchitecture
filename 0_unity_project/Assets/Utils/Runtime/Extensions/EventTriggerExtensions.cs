using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace App.Core.Extensions
{
    [Serializable]
    public class UnityEventTriggerSubscriber
    {
        [SerializeField] private EventTrigger _trigger;
        [SerializeField] private EventTriggerType _type;

        public EventTrigger Target => _trigger;

        public UnityEventTriggerSubscriber()
        {
        }

        public UnityEventTriggerSubscriber(EventTriggerType type)
        {
            _type = type;
        }

        public void AddListener(UnityAction<BaseEventData> call)
        {
            if (_trigger == null)
            {
                return;
            }

            RemoveListener(call);
            _trigger.AddListener(_type, call);
        }

        public void RemoveListener(UnityAction<BaseEventData> call)
        {
            if (_trigger == null)
            {
                return;
            }
            _trigger.RemoveListener(_type, call);
        }
    }

    public static class EventTriggerExtensions
    {
        public static void AddListener(
            this EventTrigger eventTrigger,
            EventTriggerType triggerType,
            UnityAction<BaseEventData> call)
        {
            if (eventTrigger == null)
            {
                throw new ArgumentNullException(nameof(eventTrigger));
            }

            if (call == null)
            {
                throw new ArgumentNullException(nameof(call));
            }

            EventTrigger.Entry entry = eventTrigger.triggers
                .Find(e => e.eventID == triggerType);
            if (entry == null)
            {
                entry = new EventTrigger.Entry();
                entry.eventID = triggerType;
                eventTrigger.triggers.Add(entry);
            }

            entry.callback.AddListener(call);
        }


        public static void RemoveListener(
            this EventTrigger eventTrigger,
            EventTriggerType triggerType,
            UnityAction<BaseEventData> call)
        {
            if (eventTrigger == null)
            {
                throw new ArgumentNullException(nameof(eventTrigger));
            }

            if (call == null)
            {
                throw new ArgumentNullException(nameof(call));
            }

            EventTrigger.Entry entry = eventTrigger.triggers
                .Find(e => e.eventID == triggerType);
            if (entry != null)
            {
                entry.callback.RemoveListener(call);
            }
        }
    }
}