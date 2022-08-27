/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Metadata;

public static class MetadataUtilities
{
    public static async ValueTask<IFormatProvider> GetFormatProviderAsync(
        MetadataContext metadata,
        CancellationToken ct) =>
        metadata.Lookup("lang") is { } langExpression &&
            await langExpression.ReduceExpressionAsync(metadata, ct) is { } langValue ?
                langValue is IFormatProvider fp ?
                    fp :
                    new CultureInfo(await MetadataUtilities.FormatValueAsync(langValue, metadata, ct).
                        ConfigureAwait(false)) :
                CultureInfo.InvariantCulture;

    private static readonly object?[] empty = new object?[0];
    private static readonly char[] dotOperator = new[] { '.' };

    internal static string UnsafeFormatValue(
        object? value,
        MetadataContext metadata) =>
        value switch
        {
            null =>
                string.Empty,
            IMetadataEntry entry =>
                UnsafeFormatValue(
                    entry.GetImplicitValueAsync(default).Result,
                    metadata),
            string str =>
                str,
            IEnumerable enumerable =>
                string.Join(",",
                    enumerable.Cast<object?>().
                    Select(v => UnsafeFormatValue(v, metadata))),
            _ =>
                value.ToString() ?? string.Empty,
        };

    public static async ValueTask<string> FormatValueAsync(
        object? value,
        MetadataContext metadata,
        CancellationToken ct) =>
        value switch
        {
            null =>
                string.Empty,
            IMetadataEntry entry =>
                await FormatValueAsync(
                    await entry.GetImplicitValueAsync(ct).ConfigureAwait(false),
                    metadata,
                    ct).
                    ConfigureAwait(false),
            string str =>
                str,
            IEnumerable enumerable =>
                string.Join(",",
                    await Task.WhenAll(
                        enumerable.Cast<object?>().
                        Select(v => FormatValueAsync(v, metadata, ct).AsTask())).
                        ConfigureAwait(false)),
            _ =>
                value.ToString() ?? string.Empty,
        };

    public static IEnumerable<object?> EnumerateValue(
        object? value, MetadataContext metadata) =>
        value switch
        {
            null => empty,
            string str => new[] { str },
            IEnumerable enumerable => enumerable.Cast<object?>(),
            _ => new[] { value },
        };
}
