/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper;

/// <summary>
/// Bulk rip off and generate static site.
/// </summary>
public sealed class BulkRipper
{
    private sealed class StoreToPathElements
    {
        public readonly string BasePath;
        public readonly string FileName;
        public readonly string RelativePath;
        public readonly string ExplicitPath;

        public StoreToPathElements(
            string basePath, string fileName,
            string relativePath, string explicitPath)
        {
            this.BasePath = basePath;
            this.FileName = fileName;
            this.RelativePath = relativePath;
            this.ExplicitPath = explicitPath;
        }
    }

    private readonly string storeToBasePath;
    private readonly Func<string, object?> getMetadata;
    private readonly Ripper ripper;

    public BulkRipper(
        string storeToBasePath,
        Func<string, RootTemplateNode?> getTemplate,
        Func<string, object?> getMetadata)
    {
        this.storeToBasePath = Path.GetFullPath(storeToBasePath);
        this.getMetadata = getMetadata;
        this.ripper = new Ripper(getTemplate);
    }

    private StoreToPathElements GetStoreToPathElements(string relativeContentPath)
    {
        var basePath = Path.GetDirectoryName(
            Path.Combine(this.storeToBasePath, relativeContentPath))!;
        var fileName = Path.GetFileNameWithoutExtension(relativeContentPath);
        var explicitPath = Path.Combine(basePath, fileName + ".html");
        var relativePath = explicitPath.Substring(this.storeToBasePath.Length + 1);

        return new(basePath, fileName, relativePath, explicitPath);
    }

    private async ValueTask<(string storeToRelativePath, string appliedTemplateName)> RipOffRelativeContentAsync(
        StoreToPathElements storeToPathElements,
        MarkdownEntry markdownEntry,
        CancellationToken ct)
    {
        object? GetMetadata(string keyName) =>
            markdownEntry.GetProperty(keyName) is { } value ?
                value :
                this.getMetadata(keyName);

        var appliedTemplateName = await this.ripper.RenderContentAsync(
            markdownEntry, GetMetadata, storeToPathElements.ExplicitPath, ct).
            ConfigureAwait(false);

        return (storeToPathElements.RelativePath, appliedTemplateName);
    }

    /// <summary>
    /// Copy content into target path.
    /// </summary>
    /// <param name="contentStream">Content stream</param>
    /// <param name="storeToPath">Store to path</param>
    /// <param name="ct">CancellationToken</param>
    public static async ValueTask CopyContentToAsync(
        Stream contentStream,
        string storeToPath,
        CancellationToken ct)
    {
        using var storeToStream = new FileStream(
            storeToPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 65536, true);

        var buffer = new byte[65536];
        while (true)
        {
            var read = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct).
                ConfigureAwait(false);
            if (read <= 0)
            {
                break;
            }

            await storeToStream.WriteAsync(buffer, 0, read, ct).
                ConfigureAwait(false);
        }

        await storeToStream.FlushAsync(ct).
            ConfigureAwait(false);
    }

    private async ValueTask CopyRelativeContentAsync(
        string relativeContentPath,
        string contentsBasePath,
        CancellationToken ct)
    {
        var contentPath = Path.Combine(
            contentsBasePath, relativeContentPath);
        var storeToPath = Path.Combine(
            this.storeToBasePath, relativeContentPath);

        using var cs = new FileStream(
            contentPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);

        await CopyContentToAsync(cs, storeToPath, ct).
            ConfigureAwait(false);
    }

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="contentsBasePathList">Markdown content placed directory path list</param>
    /// <remarks>Coverage</remarks>
    public ValueTask<(int count, int maxConcurrentProcessing)> RipOffAsync(
        params string[] contentsBasePathList) =>
        this.RipOffAsync(contentsBasePathList, (_, _, _, _) => default, default);

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="generated">Generated callback</param>
    /// <param name="contentsBasePathList">Markdown content placed directory path list</param>
    /// <remarks>Coverage</remarks>
    public ValueTask<(int count, int maxConcurrentProcessing)> RipOffAsync(
        Func<string, string, string, string, ValueTask> generated,
        params string[] contentsBasePathList) =>
        this.RipOffAsync(contentsBasePathList, generated, default);

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="generated">Generated callback</param>
    /// <param name="ct">CancellationToken</param>
    /// <param name="contentsBasePathList">Markdown content placed directory path list</param>
    /// <remarks>Coverage</remarks>
    public ValueTask<(int count, int maxConcurrentProcessing)> RipOffAsync(
        Func<string, string, string, string, ValueTask> generated,
        CancellationToken ct, params string[] contentsBasePathList) =>
        this.RipOffAsync(contentsBasePathList, generated, ct);

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="contentsBasePathList">Markdown content placed directory path iterator</param>
    /// <param name="generated">Generated callback</param>
    /// <param name="ct">CancellationToken</param>
    /// <remarks>Coverage</remarks>
    public async ValueTask<(int count, int maxConcurrentProcessing)> RipOffAsync(
        IEnumerable<string> contentsBasePathList,
        Func<string, string, string, string, ValueTask> generated,
        CancellationToken ct)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var dc = new SafeDirectoryCreator();

        var candidates = contentsBasePathList.
            Select(contentsBasePath => Path.GetFullPath(contentsBasePath)).
            Where(contentsBasePath => Directory.Exists(contentsBasePath)).
            SelectMany(contentsBasePath => Directory.EnumerateFiles(
                contentsBasePath, "*.*", SearchOption.AllDirectories).
                Select(path =>
                    (contentsBasePath,
                     relativeContentPath: path.Substring(contentsBasePath.Length + 1)))).
            ToArray();

#if DEBUG
        var markdownEntries = new List<MarkdownEntry>();
        foreach (var candidate in candidates.
            Where(candidate => Path.GetExtension(candidate.relativeContentPath) == ".md"))
        {
            var markdownEntry = await ripper.ParseMarkdownHeaderAsync(
                candidate.contentsBasePath, candidate.relativeContentPath, ct).
                ConfigureAwait(false);
            markdownEntries.Add(markdownEntry);
        }
#else
        var markdownEntries = await Task.WhenAll(
            candidates.
            Where(candidate => Path.GetExtension(candidate.relativeContentPath) == ".md").
            Select(candidate =>
                ripper.ParseMarkdownHeaderAsync(
                    candidate.contentsBasePath, candidate.relativeContentPath, ct).
                AsTask())).
            ConfigureAwait(false);
#endif

        var headerByCandidate = markdownEntries.ToDictionary(
            markdownEntry => (markdownEntry.ContentBasePath, markdownEntry.RelativePath));

        var tags = EntryAggregator.AggregateTags(markdownEntries);
        var categories = EntryAggregator.AggregateCategories(markdownEntries);

        async ValueTask RunOnceAsync(string contentBasePath, string relativeContentPath)
        {
            var storeToPathElements = this.GetStoreToPathElements(
                relativeContentPath);

            await dc!.CreateIfNotExistAsync(storeToPathElements.BasePath, ct).
                ConfigureAwait(false);

            if (headerByCandidate.TryGetValue(
                (contentBasePath, relativeContentPath), out var markdownEntry))
            {
                var (relativeGeneratedPath, appliedTemplateName) =
                    await this.RipOffRelativeContentAsync(
                        storeToPathElements, markdownEntry, ct).
                        ConfigureAwait(false);

                await generated(
                    relativeContentPath,
                    relativeGeneratedPath,
                    contentBasePath,
                    appliedTemplateName).
                    ConfigureAwait(false);
            }
            else
            {
                await this.CopyRelativeContentAsync(
                    relativeContentPath, contentBasePath, ct).
                    ConfigureAwait(false);
            }
        }

        var count = 0;
        var concurrentProcessing = 0;
        var maxConcurrentProcessing = 0;
        async Task RunOnceWithMeasurementAsync(string contentBasePath, string relativeContentPath)
        {
            count++;
            var cp = Interlocked.Increment(ref concurrentProcessing);
            maxConcurrentProcessing = Math.Max(maxConcurrentProcessing, cp);

            try
            {
                await RunOnceAsync(contentBasePath, relativeContentPath).
                    ConfigureAwait(false);
            }
            catch
            {
                Interlocked.Decrement(ref concurrentProcessing);
                throw;
            }

            Interlocked.Decrement(ref concurrentProcessing);
        }

#if DEBUG
        foreach (var candidate in candidates)
        {
            await RunOnceWithMeasurementAsync(candidate.contentsBasePath, candidate.relativeContentPath).
                ConfigureAwait(false);
        }
#else
        await Task.WhenAll(candidates.
            Select(candidate => RunOnceWithMeasurementAsync(candidate.contentsBasePath, candidate.relativeContentPath))).
            ConfigureAwait(false);
#endif

        return (count, maxConcurrentProcessing);
    }
}
