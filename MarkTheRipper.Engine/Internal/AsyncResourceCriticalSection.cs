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

internal sealed class AsyncResourceCriticalSection
{
    private sealed class Waiter
    {
        private Queue<TaskCompletionSource<bool>>? queue = new();
        private bool isCanceled;

        public ValueTask WaitAsync()
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

    private sealed class WaiterContinuatiuon : IDisposable
    {
        private readonly Waiter waiter;
        private readonly CancellationTokenRegistration ctr;

        public WaiterContinuatiuon(Waiter waiter, CancellationToken ct)
        {
            this.waiter = waiter;
            this.ctr = ct.Register(() => waiter.SetCanceled());
        }

        public void Dispose()
        {
            this.ctr.Dispose();
            this.waiter.SetFinished();
        }
    }

    private sealed class NopContinuation : IDisposable
    {
        private NopContinuation()
        {
        }

        public void Dispose()
        {
        }

        public static readonly NopContinuation Instance = new();
    }

    ///////////////////////////////////////////////////////////////////////////////////

    private readonly Dictionary<string, Waiter> waiters = new();

    private (bool required, Waiter waiter) GetWaiter(string resourceKey)
    {
        Waiter waiter;
        lock (this.waiters)
        {
            if (!this.waiters.TryGetValue(resourceKey, out waiter!))
            {
                waiter = new();
                this.waiters.Add(resourceKey, waiter);
                return (true, waiter);
            }
        }
        return (false, waiter);
    }

    public async ValueTask AllocateAsync(
        string key,
        Func<ValueTask> allocator,
        CancellationToken ct)
    {
        var (required, waiter) = this.GetWaiter(key);
        if (required)
        {
            using var _ = ct.Register(() => waiter.SetCanceled());
            try
            {
                await allocator().
                    ConfigureAwait(false);
            }
            finally
            {
                waiter.SetFinished();
            }
        }
        else
        {
            await waiter.WaitAsync().
                ConfigureAwait(false);
        }
    }

    public async ValueTask<IDisposable> EnterAsync(
        string resourceKey,
        CancellationToken ct)
    {
        var (required, waiter) = this.GetWaiter(resourceKey);
        if (required)
        {
            return new WaiterContinuatiuon(waiter, ct);
        }
        else
        {
            await waiter.WaitAsync().
                ConfigureAwait(false);
            return NopContinuation.Instance;
        }
    }
}
