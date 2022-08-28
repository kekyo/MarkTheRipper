/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Internal;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Metadata;

public static class MetadataUtilities
{
    private static readonly object?[] empty = new object?[0];

    /////////////////////////////////////////////////////////////////////

    public static async ValueTask<IReadOnlyDictionary<string, IExpression>> ReadMetadataAsync(
        string path, CancellationToken ct)
    {
        using var rs = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            65536,
            true);
        var tr = new StreamReader(
            rs,
            Encoding.UTF8,
            true);
        var jr = new JsonTextReader(tr);

        var s = Utilities.GetDefaultJsonSerializer();

        var jt = await JToken.LoadAsync(jr, ct);

        static IExpression ToExpression(JToken? token) =>
            token switch
            {
                JValue v when v.Value?.ToString() is var sv =>
                    sv != null ?
                        Parser.ParseExpression(sv, ListTypes.Value) :
                        new ValueExpression(null),
                JArray a => new ArrayExpression(a.Select(ToExpression).ToArray()),
                null => new ValueExpression(null),
                _ => Parser.ParseExpression(token.ToString(), ListTypes.Value),
            };

        return (jt.ToObject<Dictionary<string, JToken?>>(s) ?? new()).
            ToDictionary(
                entry => entry.Key,
                entry => ToExpression(entry.Value));
    }

    /////////////////////////////////////////////////////////////////////

    private static IFormatProvider UnsafeGetFormatProvider(
        MetadataContext metadata) =>
        metadata.Lookup("lang") is { } langExpression &&
            Reducer.UnsafeReduceExpression(langExpression, metadata) is { } langValue ?
                langValue is IFormatProvider fp ?
                    fp :
                    new CultureInfo(UnsafeFormatValue(langValue, metadata)) :
                CultureInfo.InvariantCulture;

    public static async ValueTask<IFormatProvider> GetFormatProviderAsync(
        MetadataContext metadata, CancellationToken ct) =>
        metadata.Lookup("lang") is { } langExpression &&
            await langExpression.ReduceExpressionAsync(metadata, ct) is { } langValue ?
                langValue is IFormatProvider fp ?
                    fp :
                    new CultureInfo(await FormatValueAsync(langValue, metadata, ct).
                        ConfigureAwait(false)) :
                CultureInfo.InvariantCulture;

    /////////////////////////////////////////////////////////////////////

    internal static string UnsafeFormatValue(
        object? value, MetadataContext metadata) =>
        value switch
        {
            null =>
                string.Empty,
            IMetadataEntry entry =>
                UnsafeFormatValue(
                    entry.GetImplicitValueAsync(metadata, default).Result,
                    metadata),
            string str =>
                str,
            IEnumerable enumerable =>
                string.Join(",",
                    enumerable.Cast<object?>().
                    Select(v => UnsafeFormatValue(v, metadata))),
            IFormattable formattable =>
                formattable.ToString(
                    null,
                    UnsafeGetFormatProvider(metadata)) ??
                string.Empty,
            _ =>
                value.ToString() ?? string.Empty,
        };

    public static async ValueTask<string> FormatValueAsync(
        object? value, MetadataContext metadata, CancellationToken ct) =>
        value switch
        {
            null =>
                string.Empty,
            IMetadataEntry entry =>
                await FormatValueAsync(
                    await entry.GetImplicitValueAsync(metadata, ct).ConfigureAwait(false),
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
            IFormattable formattable =>
                formattable.ToString(
                    null,
                    await GetFormatProviderAsync(metadata, ct).
                        ConfigureAwait(false)) ??
                string.Empty,
            _ =>
                value.ToString() ??
                string.Empty,
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