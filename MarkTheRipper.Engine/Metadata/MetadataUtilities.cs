/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Functions;
using MarkTheRipper.IO;
using MarkTheRipper.TextTreeNodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Metadata;

public static class MetadataUtilities
{
    private static readonly object?[] empty = new object?[0];

    /////////////////////////////////////////////////////////////////////

    public static IMetadataContext CreateWithDefaults(IHttpAccessor httpAccessor)
    {
        var metadata = new MetadataContext();

        metadata.SetValue("httpAccessor", httpAccessor);

        metadata.SetValue("generator", $"MarkTheRipper {ThisAssembly.AssemblyVersion}");
        metadata.SetValue("generated", DateTimeOffset.Now);
        metadata.SetValue("lang", CultureInfo.CurrentCulture);
        metadata.SetValue("timezone", TimeZoneInfo.Local);
        metadata.SetValue("layout", "page");

        metadata.SetValue("add", FunctionFactory.CastTo(Formula.AddAsync));
        metadata.SetValue("sub", FunctionFactory.CastTo(Formula.SubtractAsync));
        metadata.SetValue("mul", FunctionFactory.CastTo(Formula.MultipleAsync));
        metadata.SetValue("div", FunctionFactory.CastTo(Formula.DivideAsync));
        metadata.SetValue("mod", FunctionFactory.CastTo(Formula.ModuloAsync));

        metadata.SetValue("take", FunctionFactory.CastTo(Iterator.TakeAsync));

        metadata.SetValue("format", FunctionFactory.CastTo(Format.FormatAsync));

        metadata.SetValue("relative", FunctionFactory.CastTo(Relative.RelativeAsync));
        metadata.SetValue("lookup", FunctionFactory.CastTo(Lookup.LookupAsync));
        metadata.SetValue("older", FunctionFactory.CastTo(Navigate.OlderAsync));
        metadata.SetValue("newer", FunctionFactory.CastTo(Navigate.NewerAsync));

        metadata.SetValue("embed", FunctionFactory.CastTo(oEmbed.EmbedAsync));
        metadata.SetValue("card", FunctionFactory.CastTo(oEmbed.CardAsync));

        return metadata;
    }

    /////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Read metadata json from the path.
    /// </summary>
    /// <param name="path">JSON file path.</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Metadata dictionary.</returns>
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
            Utilities.UTF8,
            true);
        var jr = new JsonTextReader(tr);

        var s = Utilities.GetDefaultJsonSerializer();

        var jt = await JToken.LoadAsync(jr, ct);

        static async ValueTask<IExpression> ToExpressionAsync(
            string keyNameHint, JToken? token, CancellationToken ct) =>
            token switch
            {
                JValue v when v.Value?.ToString() is var sv =>
                    sv != null ?
                        await Parser.ParseKeywordExpressionAsync(keyNameHint, sv, ct) :
                        new ValueExpression(null),
                JArray a => new ArrayExpression(
                    await Task.WhenAll(a.Select(e => ToExpressionAsync("", e, ct).AsTask()))),
                null => new ValueExpression(null),
                _ => token.Value<string>() is { } sv ?
                    await Parser.ParseKeywordExpressionAsync(
                        keyNameHint, sv, ct) :
                    new ValueExpression(null),
            };

        return (await Task.WhenAll(
            (jt.ToObject<Dictionary<string, JToken?>>(s) ?? new()).
             Select(async entry =>
                (entry.Key,
                 Value: await ToExpressionAsync(entry.Key, entry.Value, ct))))).
            ToDictionary(
                entry => entry.Key,
                entry => entry.Value);
    }

    /////////////////////////////////////////////////////////////////////

    private static IFormatProvider UnsafeGetFormatProvider(
        IMetadataContext metadata) =>
        metadata.Lookup("lang") is { } langExpression &&
            Reducer.Instance.UnsafeReduceExpression(langExpression, metadata) is { } langValue ?
                langValue is IFormatProvider fp ?
                    fp :
                    Utilities.GetLocale(UnsafeFormatValue(langValue, metadata)) :
                CultureInfo.InvariantCulture;

    public static async ValueTask<CultureInfo> GetLanguageAsync(
        this IMetadataContext metadata, CancellationToken ct) =>
        metadata.Lookup("lang") is { } langExpression &&
            await Reducer.Instance.ReduceExpressionAsync(langExpression, metadata, ct) is { } langValue ?
                langValue is CultureInfo ci ?
                    ci :
                    Utilities.GetLocale(await FormatValueAsync(langValue, metadata, ct)) :
            CultureInfo.InvariantCulture;

    /////////////////////////////////////////////////////////////////////

    internal static string UnsafeFormatValue(
        object? value, IMetadataContext metadata) =>
        value switch
        {
            null =>
                string.Empty,
            IMetadataEntry entry =>
                UnsafeFormatValue(
                    entry.GetImplicitValueAsync(metadata, Reducer.Instance, default).Result,
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
        object? value, IMetadataContext metadata, CancellationToken ct) =>
        value switch
        {
            null =>
                string.Empty,
            IMetadataEntry entry =>
                await FormatValueAsync(
                    await entry.GetImplicitValueAsync(metadata, Reducer.Instance, ct),
                    metadata,
                    ct),
            string str =>
                str,
            IEnumerable enumerable =>
                string.Join(",",
                    await Task.WhenAll(
                        enumerable.Cast<object?>().
                        Select(v => FormatValueAsync(v, metadata, ct).AsTask()))),
            IFormattable formattable =>
                formattable.ToString(
                    null,
                    await GetLanguageAsync(metadata, ct)) ??
                string.Empty,
            _ =>
                value.ToString() ??
                string.Empty,
        };

    public static IEnumerable<object?> EnumerateValue(
        object? value, IMetadataContext metadata) =>
        value switch
        {
            null => empty,
            string str => new[] { str },
            IEnumerable enumerable => enumerable.Cast<object?>(),
            _ => new[] { value },
        };

    public static async ValueTask<TValue> LookupValueAsync<TValue>(
        this IMetadataContext metadata, string keyName, TValue defaultValue, CancellationToken ct) =>
        metadata.Lookup(keyName) is { } expression &&
        await Reducer.Instance.ReduceExpressionAsync(expression, metadata, ct) is TValue value ?
            value : defaultValue;

    /////////////////////////////////////////////////////////////////////

    public static async ValueTask<RootTextNode> GetLayoutAsync(
        this IMetadataContext metadata, string layoutName,
        string? fallbackName, CancellationToken ct)
    {
        if (metadata.Lookup("layoutList") is { } layoutListExpression &&
            await Reducer.Instance.ReduceExpressionAsync(
                layoutListExpression, metadata, ct) is IReadOnlyDictionary<string, RootTextNode> tl)
        {
            if (tl.TryGetValue(layoutName, out var layout))
            {
                return layout;
            }
            else if (fallbackName != null)
            {
                if (tl.TryGetValue(fallbackName, out layout))
                {
                    return layout;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Layout `{layoutName}` and fallback `{fallbackName}` were not found.");
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"Layout `{layoutName}` are not found.");
            }
        }
        else
        {
            throw new InvalidOperationException(
                "Layout list was not found.");
        }
    }

    public static async ValueTask<RootTextNode> GetLayoutAsync(
        this IMetadataContext metadata, CancellationToken ct)
    {
        if (metadata.Lookup("layout") is { } layoutExpression &&
            await Reducer.Instance.ReduceExpressionAsync(
                layoutExpression, metadata, ct) is { } layoutValue)
        {
            if (layoutValue is RootTextNode layout)
            {
                return layout;
            }
            else if (layoutValue is PartialLayoutEntry entry)
            {
                return await metadata.GetLayoutAsync(entry.Name, "page", ct);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Invalid layout object. Value={layoutValue.GetType().Name}");
            }
        }
        else
        {
            throw new InvalidOperationException(
                "Layout not defined.");
        }
    }
}
