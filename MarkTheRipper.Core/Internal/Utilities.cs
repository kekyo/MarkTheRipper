/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Internal;

internal static class Utilities
{
    public static readonly char[] PathSeparators = new[]
    {
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar,
    };

    ///////////////////////////////////////////////////////////////////////////////////

    public static JsonSerializer GetDefaultJsonSerializer()
    {
        var defaultNamingStrategy = new CamelCaseNamingStrategy();
        var serializer = new JsonSerializer
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            DateTimeZoneHandling = DateTimeZoneHandling.Local,
            NullValueHandling = NullValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ContractResolver = new DefaultContractResolver { NamingStrategy = defaultNamingStrategy, },
        };
        serializer.Converters.Add(new StringEnumConverter(defaultNamingStrategy));
        return serializer;
    }

    ///////////////////////////////////////////////////////////////////////////////////

    private static class EmtyArray<T>
    {
        public static readonly T[] Empty = new T[0];
    }

    public static T[] Empty<T>() =>
        EmtyArray<T>.Empty;

    ///////////////////////////////////////////////////////////////////////////////////

    public static string? FormatValue(
        object? value, object? parameter, IFormatProvider fp) =>
        (value, parameter) switch
        {
            (null, _) => null,
            (IEntry entry, _) => FormatValue(entry.ImplicitValue, parameter, fp),
            (IFormattable formattable, string format) =>
                formattable.ToString(format, fp),
            (string str, _) => str,
            (IEnumerableEntry enumerable, _) =>
                string.Join(",", enumerable.GetEntries().Select(v => FormatValue(v, parameter, fp))),
            (IEnumerable enumerable, _) =>
                string.Join(",", enumerable.Cast<object?>().Select(v => FormatValue(v, parameter, fp))),
            _ => value.ToString(),
        };

    private static readonly object?[] empty = new object?[0];

    public static IEnumerable<object?> EnumerateValue(object? value) =>
        value switch
        {
            null => empty,
            string str => new[] { str },
            IEnumerableEntry enumerable => enumerable.GetEntries(),
            IEnumerable enumerable => enumerable.Cast<object?>(),
            _ => new[] { value },
        };

    ///////////////////////////////////////////////////////////////////////////////////

#if !NET6_0_OR_GREATER
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> enumerable, Func<T, TKey> selector)
    {
        var keys = new HashSet<TKey>();
        foreach (var entry in enumerable)
        {
            if (keys.Add(selector(entry)))
            {
                yield return entry;
            }
        }
    }
#endif

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
