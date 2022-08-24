using System;

namespace App.Core.Runtime
{
	public class DisposableLambda : IDisposable
	{
		private Action _onDispose;

		public DisposableLambda(System.Action onDispose)
		{
			_onDispose = onDispose;
		}

		private void ReleaseUnmanagedResources()
		{
			_onDispose?.Invoke();
			_onDispose = null;
		}

		public void Dispose()
		{
			ReleaseUnmanagedResources();
			GC.SuppressFinalize(this);
		}

		~DisposableLambda()
		{
			ReleaseUnmanagedResources();
		}
	}
}