/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Metadata;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Template;

public delegate ValueTask<object?> AsyncFunctionDelegate(
    object? parameter, MetadataContext context, CancellationToken ct);

internal static class Reducer
{
    private static readonly object?[] empty = new object?[0];
    private static readonly char[] dotOperator = new[] { '.' };

    public static string? UnsafeFormatValue(
        object? value, object? parameter, MetadataContext context) =>
        (value, parameter) switch
        {
            (null, _) =>
                null,
            (IMetadataEntry entry, _) =>
                UnsafeFormatValue(
                    entry.GetImplicitValueAsync(default).Result,
                    parameter,
                    context),
            (AsyncFunctionDelegate func, _) =>
                UnsafeFormatValue(
                    func(parameter, context, default).Result, null, context),
            (IFormattable formattable, string format) =>
                formattable.ToString(format, context.Lookup("lang") switch
                {
                    IFormatProvider fp => fp,
                    string lang => new CultureInfo(lang),
                    _ => CultureInfo.CurrentCulture,
                }),
            (string str, _) =>
                str,
            (IEnumerable enumerable, _) =>
                string.Join(",",
                    enumerable.Cast<object?>().
                    Select(v => UnsafeFormatValue(v, parameter, context))),
            _ =>
                value.ToString(),
        };

    public static async ValueTask<string?> FormatValueAsync(
        object? value, object? parameter, MetadataContext context, CancellationToken ct) =>
        (value, parameter) switch
        {
            (null, _) =>
                null,
            (IMetadataEntry entry, _) =>
                await FormatValueAsync(
                    await entry.GetImplicitValueAsync(ct).ConfigureAwait(false),
                    parameter,
                    context,
                    ct).
                    ConfigureAwait(false),
            (AsyncFunctionDelegate func, _) =>
                await FormatValueAsync(
                    await func(parameter, context, ct).ConfigureAwait(false), null, context, ct).
                    ConfigureAwait(false),
            (IFormattable formattable, string format) =>
                formattable.ToString(format, context.Lookup("lang") switch
                {
                    IFormatProvider fp => fp,
                    string lang => new CultureInfo(lang),
                    _ => CultureInfo.CurrentCulture,
                }),
            (string str, _) =>
                str,
            (IEnumerable enumerable, _) =>
                string.Join(",",
                    await Task.WhenAll(
                        enumerable.Cast<object?>().
                        Select(v => FormatValueAsync(v, parameter, context, ct).AsTask())).
                        ConfigureAwait(false)),
            _ =>
                value.ToString(),
        };

    public static IEnumerable<object?> EnumerateValue(
        object? value, MetadataContext context) =>
        value switch
        {
            null => empty,
            string str => new[] { str },
            IEnumerable enumerable => enumerable.Cast<object?>(),
            _ => new[] { value },
        };

    private static object? Reduce(
        string[] terms, int index, object? currentValue,
        MetadataContext metadata) =>
        index < terms.Length &&
        currentValue is IMetadataEntry entry &&
        entry.GetProperty(terms[index++], metadata) is { } value ?
            Reduce(terms, index, value, metadata) :
            currentValue;

    private static object? Reduce(
        string[] terms, int index,
        MetadataContext metadata) =>
        index < terms.Length &&
        metadata.Lookup(terms[index++]) is { } value ?
            Reduce(terms, index, value, metadata) :
            null;

    public static object? Reduce(
        string expression,
        MetadataContext metadata) =>
        Reduce(expression.Split(
            dotOperator,
            StringSplitOptions.RemoveEmptyEntries),
            0,
            metadata);
}
