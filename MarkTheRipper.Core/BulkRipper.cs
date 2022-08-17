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
    private readonly Ripper ripper = new();

    public BulkRipper(string storeToBasePath) =>
        this.storeToBasePath = Path.GetFullPath(storeToBasePath);

    private StoreToPathElements GetStoreToPathElements(PathEntry relativeContentPath)
    {
        var basePath = Path.GetDirectoryName(
            Path.Combine(this.storeToBasePath, relativeContentPath.RealPath))!;
        var fileName = Path.GetFileNameWithoutExtension(relativeContentPath.RealPath);
        var explicitPath = Path.Combine(basePath, fileName + ".html");
        var relativePath = explicitPath.Substring(this.storeToBasePath.Length + 1);

        return new(basePath, fileName, relativePath, explicitPath);
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
    /// <param name="metadata">Metadata context</param>
    /// <param name="contentsBasePathList">Markdown content placed directory path list</param>
    /// <remarks>Coverage</remarks>
    public ValueTask<(int count, int maxConcurrentProcessing)> RipOffAsync(
        MetadataContext metadata,
        params string[] contentsBasePathList) =>
        this.RipOffAsync(contentsBasePathList, (_, _, _, _) => default, metadata, default);

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="generated">Generated callback</param>
    /// <param name="metadata">Metadata context</param>
    /// <param name="contentsBasePathList">Markdown content placed directory path list</param>
    /// <remarks>Coverage</remarks>
    public ValueTask<(int count, int maxConcurrentProcessing)> RipOffAsync(
        Func<string, string, string, string, ValueTask> generated,
        MetadataContext metadata,
        params string[] contentsBasePathList) =>
        this.RipOffAsync(contentsBasePathList, generated, metadata, default);

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="generated">Generated callback</param>
    /// <param name="metadata">Metadata context</param>
    /// <param name="ct">CancellationToken</param>
    /// <param name="contentsBasePathList">Markdown content placed directory path list</param>
    /// <remarks>Coverage</remarks>
    public ValueTask<(int count, int maxConcurrentProcessing)> RipOffAsync(
        Func<string, string, string, string, ValueTask> generated,
        MetadataContext metadata,
        CancellationToken ct,
        params string[] contentsBasePathList) =>
        this.RipOffAsync(contentsBasePathList, generated, metadata, ct);

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="contentsBasePathList">Markdown content placed directory path iterator</param>
    /// <param name="generated">Generated callback</param>
    /// <param name="metadata">Metadata context</param>
    /// <param name="ct">CancellationToken</param>
    /// <remarks>Coverage</remarks>
    public async ValueTask<(int count, int maxConcurrentProcessing)> RipOffAsync(
        IEnumerable<string> contentsBasePathList,
        Func<string, string, string, string, ValueTask> generated,
        MetadataContext metadata,
        CancellationToken ct)
    {
        var dc = new SafeDirectoryCreator();

        var candidates = contentsBasePathList.
            Select(contentsBasePath => Path.GetFullPath(contentsBasePath)).
            Where(contentsBasePath => Directory.Exists(contentsBasePath)).
            SelectMany(contentsBasePath => Directory.EnumerateFiles(
                contentsBasePath, "*.*", SearchOption.AllDirectories).
                Select(path =>
                    (contentsBasePath,
                     relativeContentPath: new PathEntry(path.Substring(contentsBasePath.Length + 1))))).
            ToArray();

#if DEBUG
        var markdownEntries = new List<MarkdownEntry>();
        foreach (var candidate in candidates.
            Where(candidate => Path.GetExtension(candidate.relativeContentPath.RealPath) == ".md"))
        {
            var markdownEntry = await this.ripper.ParseMarkdownHeaderAsync(
                candidate.contentsBasePath,
                candidate.relativeContentPath,
                ct).
                ConfigureAwait(false);
            markdownEntries.Add(markdownEntry);
        }
#else
        var markdownEntries = await Task.WhenAll(
            candidates.
            Where(candidate => Path.GetExtension(candidate.relativeContentPath) == ".md").
            Select(candidate =>
                this.ripper.ParseMarkdownHeaderAsync(
                    candidate.contentsBasePath,
                    candidate.relativeContentPath,
                    ct).
                AsTask())).
            ConfigureAwait(false);
#endif

        var entriesByCandidate = markdownEntries.ToDictionary(
            markdownEntry => (markdownEntry.contentBasePath, markdownEntry.RelativeContentPath));

        var tagList = EntryAggregator.AggregateTags(markdownEntries, metadata);
        var rootCategory = EntryAggregator.AggregateCategories(markdownEntries, metadata);

        var mc = metadata.Spawn();
        mc.Set("tagList", tagList.Values.ToArray());
        mc.Set("rootCategory", rootCategory);

        async ValueTask RunOnceAsync(
            string contentBasePath, PathEntry relativeContentPath)
        {
            var storeToPathElements = this.GetStoreToPathElements(
                relativeContentPath);

            await dc!.CreateIfNotExistAsync(storeToPathElements.BasePath, ct).
                ConfigureAwait(false);

            if (entriesByCandidate.TryGetValue(
                (contentBasePath, relativeContentPath), out var markdownEntry))
            {
                var appliedTemplateName = await this.ripper.RenderContentAsync(
                    markdownEntry,
                    mc,
                    storeToPathElements.ExplicitPath,
                    ct).
                    ConfigureAwait(false);

                await generated(
                    relativeContentPath.RealPath,
                    storeToPathElements.RelativePath,
                    contentBasePath,
                    appliedTemplateName).
                    ConfigureAwait(false);
            }
            else
            {
                await this.CopyRelativeContentAsync(
                    relativeContentPath.RealPath, contentBasePath, ct).
                    ConfigureAwait(false);
            }
        }

        var count = 0;
        var concurrentProcessing = 0;
        var maxConcurrentProcessing = 0;
        async Task RunOnceWithMeasurementAsync(
            string contentBasePath, PathEntry relativeContentPath)
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
            await RunOnceWithMeasurementAsync(
                candidate.contentsBasePath, candidate.relativeContentPath).
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
