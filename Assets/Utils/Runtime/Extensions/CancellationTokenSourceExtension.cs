using System.Threading;

namespace App.Core.Extensions
{
    public static class CancellationTokenSourceExtension
    {
        public static bool IsNullOrCancelled(this CancellationTokenSource source)
        {
            return source == null || source.IsCancellationRequested;
        }
    }
}