/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Internal;

internal static class Utilities
{
    public static string? FormatValue(
        object? value, object? parameter, IFormatProvider fp) =>
        (value, parameter) switch
        {
            (null, _) => null,
            (_, null) => value.ToString(),
            (IFormattable formattable, string format) => formattable.ToString(format, fp),
            _ => value.ToString(),
        };

    ///////////////////////////////////////////////////////////////////////////////////

    public static ValueTask WithCancellation(
        this Task task, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (task.IsCompleted)
        {
            return default;
        }

        var tcs = new TaskCompletionSource<bool>();
        CancellationTokenRegistration? cr =
            ct.Register(() => tcs.TrySetCanceled());

        task.ContinueWith(t =>
        {
            cr?.Dispose();
            cr = null;

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
                tcs.TrySetResult(true);
            }
        });

        return new ValueTask(tcs.Task);
    }

    public static ValueTask<T> WithCancellation<T>(
        this Task<T> task, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (task.IsCompleted)
        {
            return new ValueTask<T>(task.Result);
        }

        var tcs = new TaskCompletionSource<T>();
        CancellationTokenRegistration? cr =
            ct.Register(() => tcs.TrySetCanceled());

        task.ContinueWith(t =>
        {
            cr?.Dispose();
            cr = null;

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

        return new ValueTask<T>(tcs.Task);
    }
}
