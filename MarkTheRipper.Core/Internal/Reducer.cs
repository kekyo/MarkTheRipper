/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Metadata;
using MarkTheRipper.Template;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Internal;

public delegate ValueTask<object?> AsyncFunctionDelegate(
    IExpression[] parameters,
    MetadataContext context,
    CancellationToken ct);

internal static class Reducer
{
    private static readonly object?[] empty = new object?[0];
    private static readonly char[] dotOperator = new[] { '.' };

    public static string UnsafeFormatValue(
        object? value,
        IExpression[] parameters,
        MetadataContext metadata) =>
        value switch
        {
            null =>
                string.Empty,
            IMetadataEntry entry =>
                UnsafeFormatValue(
                    entry.GetImplicitValueAsync(default).Result,
                    parameters,
                    metadata),
            AsyncFunctionDelegate func =>
                UnsafeFormatValue(
                    func(parameters, metadata, default).Result,
                    Utilities.Empty<IExpression>(),  // TODO: nested argument list
                    metadata),
            IFormattable formattable
                when parameters.FirstOrDefault() is ValueExpression(string format) =>
                formattable.ToString(format, metadata.Lookup("lang") switch
                {
                    IFormatProvider fp => fp,
                    string lang => new CultureInfo(lang),
                    _ => CultureInfo.CurrentCulture,
                }),
            string str =>
                str,
            IEnumerable enumerable =>
                string.Join(",",
                    enumerable.Cast<object?>().
                    Select(v => UnsafeFormatValue(v, parameters, metadata))),
            _ =>
                value.ToString() ?? string.Empty,
        };

    public static async ValueTask<string> FormatValueAsync(
        object? value,
        IExpression[] parameters,
        MetadataContext metadata,
        CancellationToken ct) =>
        value switch
        {
            null =>
                string.Empty,
            IMetadataEntry entry =>
                await FormatValueAsync(
                    await entry.GetImplicitValueAsync(ct).ConfigureAwait(false),
                    parameters,
                    metadata,
                    ct).
                    ConfigureAwait(false),
            AsyncFunctionDelegate func =>
                await FormatValueAsync(
                    await func(parameters, metadata, ct).ConfigureAwait(false),
                    Utilities.Empty<IExpression>(),  // TODO: nested argument list
                    metadata,
                    ct).
                    ConfigureAwait(false),
            IFormattable formattable
                when parameters.FirstOrDefault() is ValueExpression(string format) =>
                formattable.ToString(format, metadata.Lookup("lang") switch
                {
                    IFormatProvider fp => fp,
                    string lang => new CultureInfo(lang),
                    _ => CultureInfo.CurrentCulture,
                }),
            string str =>
                str,
            IEnumerable enumerable =>
                string.Join(",",
                    await Task.WhenAll(
                        enumerable.Cast<object?>().
                        Select(v => FormatValueAsync(v, parameters, metadata, ct).AsTask())).
                        ConfigureAwait(false)),
            _ =>
                value.ToString() ?? string.Empty,
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

    ////////////////////////////////////////////////////////////////////

    private static object? ReduceProperty(
        string[] elements,
        int index,
        object? currentValue,
        MetadataContext metadata) =>
        index < elements.Length &&
        currentValue is IMetadataEntry entry &&
        entry.GetProperty(elements[index++], metadata) is { } value ?
            ReduceProperty(elements, index, value, metadata) :
            currentValue;

    private static object? ReduceExpressionElements(
        string[] elements,
        int index,
        MetadataContext metadata) =>
        index < elements.Length &&
        metadata.Lookup(elements[index++]) is { } value ?
            ReduceProperty(elements, index, value, metadata) :
            null;

    public static object? ReduceExpression(
        IExpression expression,
        MetadataContext metadata) =>
        expression switch
        {
            VariableExpression v => ReduceExpressionElements(
                v.Name.Split(dotOperator, StringSplitOptions.RemoveEmptyEntries),
                0, metadata),
            ValueExpression v => v.Value,
            _ => throw new InvalidOperationException(),
        };
}
