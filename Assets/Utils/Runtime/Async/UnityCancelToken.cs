using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace App.Core.Tools
{
    /// <summary>
    /// Define the cancellation token.
    /// </summary>
    public class UnityCancelToken
    {
        private readonly CancellationTokenSource _asASource;
        private static TaskScheduler _unityScheduler;

        public CancellationToken Token
        {
            get
            {
                return AsASource().Token;
            }
        }

        public TaskScheduler UnityScheduler
        {
            get
            {
                if (_unityScheduler == null)
                {
                    // UniTask (does not have TaskScheduler, but has a UnitySynchronizationContext) PlayerLoopHelper.UnitySynchronizationContext;
                    // the main thread id and TaskScheduler.Default.id are the same!
                    // from currentSynchronizationContext() will create new one (different id)!
                    _unityScheduler = TaskScheduler.Default; 
                }
                return _unityScheduler;
            }
        }

        public UnityCancelToken()
        {
            _asASource = new CancellationTokenSource();
            if (UnityScheduler == null)
            {
                Debug.LogError("Can't initialize UnityScheduler");
            }
        }
        
        /// <summary>
        /// Do not use it directly!
        /// </summary>
        public CancellationTokenSource AsASource()
        {
            return _asASource;
        }

        public void Cancel()
        {
            AsASource().Cancel();
        }
    }
}