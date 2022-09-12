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
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.IO;

public sealed class HttpAccessor : IHttpAccessor
{
    private static readonly Lazy<HttpClient> httpClientFactory =
        new(() => new HttpClient());

    public static readonly IHttpAccessor Instance =
        new HttpAccessor();

    private HttpAccessor()
    {
    }

    public async ValueTask<JToken> FetchJsonAsync(Uri url, CancellationToken ct)
    {
        using var stream = await httpClientFactory.Value.GetStreamAsync(url).
            WithCancellation(ct).
            ConfigureAwait(false);

        return await InternalUtilities.DefaultJsonSerializer.DeserializeJsonAsync(stream, ct).
            ConfigureAwait(false);
    }

    public async ValueTask<IHtmlDocument> FetchHtmlAsync(Uri url, CancellationToken ct)
    {
        var parser = new HtmlParser();

        using var stream = await httpClientFactory.Value.GetStreamAsync(url).
            WithCancellation(ct).
            ConfigureAwait(false);

        return await parser.ParseDocumentAsync(stream, ct).
            ConfigureAwait(false);
    }
}
