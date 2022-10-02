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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.IO;

public sealed class HttpAccessor : IHttpAccessor
{
    private static readonly Lazy<HttpClient> httpClientFactory =
        new(() => new());
    private static readonly Lazy<HttpClient> nonRedirectedHttpClientFactory =
        new(() => new(new HttpClientHandler() { AllowAutoRedirect = false }));

    public static readonly IHttpAccessor Instance =
        new HttpAccessor();

    private HttpAccessor()
    {
    }

    public async ValueTask<JToken> FetchJsonAsync(
        Uri url, CancellationToken ct)
    {
        using var stream = await httpClientFactory.Value.GetStreamAsync(url).
            WithCancellation(ct).
            ConfigureAwait(false);

        return await InternalUtilities.DefaultJsonSerializer.DeserializeJsonAsync(stream, ct).
            ConfigureAwait(false);
    }

    public async ValueTask<JToken> PostJsonAsync(
        Uri url, JToken requestJson,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, url);

        var content = new StringContent(
            requestJson.ToString(),
            Utilities.UTF8,
            "application/json");
        request.Content = content;

        foreach (var entry in headers)
        {
            if (!content.Headers.TryAddWithoutValidation(entry.Key, entry.Value))
            {
                request.Headers.Add(entry.Key, entry.Value);
            }
        }

        using var response = await httpClientFactory.Value.
            SendAsync(request, ct).
            ConfigureAwait(false);

        using var contentStream = await response.Content.ReadAsStreamAsync().
            WithCancellation(ct).
            ConfigureAwait(false);
#if DEBUG
        var stream = new MemoryStream();
        await contentStream.CopyToAsync(stream);
        stream.Position = 0;

        var jsonString = new StreamReader(stream).ReadToEnd();
        stream.Position = 0;
#else
        var stream = contentStream;
#endif

        return await InternalUtilities.DefaultJsonSerializer.
            DeserializeJsonAsync(stream, ct).
            ConfigureAwait(false);
    }

    public async ValueTask<IHtmlDocument> FetchHtmlAsync(
        Uri url, CancellationToken ct)
    {
        var parser = new HtmlParser();

        using var stream = await httpClientFactory.Value.GetStreamAsync(url).
            WithCancellation(ct).
            ConfigureAwait(false);

        return await parser.ParseDocumentAsync(stream, ct).
            ConfigureAwait(false);
    }

    public async ValueTask<Uri?> ExamineShortUrlAsync(
        Uri url, CancellationToken ct)
    {
        using var response = await nonRedirectedHttpClientFactory.Value.
            GetAsync(url, ct).
            ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return ((int)response.StatusCode / 100) == 3 ?
            response.Headers.Location : null;
    }
}
