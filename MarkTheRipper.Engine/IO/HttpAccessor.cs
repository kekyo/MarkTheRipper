/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using MarkTheRipper.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.IO;

public sealed class HttpAccessor : IHttpAccessor
{
    private static readonly Lazy<HttpClient> httpClientFactory =
        new(() => new(new HttpClientHandler { AllowAutoRedirect = false }));
    private static readonly IReadOnlyDictionary<string, string> empty =
        new Dictionary<string, string>();

    private readonly string cacheBasePath;
    private readonly SafeDirectoryCreator safeDirectoryCreator = new();
    private readonly AsyncResourceCriticalSection cs = new();

    public HttpAccessor(string cacheBasePath) =>
        this.cacheBasePath = cacheBasePath;

    private async ValueTask<T> RunAsync<T>(
        Uri url,
        string? defaultExtHint,
        HttpContent? content,
        CultureInfo? language,
        IReadOnlyDictionary<string, string> headers,
        IReadOnlyDictionary<string, string>? cacheKeyValues,
        Func<Uri, Func<Stream>, ValueTask<T>> deserializer,
        CancellationToken ct)
    {
        // The method implementation is too complex, goals are:
        // 1. Cache prepared content from HTTP.
        // 2. Transact and observe URLs for HTTP redirection.
        // 3. Avoid any race conditions.

        var currentUrl = url;

        while (true)
        {
            // Get candidate cached (physical) path from current URL.
            var (pathBase, ext) = HttpAccessorUtilities.GetPhysicalPath(
                this.cacheBasePath, currentUrl, defaultExtHint ?? string.Empty, cacheKeyValues ??
                HttpAccessorUtilities.GetCacheKeyValues(currentUrl, language));

            // Found cached pending content.
            var targetPath = $"{pathBase}{ext}";
            var pendingTargetPath = $"{pathBase}_pending";
            if (File.Exists(pendingTargetPath))
            {
                // Accept pending file with extension when default extension is given.
                if (defaultExtHint is { })
                {
                    File.Delete(targetPath);
                    try
                    {
                        File.Move(pendingTargetPath, targetPath);
                    }
                    catch (FileNotFoundException)
                    {
                        // (Avoid race condition)
                    }
                }
                // Reuse pending file immediately.
                else
                {
                    targetPath = pendingTargetPath;
                }
            }

            // Found cached final content.
            if (File.Exists(targetPath))
            {
                Stream? cachedContentStream = null;
                try
                {
                    return await deserializer(currentUrl, () =>
                    {
                        cachedContentStream = new FileStream(
                            targetPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
                        return cachedContentStream;
                    }).
                    ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);

                    // Recovery protocol: Will remove cached file and force re-fetch.
                    cachedContentStream?.Close();
                    try
                    {
                        File.Delete(targetPath);
                    }
                    catch
                    {
                    }
                }
                finally
                {
                    cachedContentStream?.Dispose();
                }
            }

            // Found cached redirection.
            var redirectPath = $"{pathBase}_redirect.url";
            if (File.Exists(redirectPath))
            {
                using var redirectStream = new FileStream(
                    redirectPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
                var tr = new StreamReader(redirectStream);

                try
                {
                    var sectionString = await tr.ReadLineAsync().
                        WithCancellation(ct).
                        ConfigureAwait(false);
                    if (sectionString != "[InternetShortcut]")
                    {
                        throw new FormatException(
                            $"Redirection cache is invalid [1]: Path={redirectPath}");
                    }

                    var kvString = (await tr.ReadLineAsync().
                        WithCancellation(ct).
                        ConfigureAwait(false))?.
                        Trim();
                    var separatorIndex = kvString?.IndexOf('=') ?? -1;
                    if (separatorIndex == -1)
                    {
                        throw new FormatException(
                            $"Redirection cache is invalid [2]: Path={redirectPath}");
                    }

                    var key = kvString!.Substring(0, separatorIndex).Trim();
                    var value = kvString.Substring(separatorIndex + 1).Trim();
                    if (key != "URL" || !Uri.TryCreate(value, UriKind.Absolute, out var redirectUrl))
                    {
                        throw new FormatException(
                            $"Redirection cache is invalid [3]: Path={redirectPath}");
                    }

                    currentUrl = redirectUrl;
                    continue;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);

                    // Recovery protocol: Will remove cached file and force re-fetch.
                    redirectStream.Close();
                    try
                    {
                        File.Delete(redirectPath);
                    }
                    catch
                    {
                    }
                }
            }

            // Not found any cached data.

            var temporaryPath = $"{pathBase}{ext}.tmp";
            var dirPath = Utilities.GetDirectoryPath(temporaryPath);

            // Construct sub directory.
            await this.safeDirectoryCreator.CreateIfNotExistAsync(dirPath, ct).
                ConfigureAwait(false);

            // Enter asynchronous critical section by target path.
            using var _ = await this.cs.EnterAsync(targetPath, ct);

            using var temporaryStream = new FileStream(
                temporaryPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 65536, true);

            try
            {
                // Construct HTTP request.
                using var request = new HttpRequestMessage(
                    content is { }  ? HttpMethod.Post : HttpMethod.Get, currentUrl);
                if (content is { })
                {
                    request.Content = content;
                }
                if (language is { })
                {
                    if (!(content?.Headers.TryAddWithoutValidation(
                        "Accept-Language", language.IetfLanguageTag) ?? false))
                    {
                        request.Headers.Add("Accept-Language", language.IetfLanguageTag);
                    }
                }
                foreach (var entry in headers)
                {
                    if (!(content?.Headers.TryAddWithoutValidation(entry.Key, entry.Value) ?? false))
                    {
                        request.Headers.Add(entry.Key, entry.Value);
                    }
                }

                // Connect HTTP server and send.
                using var response = await httpClientFactory.Value.SendAsync(request, ct).
                    ConfigureAwait(false);

                // Needed redirection:
                if (((int)response.StatusCode / 100) == 3)
                {
                    var tw = new StreamWriter(temporaryStream, Utilities.UTF8);
                    tw.NewLine = "\n";

                    // Retreive redirection URL.
                    currentUrl = new Uri(
                        response.Headers.Location?.ToString() ?? string.Empty,
                        UriKind.Absolute);

                    // Save into cache file.
                    await tw.WriteLineAsync("[InternetShortcut]").
                        ConfigureAwait(false);
                    await tw.WriteLineAsync($"URL={currentUrl}").
                        ConfigureAwait(false);
                    await tw.FlushAsync().
                        ConfigureAwait(false);

                    temporaryStream.Close();

                    // Enable cache file.
                    File.Delete(redirectPath);
                    File.Move(temporaryPath, redirectPath);
                }
                // Other:
                else
                {
                    // Save HTTP response content.
                    response.EnsureSuccessStatusCode();

                    await response.Content.CopyToAsync(temporaryStream).
                        WithCancellation(ct).
                        ConfigureAwait(false);
                    await temporaryStream.FlushAsync().
                        ConfigureAwait(false);

                    temporaryStream.Close();

                    // Commit target path.
                    //   The file will be made pending state when default extension hint is not given.
                    //   Final (NOT redirection) response contains genuine content body,
                    //   but we don't know applicable extension when this time.
                    var exactTargetPath = defaultExtHint is { } ?
                        targetPath : pendingTargetPath;

                    // Enable cache file.
                    File.Delete(exactTargetPath);
                    File.Move(temporaryPath, exactTargetPath);

                    Stream? cachedContentStream = null;
                    try
                    {
                        return await deserializer(currentUrl, () =>
                        {
                            cachedContentStream = new FileStream(
                                exactTargetPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
                            return cachedContentStream;
                        }).
                        ConfigureAwait(false);
                    }
                    finally
                    {
                        cachedContentStream?.Dispose();
                    }
                }
            }
            catch
            {
                temporaryStream.Close();
                File.Delete(temporaryPath);
                throw;
            }
        }
    }

    public ValueTask<JToken> FetchJsonAsync(
        Uri url, CultureInfo requestLanguage, CancellationToken ct) =>
        this.RunAsync(
            url, ".json",
            null, requestLanguage,
            empty, null,
            (url, streamOpener) =>
                InternalUtilities.DefaultJsonSerializer.DeserializeJsonAsync(streamOpener(), ct),
            ct);

    public async ValueTask<JToken> PostJsonAsync(
        Uri url, JToken requestJson,
        IReadOnlyDictionary<string, string> headers,
        IReadOnlyDictionary<string, string> cacheKeyValues,
        CancellationToken ct)
    {
        using var content = new StringContent(
            requestJson.ToString(),
            Utilities.UTF8,
            "application/json");
        return await this.RunAsync(
            url, ".json",
            content, null,
            headers, cacheKeyValues,
            (url, streamOpener) =>
                InternalUtilities.DefaultJsonSerializer.DeserializeJsonAsync(streamOpener(), ct),
            ct);
    }

    public ValueTask<IHtmlDocument> FetchHtmlAsync(
        Uri url, CultureInfo requestLanguage, CancellationToken ct) =>
        this.RunAsync(
            url, ".html",
            null, requestLanguage,
            empty, null,
            (url, streamOpener) => new ValueTask<IHtmlDocument>(
                new HtmlParser().ParseDocumentAsync(streamOpener(), ct)),
            ct);

    public ValueTask<Uri> ExamineShortUrlAsync(
        Uri url, CultureInfo requestLanguage, CancellationToken ct) =>
        this.RunAsync(
            url,
            null,  // Make pending
            null, requestLanguage,
            empty, null,
            (url, streamOpener) => new ValueTask<Uri>(url),
            ct);
}
