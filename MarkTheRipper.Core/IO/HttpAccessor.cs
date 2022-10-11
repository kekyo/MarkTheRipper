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
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.IO;

public sealed class HttpAccessor : IHttpAccessor
{
    private static readonly Lazy<HttpClient> httpClientFactory =
        new(() => new());
    private static readonly Lazy<HttpClient> nonRedirectedHttpClientFactory =
        new(() => new(new HttpClientHandler() { AllowAutoRedirect = false }));
    private static readonly HashSet<char> sanitizeChars = new()
    {
        '~', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')',
        '+', '=', '[', ']', '{', '}', ';', '<', '>', '|', '?',
        ',', '\'', '"', '`',
    };

    private readonly string cacheBasePath;
    private readonly SafeDirectoryCreator safeDirectoryCreator = new();

    public HttpAccessor(string cacheBasePath) =>
        this.cacheBasePath = cacheBasePath;

    private static IReadOnlyDictionary<string, string> GetCacheKeyValuesFrom(Uri url) =>
        url.Query.StartsWith("?") ?
            url.Query.TrimStart('?').
            Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries).
            Select(kv =>
            {
                var splitted = kv.Split('=');
                return (key: splitted[0], value: splitted.ElementAtOrDefault(1));
            }).
            ToDictionary(kv => kv.key, kv => kv.value)! :
            new Dictionary<string, string>();

    private static string CalculateHashPostfix(
        IReadOnlyDictionary<string, string> cacheKeyValues)
    {
        if (cacheKeyValues.Count >= 1)
        {
            using var ms = new MemoryStream();
            var tw = new StreamWriter(ms);
            foreach (var kv in cacheKeyValues.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
            {
                tw.Write(kv.Key);
                tw.Write('=');
                tw.Write(kv.Value);
                tw.Write(',');
            }
            tw.Flush();
            ms.Position = 0;

            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(ms);

            return "_" +
                BitConverter.ToString(hash).
                Replace("-", string.Empty).
                ToLowerInvariant();
        }
        else
        {
            return string.Empty;
        }
    }

    private async ValueTask<Stream> GetResourceStreamAsync(
        Uri url, string defaultExtHint,
        IReadOnlyDictionary<string, string> cacheKeyValues,
        Func<Stream, ValueTask> fetcher,
        CancellationToken ct)
    {
        var hashPostfix = CalculateHashPostfix(cacheKeyValues);
        var host = url.IsDefaultPort ? url.Host : $"{url.Host}_{url.Port}";

        var fileNameHint = url.LocalPath.Split('/').LastOrDefault() ??
            "index";
        var ext = Path.GetExtension(fileNameHint);
        if (ext.Length == 0 || ext == ".")
        {
            ext = defaultExtHint;
        }

        var fileName =
            Path.GetFileNameWithoutExtension(fileNameHint) +
            hashPostfix +
            ext;
        var path = Path.Combine(
            this.cacheBasePath, host, fileName);
        var dirPath = Utilities.GetDirectoryName(path);

        await this.safeDirectoryCreator.CreateIfNotExistAsync(
            dirPath, ct).
            ConfigureAwait(false);

        if (!File.Exists(path))
        {
            using var targetStream = new FileStream(
                path + ".tmp", FileMode.Create, FileAccess.ReadWrite, FileShare.None, 65536, true);

            try
            {
                await fetcher(targetStream).
                    ConfigureAwait(false);
                await targetStream.FlushAsync().
                    ConfigureAwait(false);

                targetStream.Close();
                File.Delete(path);
                File.Move(path + ".tmp", path);
            }
            catch
            {
                targetStream.Close();
                File.Delete(path + ".tmp");
                throw;
            }
        }

        return new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
    }

    public async ValueTask<JToken> FetchJsonAsync(
        Uri url, CancellationToken ct)
    {
        var cacheKeyValues = GetCacheKeyValuesFrom(url);
        using var targetStream = await this.GetResourceStreamAsync(
            url, ".json", cacheKeyValues,
            async targetStream =>
            {
                using var responseStream = await httpClientFactory.Value.GetStreamAsync(url).
                    WithCancellation(ct).
                    ConfigureAwait(false);

                await responseStream.CopyToAsync(targetStream).
                    ConfigureAwait(false);
                await targetStream.FlushAsync().
                    ConfigureAwait(false);
            },
            ct);

        return await InternalUtilities.DefaultJsonSerializer.DeserializeJsonAsync(targetStream, ct).
            ConfigureAwait(false);
    }

    public async ValueTask<JToken> PostJsonAsync(
        Uri url, JToken requestJson,
        IReadOnlyDictionary<string, string> headers,
        IReadOnlyDictionary<string, string> cacheKeyValues,
        CancellationToken ct)
    {
        using var targetStream = await this.GetResourceStreamAsync(
            url, ".json", cacheKeyValues,
            async targetStream =>
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

                using var responseStream = await response.Content.ReadAsStreamAsync().
                    WithCancellation(ct).
                    ConfigureAwait(false);

                await responseStream.CopyToAsync(targetStream).
                    ConfigureAwait(false);
                await targetStream.FlushAsync().
                    ConfigureAwait(false);
            },
            ct);

        return await InternalUtilities.DefaultJsonSerializer.
            DeserializeJsonAsync(targetStream, ct).
            ConfigureAwait(false);
    }

    public async ValueTask<IHtmlDocument> FetchHtmlAsync(
        Uri url, CancellationToken ct)
    {
        var cacheKeyValues = GetCacheKeyValuesFrom(url);
        using var targetStream = await this.GetResourceStreamAsync(
            url, ".html", cacheKeyValues,
            async targetStream =>
            {
                using var responseStream = await httpClientFactory.Value.GetStreamAsync(url).
                    WithCancellation(ct).
                    ConfigureAwait(false);

                await responseStream.CopyToAsync(targetStream).
                    ConfigureAwait(false);
                await targetStream.FlushAsync().
                    ConfigureAwait(false);
            },
            ct);

        var parser = new HtmlParser();
        return await parser.ParseDocumentAsync(targetStream, ct).
            ConfigureAwait(false);
    }

    public async ValueTask<Uri?> ExamineShortUrlAsync(
        Uri url, CancellationToken ct)
    {
        var cacheKeyValues = GetCacheKeyValuesFrom(url);
        using var targetStream = await this.GetResourceStreamAsync(
            url, ".txt", cacheKeyValues,
            async targetStream =>
            {
                using var response = await nonRedirectedHttpClientFactory.Value.GetAsync(url, ct).
                    ConfigureAwait(false);

                var tw = new StreamWriter(targetStream);
                if (((int)response.StatusCode / 100) == 3)
                {
                    await tw.WriteAsync(response.Headers.Location?.ToString() ?? string.Empty).
                        ConfigureAwait(false);
                    await tw.FlushAsync().
                        ConfigureAwait(false);
                }
            },
            ct);

        var tr = new StreamReader(targetStream);
        var location = await tr.ReadToEndAsync().
            ConfigureAwait(false);
        return Uri.TryCreate(location, UriKind.Absolute, out var redirectUrl) ?
            redirectUrl : null;
    }
}
