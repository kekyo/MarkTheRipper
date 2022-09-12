/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using MarkTheRipper.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper;

public static class Utilities
{
    public static readonly char[] PathSeparators = new[]
    {
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar,
    };

    public static readonly Encoding UTF8 =
        new UTF8Encoding(false);   // No BOM

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

    public static ValueTask<JToken> DeserializeJsonAsync(
        this JsonSerializer serializer, Stream stream, CancellationToken ct)
    {
        var tr = new StreamReader(stream, UTF8);
        var jr = new JsonTextReader(tr);
        return new ValueTask<JToken>(JToken.ReadFromAsync(jr, null, ct));
    }

    public static IEnumerable<TValue> EnumerateArray<TValue>(
        this JToken token, JsonSerializer? serializer = default)
    {
        serializer ??= defaultJsonSerializer;

        if (token is JArray array)
        {
            foreach (var item in array)
            {
                yield return item.ToObject<TValue>(serializer)!;
            }
        }
    }

    public static TValue GetValue<TValue>(
        this JToken token, string memberName, TValue defaultValue = default!,
        JsonSerializer? serializer = default)
    {
        serializer ??= defaultJsonSerializer;

        if (token is JObject obj)
        {
            if (obj.TryGetValue(memberName, out var value))
            {
                return value.ToObject<TValue>(serializer) ??
                    defaultValue;
            }
        }

        return defaultValue;
    }

    ///////////////////////////////////////////////////////////////////////////////////

    private static readonly Lazy<HttpClient> httpClientFactory =
        new(() => new HttpClient());
    internal static readonly JsonSerializer defaultJsonSerializer =
        GetDefaultJsonSerializer();

    public static async ValueTask<JToken> FetchJsonAsync(Uri url, CancellationToken ct)
    {
        using var stream = await httpClientFactory.Value.GetStreamAsync(url).
            WithCancellation(ct).
            ConfigureAwait(false);

        return await defaultJsonSerializer.DeserializeJsonAsync(stream, ct).
            ConfigureAwait(false);
    }

    public static async ValueTask<IHtmlDocument> FetchHtmlAsync(Uri url, CancellationToken ct)
    {
        var parser = new HtmlParser();

        using var stream = await httpClientFactory.Value.GetStreamAsync(url).
            WithCancellation(ct).
            ConfigureAwait(false);

        return await parser.ParseDocumentAsync(stream, ct).
            ConfigureAwait(false);
    }
}
