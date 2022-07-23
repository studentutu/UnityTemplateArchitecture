using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace App.Core.Extensions
{
    public static class DelegateExtensions
    {
        public static Action<S> AsThrottledDebounce<S>(this Action<S> self, double delayInMs)
        {
            EventHandler<S> onChangedHandler = (_, value) => self(value);
            EventHandler<S> debounced = onChangedHandler.AsThrottledDebounce(delayInMs);
            return (value) => debounced(null, value);
        }

        public class EventHandlerResult<T>
        {
            public T result;
        }

        public static Func<T, R> AsThrottledDebounce<T, R>(this Func<T, R> self, double delayInMs,
            bool skipFirstEvent = false)
        {
            EventHandler<EventHandlerResult<R>> action = (input, output) => { output.result = self((T) input); };
            EventHandler<EventHandlerResult<R>> throttledAction = action.AsThrottledDebounce(delayInMs, skipFirstEvent);
            return (T input) =>
            {
                var output = new EventHandlerResult<R>();
                throttledAction(input, output);
                return output.result;
            };
        }

        /// <summary> 
        /// This will create an EventHandler where the first call is executed and the last call is executed but 
        /// every call in between that is below the passed millisecond threshold is ignored
        /// </summary>
        public static EventHandler<T> AsThrottledDebounce<T>(this EventHandler<T> self, double delayInMs,
            bool skipFirstEvent = false)
        {
            object threadLock = new object();
            T latestEventArgs;
            Stopwatch s = Stopwatch.StartNew();
            bool triggerFirstEvent = !skipFirstEvent;
            Func<object, T, Task> asyncEventHandler = async (sender, eventArgs) =>
            {
                lock (threadLock)
                {
                    latestEventArgs = eventArgs;
                    s.Restart();
                    if (triggerFirstEvent)
                    {
                        triggerFirstEvent = false;
                        self(sender, eventArgs);
                    }
                }

                var delay = Task.Delay((int) (delayInMs * 1.1f));
                await HandlerTaskResultIfNeeded(eventArgs);
                await delay;
                if (s.ElapsedMilliseconds >= delayInMs)
                {
                    lock (threadLock)
                    {
                        if (s.ElapsedMilliseconds >= delayInMs)
                        {
                            s.Reset(); // Stop (and reset) and only continue below
                            self(sender, latestEventArgs);
                            if (!skipFirstEvent)
                            {
                                triggerFirstEvent = true;
                            }
                        }
                    }

                    await HandlerTaskResultIfNeeded(latestEventArgs);
                    s.Restart();
                }
            };
            return (sender, eventArgs) =>
            {
                asyncEventHandler(sender, eventArgs).ContinueWithSameContext(finishedTask =>
                {
                    // A failed task cant be awaited but it can be logged
                    if (finishedTask.Exception != null)
                    {
                        Debug.LogException(finishedTask.Exception);
                    }
                });
            };
        }

        private static async Task HandlerTaskResultIfNeeded(object eventArgs)
        {
            if (eventArgs is EventHandlerResult<Task> t && t.result != null)
            {
                await t.result;
            }
        }

        /// <summary> Ensures that the continuation action is called on the same syncr. context </summary>
        public static Task ContinueWithSameContext(this Task self, Action<Task> continuationAction)
        {
            try
            {
                // Catch in case current sync context is not allowed to be used as a scheduler (e.g in xUnit)
                return self.ContinueWith(continuationAction, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception)
            {
                return self.ContinueWith(continuationAction);
            }
        }
    }
}