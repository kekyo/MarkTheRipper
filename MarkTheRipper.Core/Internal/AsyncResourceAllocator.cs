/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Internal;

internal sealed class AsyncResourceAllocator
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

    ///////////////////////////////////////////////////////////////////////////////////

    private readonly Dictionary<string, Waiter> waiters = new();
    private readonly Func<string, ValueTask> allocator;

    public AsyncResourceAllocator(Func<string, ValueTask> allocator) =>
        this.allocator = allocator;

    private (bool required, Waiter waiter) GetWaiter(string key)
    {
        Waiter waiter;
        lock (this.waiters)
        {
            if (!this.waiters.TryGetValue(key, out waiter!))
            {
                waiter = new();
                this.waiters.Add(key, waiter);
                return (true, waiter);
            }
        }
        return (false, waiter);
    }

    public async ValueTask AllocateAsync(
        string key, CancellationToken ct)
    {
        var (required, waiter) = this.GetWaiter(key);
        if (required)
        {
            using var _ = ct.Register(() => waiter.SetCanceled());
            try
            {
                await this.allocator(key).
                ConfigureAwait(false);
            }
            finally
            {
                waiter.SetFinished();
            }
        }
        else
        {
            await waiter.AllocateWaitTask().
                ConfigureAwait(false);
        }
    }
}
