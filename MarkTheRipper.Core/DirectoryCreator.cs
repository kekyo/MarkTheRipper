/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper;

internal sealed class DirectoryCreator
{
    private sealed class Waiter
    {
        private Queue<TaskCompletionSource<bool>>? queue = new();
        private bool isCanceled;

        public ValueTask AllocateWaitTask()
        {
            lock (this)
            {
                if (this.queue is { } queue)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    queue.Enqueue(tcs);
                    return new ValueTask(tcs.Task);
                }
                else
                {
                    if (this.isCanceled)
                    {
                        throw new TaskCanceledException();
                    }
                    return default;
                }
            }
        }

        public void SetFinished()
        {
            lock (this)
            {
                if (this.queue is { } queue)
                {
                    while (queue.Count >= 1)
                    {
                        queue.Dequeue().TrySetResult(true);
                    }
                    this.queue = null;
                }
            }
        }

        public void SetCanceled()
        {
            lock (this)
            {
                if (this.queue is { } queue)
                {
                    while (queue.Count >= 1)
                    {
                        queue.Dequeue().TrySetCanceled();
                    }
                    this.queue = null;
                    this.isCanceled = true;
                }
            }
        }
    }

    private readonly Dictionary<string, Waiter> dirs = new();

    private (bool required, Waiter waiter) GetWaiter(string dirPath)
    {
        Waiter waiter;
        lock (this.dirs)
        {
            if (!this.dirs.TryGetValue(dirPath, out waiter!))
            {
                waiter = new();
                this.dirs.Add(dirPath, waiter);
                return (true, waiter);
            }
        }
        return (false, waiter);
    }

    public ValueTask CreateIfNotExistAsync(
        string dirPath, CancellationToken ct)
    {
        var (required, waiter) = this.GetWaiter(dirPath);
        if (required)
        {
            using var _ = ct.Register(() => waiter.SetCanceled());
            try
            {
                if (!Directory.Exists(dirPath))
                {
                    try
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                waiter.SetFinished();
            }
            return default;
        }
        else
        {
            return waiter.AllocateWaitTask();
        }
    }
}
