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

namespace MarkTheRipper.Internal;

internal static class Expression
{
    private static readonly object?[] empty = new object?[0];
    private static readonly char[] dotOperator = new[] { '.' };

    public static string? FormatValue(
        object? value, object? parameter, MetadataContext context) =>
        (value, parameter) switch
        {
            (null, _) => null,
            (IMetadataEntry entry, _) => FormatValue(entry.ImplicitValue, parameter, context),
            (IFormattable formattable, string format) =>
                formattable.ToString(format, context.Lookup("lang") switch
                {
                    IFormatProvider fp => fp,
                    string lang => new CultureInfo(lang),
                    _ => CultureInfo.CurrentCulture,
                }),
            (string str, _) => str,
            (IEnumerable enumerable, _) =>
                string.Join(",", enumerable.Cast<object?>().Select(v => FormatValue(v, parameter, context))),
            _ => value.ToString(),
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
