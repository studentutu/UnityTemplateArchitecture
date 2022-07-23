/*
 * License MIT
 * Author baba-s
 * link : https://github.com/baba-s/UniMessageBus
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Core.CommonPatterns
{
    /// <summary>
    /// Classes that manage message buses.
    /// Usage : create custom message bus.
    /// Subscribe to the events via EventBus.Get<CustomBus>.Add().Remove();
    /// </summary>
    public partial class EventBus
    {
        private static Dictionary<Type, IEventBus> m_table;

        public static void ResetAll()
        {
            m_table = null;
        }

        /// <summary>
        /// Returns the message bus
        /// </summary>
        public static T Subscription<T>() where T : IEventBus
        {
            // Create a table the first time you use the message bus returns
            if (m_table == null)
            {
                m_table = new Dictionary<Type, IEventBus>();
            }

            var type = typeof(T);

            // Returns the registered bus if the bus is registered in the table
            if (m_table.TryGetValue(type, out var messageBus))
            {
                return (T) messageBus;
            }

            // If not, create a bus, register it in the table, and then return it
            messageBus = (IEventBus) Activator.CreateInstance(type);
            m_table.Add(type, messageBus);
            messageBus.Init();

            return (T) messageBus;
        }
    }

    /// <inheritdoc/>
    public abstract partial class EventBus : IEventBus
    {
        private Action m_callback;

        /// <summary>
        /// Register a callback that is called when a message is received
        /// </summary>
        public EventBus Add(Action callback)
        {
            m_callback += callback;
            return this;
        }

        /// <summary>
        /// Releases a callback that is called when a message is received
        /// </summary>
        public EventBus Remove(Action callback)
        {
            m_callback -= callback;
            return this;
        }

        /// <summary>
        /// Send a message
        /// </summary>
        public void Send()
        {
#if UNITY_EDITOR
            if (m_callback == null ||
                m_callback.GetInvocationList() != null && m_callback.GetInvocationList().Length == 0)
            {
                Debug.LogWarning($"Nobody is listening to the event {this.GetType()}");
            }
#endif
            m_callback?.Invoke();
        }

        public abstract void Init();
    }

    /// <inheritdoc/>
    public abstract class EventBus<T> : IEventBus
    {
        private Action<T> m_callback;

        /// <summary>
        /// Register a callback that is called when a message is received
        /// </summary>
        public EventBus<T> Add(Action<T> callback)
        {
            m_callback += callback;
            return this;
        }

        /// <summary>
        /// Releases a callback that is called when a message is received
        /// </summary>
        public EventBus<T> Remove(Action<T> callback)
        {
            m_callback -= callback;
            return this;
        }

        /// <summary>
        /// Send a message
        /// </summary>
        public void Send(T arg1)
        {
#if UNITY_EDITOR
            if (m_callback == null ||
                m_callback.GetInvocationList() != null && m_callback.GetInvocationList().Length == 0)
            {
                Debug.LogWarning($"Nobody is listening to the event {this.GetType()}");
            }
#endif
            m_callback?.Invoke(arg1);
        }

        public abstract void Init();
    }

    /// <inheritdoc/>
    public abstract class EventBus<T1, T2> : IEventBus
    {
        private Action<T1, T2> m_callback;

        /// <summary>
        /// Register a callback that is called when a message is received
        /// </summary>
        public EventBus<T1, T2> Add(Action<T1, T2> callback)
        {
            m_callback += callback;
            return this;
        }

        /// <summary>
        /// Releases a callback that is called when a message is received
        /// </summary>
        public EventBus<T1, T2> Remove(Action<T1, T2> callback)
        {
            m_callback -= callback;
            return this;
        }

        /// <summary>
        /// Send a message
        /// </summary>
        public void Send(T1 arg1, T2 arg2)
        {
#if UNITY_EDITOR
            if (m_callback == null ||
                m_callback.GetInvocationList() != null && m_callback.GetInvocationList().Length == 0)
            {
                Debug.LogWarning($"Nobody is listening to the event {this.GetType()}");
            }
#endif
            m_callback?.Invoke(arg1, arg2);
        }

        public abstract void Init();
    }

    /// <inheritdoc/>
    public abstract class EventBus<T1, T2, T3> : IEventBus
    {
        private Action<T1, T2, T3> m_callback;

        /// <summary>
        /// Register a callback that is called when a message is received
        /// </summary>
        public EventBus<T1, T2, T3> Add(Action<T1, T2, T3> callback)
        {
            m_callback += callback;
            return this;
        }

        /// <summary>
        /// Releases a callback that is called when a message is received
        /// </summary>
        public EventBus<T1, T2, T3> Remove(Action<T1, T2, T3> callback)
        {
            m_callback -= callback;
            return this;
        }

        /// <summary>
        /// Send a message
        /// </summary>
        public void Send(T1 arg1, T2 arg2, T3 arg3)
        {
#if UNITY_EDITOR
            if (m_callback == null ||
                m_callback.GetInvocationList() != null && m_callback.GetInvocationList().Length == 0)
            {
                Debug.LogWarning($"Nobody is listening to the event {this.GetType()}");
            }
#endif
            m_callback?.Invoke(arg1, arg2, arg3);
        }

        public abstract void Init();
    }

    /// <inheritdoc/>
    public abstract class EventBus<T1, T2, T3, T4> : IEventBus
    {
        private Action<T1, T2, T3, T4> m_callback;


        /// <summary>
        /// Register a callback that is called when a message is received
        /// </summary>
        public EventBus<T1, T2, T3, T4> Add(Action<T1, T2, T3, T4> callback)
        {
            m_callback += callback;
            return this;
        }

        /// <summary>
        /// Releases a callback that is called when a message is received
        /// </summary>
        public EventBus<T1, T2, T3, T4> Remove(Action<T1, T2, T3, T4> callback)
        {
            m_callback -= callback;
            return this;
        }

        /// <summary>
        /// Send a message
        /// </summary>
        public void Send(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
#if UNITY_EDITOR
            if (m_callback == null ||
                m_callback.GetInvocationList() != null && m_callback.GetInvocationList().Length == 0)
            {
                Debug.LogWarning($"Nobody is listening to the event {this.GetType()}");
            }
#endif
            m_callback?.Invoke(arg1, arg2, arg3, arg4);
        }

        public abstract void Init();
    }
}