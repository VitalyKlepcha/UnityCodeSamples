using Cysharp.Threading.Tasks;
using System.Threading;

public static class UniTaskExt
{
    public delegate UniTask ActionAsync();
    public static void CancelAndDispose(this CancellationTokenSource source)
    {
        if (!source.IsCancellationRequested)
        {
            source.Cancel();
            source.Dispose();
        }
    }
}