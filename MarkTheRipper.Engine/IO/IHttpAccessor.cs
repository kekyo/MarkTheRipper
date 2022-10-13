/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using AngleSharp.Html.Dom;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.IO;

public interface IHttpAccessor
{
    ValueTask<JToken> FetchJsonAsync(
        Uri url, CancellationToken ct);
    ValueTask<JToken> PostJsonAsync(
        Uri url, JToken requestJson,
        IReadOnlyDictionary<string, string> headers,
        IReadOnlyDictionary<string, string> cacheKeyValues,
        CancellationToken ct);

    ValueTask<IHtmlDocument> FetchHtmlAsync(
        Uri url, CancellationToken ct);

    ValueTask<Uri?> ExamineShortUrlAsync(Uri url, CancellationToken ct);
}

public static class HttpAccessorExtension
{
    public static async ValueTask<T> PostJsonAsync<T>(
        this IHttpAccessor httpAccess,
        Uri url, JToken requestJson,
        IReadOnlyDictionary<string, string> headers,
        IReadOnlyDictionary<string, string> cacheKeyValues,
        CancellationToken ct)
    {
        var jt = await httpAccess.PostJsonAsync(
            url, requestJson, headers, cacheKeyValues, ct).
            ConfigureAwait(false);
        return jt.ToObject<T>()!;
    }
}
