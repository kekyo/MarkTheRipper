﻿/////////////////////////////////////////////////////////////////////////////////////
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

    public static readonly Encoding UTF8 =
        new UTF8Encoding(false);   // No BOM

    ///////////////////////////////////////////////////////////////////////////////////

    public static int IndexOfNot(this string str, char separator, int start)
    {
        var index = start;
        while (index < str.Length)
        {
            var ch = str[index];
            if (separator != ch)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    public static int IndexOfNotAll(this string str, char[] separators, int start)
    {
        var index = start;
        while (index < str.Length)
        {
            var ch = str[index];
            var found = false;
            for (var i = 0; i < separators.Length; i++)
            {
                if (separators[i] == ch)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

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

    public static IEnumerable<T> Unfold<T>(this T value, Func<T, T?> selector)
    {
        var current = value;
        while (current != null)
        {
            yield return current;
            current = selector(current);
        }
    }

    private sealed class Comparer<T> : IComparer<T>
    {
        private readonly Func<T, T, int> comparer;

        public Comparer(Func<T, T, int> comparer) =>
            this.comparer = comparer;

        public int Compare(T? x, T? y) =>
            this.comparer(x!, y!);

    }

    public static IOrderedEnumerable<T> OrderBy<T, TKey>(
        this IEnumerable<T> enumerable,
        Func<T, TKey> selector,
        Func<TKey, TKey, int> comparer) =>
        enumerable.OrderBy(selector, new Comparer<TKey>(comparer));

    public static IEnumerable<(T first, T? second)> Overlapped<T>(
        this IEnumerable<T> enumerable)
    {
        using var enumerator = enumerable.GetEnumerator();
        if (enumerator.MoveNext())
        {
            var first = enumerator.Current;
            if (enumerator.MoveNext())
            {
                do
                {
                    var second = enumerator.Current;
                    yield return (first, second);
                    first = second;
                }
                while (enumerator.MoveNext());
            }
            yield return (first, default(T));
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////

    public static readonly ValueTask<object?> NullAsync =
        new(default(object?));

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
