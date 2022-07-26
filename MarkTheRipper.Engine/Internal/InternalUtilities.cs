﻿/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Internal;

internal static class InternalUtilities
{
    private static readonly Uri defaultUrl =
        new Uri("https://example.com/");

    public static readonly JsonSerializer DefaultJsonSerializer =
        Utilities.GetDefaultJsonSerializer();

    ///////////////////////////////////////////////////////////////////////////////////

    public static Uri GetUrl(string? urlString) =>
        (!string.IsNullOrWhiteSpace(urlString) &&
         Uri.TryCreate(urlString, UriKind.Absolute, out var url)) ?
            url :
            defaultUrl;

    ///////////////////////////////////////////////////////////////////////////////////

    public static IEnumerable<string> EnumerateAllFiles(
        string path, string searchPattern) =>
        Directory.Exists(path) ?
            Directory.EnumerateFiles(path, searchPattern, SearchOption.AllDirectories) :
            Empty<string>();

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

    private static class EmptyArray<T>
    {
        public static readonly T[] Empty = new T[0];
    }

    public static T[] Empty<T>() =>
        EmptyArray<T>.Empty;

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

    public static IEnumerable<U> Collect<T, U>(this IEnumerable<T> enumerable, Func<T, U?> selector)
    {
        foreach (var value in enumerable)
        {
            if (selector(value) is { } mapped)
            {
                yield return mapped;
            }
        }
    }

    public static SortedDictionary<TKey, TValue> ToSortedDictionary<T, TKey, TValue>(
        this IEnumerable<T> enumerable,
        Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
        where TKey : IEquatable<TKey>
    {
        var sd = new SortedDictionary<TKey, TValue>();
        foreach (var item in enumerable)
        {
            var key = keySelector(item);
            var value = valueSelector(item);
            sd.Add(key, value);
        }
        return sd;
    }

    public static IEnumerable<T> Unfold<T>(this T? value, Func<T, T?> selector)
    {
        var current = value;
        while (current != null)
        {
            yield return current;
            current = selector(current);
        }
    }

    public static IEnumerable<(T first, T? second)> OverlappedPair<T>(
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
