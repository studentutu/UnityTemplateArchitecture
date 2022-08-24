using System;
using System.Collections.Generic;
using UniRx;
using App.Core.Extensions.UniRx;

public static class MessageBrokerExtension
{
    public static BufferedMessagebroker Buffered(this IMessageBroker otherBrokers)
    {
        return BufferedMessagebroker.BufferedBrokerInstance;
    }
}

namespace App.Core.Extensions.UniRx
{
    public class BufferedMessagebroker : IMessageBroker, IDisposable
    {
        internal static BufferedMessagebroker BufferedBrokerInstance = new BufferedMessagebroker();

        bool isDisposed = false;
        readonly Dictionary<Type, object> notifiers = new Dictionary<Type, object>();
        private readonly Dictionary<Type, object> lastValue = new Dictionary<Type, object>();

        
        public void Publish<T>(T message)
        {
            object notifier;
            var typeToLook = typeof(T);
            lock (notifiers)
            {
                if (isDisposed) return;
                if (!lastValue.ContainsKey(typeToLook))
                {
                    lastValue.Add(typeToLook, message);
                }

                lastValue[typeToLook] = message;

                if (!notifiers.TryGetValue(typeof(T), out notifier))
                {
                    return;
                }
            }

            ((ISubject<T>) notifier).OnNext(message);
        }

        public IDisposable ReceiveBuffered<T>(Action<T> onNext)
        {
            if (isDisposed)
            {
                return Disposable.Empty;
            }

            var disposed = ((IMessageReceiver) this).Receive<T>().Subscribe(onNext);
            if (lastValue.TryGetValue(typeof(T), out var value))
            {
                // raise latest value on subscribe
                onNext((T) value);
            }

            return disposed;
        }

        IObservable<T> IMessageReceiver.Receive<T>()
        {
            object notifier;
            var typeToLook = typeof(T);
            lock (notifiers)
            {
                if (isDisposed) throw new ObjectDisposedException("BufferedMessageBroker");

                if (!notifiers.TryGetValue(typeToLook, out notifier))
                {
                    ISubject<T> n = new Subject<T>().Synchronize();
                    notifier = n;
                    notifiers.Add(typeToLook, notifier);
                }
            }

            return ((IObservable<T>) notifier).AsObservable();
        }

        public void ClearBuffer<T>()
        {
            lastValue.Remove(typeof(T));
        }

        public void Dispose()
        {
            lock (notifiers)
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    notifiers.Clear();
                }
            }
        }

        public void ClearAllBuffer()
        {
            lastValue.Clear();
            notifiers.Clear();
            var broker = MessageBroker.Default as MessageBroker;
            broker?.Dispose();
        }
    }
}