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
        public readonly string DirPath;
        public readonly string FileName;
        public readonly string RelativePath;

        public StoreToPathElements(
            string dirPath, string fileName, string relativePath)
        {
            this.DirPath = dirPath;
            this.FileName = fileName;
            this.RelativePath = relativePath;
        }
    }

    private readonly string storeToBasePath;
    private readonly Ripper ripper;

    public BulkRipper(Ripper ripper, string storeToBasePath)
    {
        this.ripper = ripper;
        this.storeToBasePath = Path.GetFullPath(storeToBasePath);
    }

    private StoreToPathElements GetStoreToPathElements(PathEntry markdownPath)
    {
        var dirPath = Utilities.GetDirectoryPath(
            Path.Combine(this.storeToBasePath, markdownPath.PhysicalPath));
        var fileName = Path.GetFileNameWithoutExtension(dirPath);
        var explicitPath = Path.Combine(dirPath, fileName + ".html");
        var relativePath = explicitPath.Substring(this.storeToBasePath.Length + 1);

        return new(dirPath, fileName, relativePath);
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
            var read = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct);
            if (read <= 0)
            {
                break;
            }

            await storeToStream.WriteAsync(buffer, 0, read, ct);
        }

        await storeToStream.FlushAsync(ct);
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

        await CopyContentToAsync(cs, storeToPath, ct);
    }

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="metadata">Metadata context</param>
    /// <param name="contentsBasePathList">Markdown content placed directory path list</param>
    /// <remarks>Coverage</remarks>
    public ValueTask<(int count, int maxConcurrentProcessing)> RipOffAsync(
        IMetadataContext metadata,
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
        IMetadataContext metadata,
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
        IMetadataContext metadata,
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
        IMetadataContext metadata,
        CancellationToken ct)
    {
        var dc = new SafeDirectoryCreator();

        var candidates = contentsBasePathList.
            Select(contentsBasePath => Path.GetFullPath(contentsBasePath)).
            SelectMany(contentsBasePath => InternalUtilities.EnumerateAllFiles(
                contentsBasePath, "*.*").
                Select(path =>
                    (contentsBasePath,
                     relativeContentPath: new PathEntry(path.Substring(contentsBasePath.Length + 1))))).
            ToArray();

        var generatedDate = metadata.Lookup("generated") is ValueExpression(DateTimeOffset gd) ?
            gd : DateTimeOffset.Now;

#if DEBUG
        var markdownEntries = new List<MarkdownEntry>();
        foreach (var candidate in candidates.
            Where(candidate => Path.GetExtension(candidate.relativeContentPath.PhysicalPath) == ".md"))
        {
            var markdownEntry = await this.ripper.ParseMarkdownHeaderAsync(
                candidate.contentsBasePath,
                candidate.relativeContentPath,
                generatedDate,
                ct);
            markdownEntries.Add(markdownEntry);
        }
#else
        var markdownEntries = await Task.WhenAll(
            candidates.
            Where(candidate => Path.GetExtension(candidate.relativeContentPath.PhysicalPath) == ".md").
            Select(candidate =>
                this.ripper.ParseMarkdownHeaderAsync(
                    candidate.contentsBasePath,
                    candidate.relativeContentPath,
                    generatedDate,
                    ct).
                AsTask()));
#endif

        var entriesByCandidate = markdownEntries.ToDictionary(
            markdownEntry => markdownEntry.MarkdownPath);

        var tagList = await EntryAggregator.AggregateTagsAsync(
            markdownEntries.Where(entry => !entry.DoesNotPublish), metadata, ct);
        var rootCategory = await EntryAggregator.AggregateCategoriesAsync(
            markdownEntries.Where(entry => !entry.DoesNotPublish), metadata, ct);

        var mc = metadata.Spawn();
        mc.SetValue("tagList", tagList.Values.ToArray());
        mc.SetValue("rootCategory", rootCategory);

        async ValueTask RunOnceAsync(
            string contentBasePath, PathEntry relativeContentPath)
        {
            var storeToPathElements = this.GetStoreToPathElements(
                relativeContentPath);

            await dc!.CreateIfNotExistAsync(storeToPathElements.DirPath, ct);

            if (entriesByCandidate.TryGetValue(
                relativeContentPath, out var markdownEntry))
            {
                if (!markdownEntry.DoesNotPublish)
                {
                    var appliedLayoutPath = await this.ripper.RenderContentAsync(
                        contentBasePath,
                        markdownEntry,
                        mc,
                        this.storeToBasePath,
                        ct);

                    await generated(
                        relativeContentPath.PhysicalPath,
                        storeToPathElements.RelativePath,
                        contentBasePath,
                        appliedLayoutPath.PhysicalPath);
                }
            }
            else
            {
                await this.CopyRelativeContentAsync(
                    relativeContentPath.PhysicalPath, contentBasePath, ct);
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
                await RunOnceAsync(contentBasePath, relativeContentPath);
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
                candidate.contentsBasePath, candidate.relativeContentPath);
        }
#else
        await Task.WhenAll(candidates.
            Select(candidate => RunOnceWithMeasurementAsync(
                candidate.contentsBasePath, candidate.relativeContentPath)));
#endif

        return (count, maxConcurrentProcessing);
    }
}
