using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace App.Core
{
    /// <summary>
    /// Use instead of locks. Monitors are way better
    /// </summary>
    public class LockMonitor : IDisposable
    {
        private object lockObj;

        public LockMonitor(object lockObj, TimeSpan timeout)
        {
            this.lockObj = lockObj;
            if (!Monitor.TryEnter(this.lockObj, timeout))
                throw new TimeoutException();
        }

        public void Dispose()
        {
            Monitor.Exit(lockObj);
        }

        private void Example()
        {
            System.Object objectLocked = new object();
            using (new LockMonitor(objectLocked, TimeSpan.FromSeconds(1)))
            using (new LockMonitor(objectLocked, TimeSpan.FromSeconds(2)))
            using (new LockMonitor(objectLocked, TimeSpan.FromSeconds(3)))
            {
                // your code
            }
        }
    }
}