/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper;

#if !NET6_0_OR_GREATER
internal static class Interops
{
    public static async ValueTask<T> WaitAsync<T>(
        this Task<T> task, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (task.IsCompleted)
        {
            return task.Result;
        }
        
        var tcs = new TaskCompletionSource<T>();
        using var _ = ct.Register(() => tcs.TrySetCanceled());

        var __ = task.ContinueWith(t =>
        {
            if (t.IsCanceled)
            {
                tcs.TrySetCanceled();
            }
            else if (t.IsFaulted)
            {
                tcs.TrySetException(t.Exception!.InnerExceptions);
            }
            else
            {
                tcs.TrySetResult(t.Result);
            }
        });

        return await tcs.Task.
            ConfigureAwait(false);
    }
}
#endif
