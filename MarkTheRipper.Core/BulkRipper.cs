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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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

    private async ValueTask<(string storeToRelativePath, string appliedTemplateName)> RipOffRelativeContentAsync(
        string relativeContentPath,
        string contentsBasePath,
        CancellationToken ct)
    {
        var storeToBasePath = Path.GetDirectoryName(
            Path.Combine(this.storeToBasePath, relativeContentPath))!;
        var storeToFileName = Path.GetFileNameWithoutExtension(relativeContentPath);
        var storeToPath = Path.Combine(storeToBasePath, storeToFileName + ".html");
        var storeToRelativePath = storeToPath.Substring(this.storeToBasePath.Length + 1);

        object? GetMetadata(string keyName) =>
            keyName == "category" &&
            relativeContentPath.Split(new[] {
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar
            }) is { } categories &&
            categories.Length >= 2 ?
                categories.Take(categories.Length - 1).ToArray() :
                this.getMetadata(keyName);

        var markdownHeader = await this.ripper.ParseMarkdownHeaderAsync(
            contentsBasePath, relativeContentPath, ct).
            ConfigureAwait(false);

        var appliedTemplateName = await this.ripper.RenderContentAsync(
            contentsBasePath, markdownHeader, GetMetadata, storeToPath, ct).
            ConfigureAwait(false);

        return (storeToRelativePath, appliedTemplateName);
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

    public sealed class TagEntry :
        IEnumerable<MarkdownHeader>
    {
        public readonly string TagName;
        public readonly (string contentBasePath, MarkdownHeader markdownHeader)[] Headers;

        public TagEntry(
            string tagName,
            (string contentBasePath, MarkdownHeader markdownHeader)[] headers)
        {
            this.TagName = tagName;
            this.Headers = headers;
        }

        public IEnumerator<MarkdownHeader> GetEnumerator() =>
            this.Headers.Select(entry => entry.markdownHeader).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            this.GetEnumerator();
    }

    private sealed class MarkdownHeaderDateComparer :
        IComparer<MarkdownHeader>
    {
        public static readonly MarkdownHeaderDateComparer Instance =
            new MarkdownHeaderDateComparer();

        private MarkdownHeaderDateComparer()
        {
        }

        public int Compare(MarkdownHeader? x, MarkdownHeader? y)
        {
            var xd = (DateTimeOffset)x!.Metadata["date"]!;
            var yd = (DateTimeOffset)y!.Metadata["date"]!;
            return xd.CompareTo(yd);
        }
    }

    public sealed class CategoryEntry
    {
        public readonly Dictionary<string, List<CategoryEntry>> Entries =
            new();
        public readonly SortedSet<MarkdownHeader> Headers =
            new(MarkdownHeaderDateComparer.Instance);
    }

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

#if DEBUG
        var candidates = contentsBasePathList.
            Select(contentsBasePath => Path.GetFullPath(contentsBasePath)).
            Where(contentsBasePath => Directory.Exists(contentsBasePath)).
            SelectMany(contentsBasePath => Directory.EnumerateFiles(
                contentsBasePath, "*.*", SearchOption.AllDirectories).
                Select(path => (contentsBasePath, path))).
            ToArray();

        var parsedEntries = await Task.WhenAll(
            candidates.Select(async candidate =>
                (candidate.contentsBasePath,
                 markdownHeader: await ripper.ParseMarkdownHeaderAsync(
                    candidate.contentsBasePath, candidate.path, ct).
                    ConfigureAwait(false)))).
            ConfigureAwait(false);

        var tags = AggregateTags(parsedEntries);
        var categories = AggregateCategories(parsedEntries);

        //object? GetMetadata(string keyName) =>
        //    tags!.TryGetValue(keyName, out var headers) ?
        //        headers :
        //        this.getMetadata(keyName);

        //await Task.WhenAll(
        //    parsedEntries.Select(markdownHeader =>
        //    {
        //        this.ripper.RenderContentAsync(contentBasePath, markdownHeader,)
        //    }));
        //        this.ripper.ParseMarkdownHeaderAsync(
        //            candidate.contentsBasePath, candidate.path, ct).
        //            AsTask())).
        //    ConfigureAwait(false);

        //{
        //    var markdownHeader = await this.ripper.ParseMarkdownHeaderAsync(
        //        candidate.contentsBasePath, candidate.path, ct).
        //        ConfigureAwait(false);

        //    await RunOnceWithMeasurementAsync(candidate.contentsBasePath, candidate.path).
        //        ConfigureAwait(false);
        //}
#else
        await Task.WhenAll(candidates.
            Select(candidate => RunOnceWithMeasurementAsync(candidate.contentsBasePath, candidate.path))).
            ConfigureAwait(false);
#endif

        async ValueTask RunOnceAsync(string contentsBasePath, string contentsPath)
        {
            var relativeContentPath = contentsPath.Substring(
                contentsBasePath.Length +
                (contentsBasePath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? 0 : 1));

            var storeToPath = Path.Combine(this.storeToBasePath, relativeContentPath);
            var storeToDirPath = Path.GetDirectoryName(storeToPath)!;

            await dc!.CreateIfNotExistAsync(storeToDirPath, ct).
                ConfigureAwait(false);

            if (Path.GetExtension(relativeContentPath) == ".md")
            {
                var (relativeGeneratedPath, appliedTemplateName) =
                    await this.RipOffRelativeContentAsync(
                        relativeContentPath,
                        contentsBasePath,
                        ct).
                        ConfigureAwait(false);

                await generated(
                    relativeContentPath,
                    relativeGeneratedPath,
                    contentsBasePath,
                    appliedTemplateName).
                    ConfigureAwait(false);
            }
            else
            {
                await this.CopyRelativeContentAsync(
                    relativeContentPath, contentsBasePath, ct).
                    ConfigureAwait(false);
            }
        }

        var count = 0;
        var concurrentProcessing = 0;
        var maxConcurrentProcessing = 0;
        async Task RunOnceWithMeasurementAsync(string contentsBasePath, string contentsPath)
        {
            count++;
            var cp = Interlocked.Increment(ref concurrentProcessing);
            maxConcurrentProcessing = Math.Max(maxConcurrentProcessing, cp);

            try
            {
                await RunOnceAsync(contentsBasePath, contentsPath).
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
            await RunOnceWithMeasurementAsync(candidate.contentsBasePath, candidate.path).
                ConfigureAwait(false);
        }
#else
        await Task.WhenAll(candidates.
            Select(candidate => RunOnceWithMeasurementAsync(candidate.contentsBasePath, candidate.path))).
            ConfigureAwait(false);
#endif

        return (count, maxConcurrentProcessing);
    }

    internal static Dictionary<string, TagEntry> AggregateTags(
        IEnumerable<(string contentsBasePath, MarkdownHeader markdownHeader)> parsedEntries) =>
        parsedEntries.SelectMany(entry =>
             entry.markdownHeader.Metadata.TryGetValue("tags", out var tags) ?
                Utilities.EnumerateValue(tags).
                Select(tagName =>
                    (tagName: Utilities.FormatValue(tagName, null, CultureInfo.InvariantCulture)!,
                        contentBasePath: entry.contentsBasePath,
                        markdownHeader: entry.markdownHeader)).
                Where(entry => !string.IsNullOrWhiteSpace(entry.tagName)) :
                Utilities.Empty<(string, string, MarkdownHeader)>()).
            GroupBy(entry => entry.tagName).
            ToDictionary(
                g => g.Key,
                g => new TagEntry(
                    g.Key,
                    g.Select(entry => (entry.contentBasePath, entry.markdownHeader)).
                    ToArray()));

    internal static CategoryEntry AggregateCategories(
        IEnumerable<(string contentsBasePath, MarkdownHeader markdownHeader)> parsedEntries)
    {
        var categoryNameLists = parsedEntries.Select(entry =>
            (entry.contentsBasePath,
             entry.markdownHeader,
             categoryNames: entry.markdownHeader.Metadata.TryGetValue("category", out var categoryNameList) ?
                Utilities.EnumerateValue(categoryNameList).
                Select(categoryName => Utilities.FormatValue(categoryName, null, CultureInfo.InvariantCulture)!).
                Where(categoryName => !string.IsNullOrWhiteSpace(categoryName)).
                ToArray() :
                (Path.GetDirectoryName(entry.markdownHeader.RelativeContentPath) ?? Path.DirectorySeparatorChar.ToString()).
                Split(new[] {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar },
                    StringSplitOptions.RemoveEmptyEntries))).
            ToArray();

        void AggregateCategory(CategoryEntry categoryEntry, int levelIndex)
        {
            foreach (var categoryNameList in categoryNameLists)
            {
                var categoryName = categoryNameList.categoryNames[levelIndex];

                if (!categoryEntry.Entries.TryGetValue(categoryName, out var entries))
                {
                    entries = new();
                    categoryEntry.Entries.Add(categoryName, entries);
                }

                var nextEntry = new CategoryEntry();
                entries.Add(nextEntry);

                if (levelIndex >= (categoryNameList.categoryNames.Length - 1))
                {
                    categoryEntry.Headers.Add(categoryNameList.markdownHeader);
                }
                else
                {
                    AggregateCategory(nextEntry, levelIndex + 1);
                }
            }
        }

        var categoryEntry0 = new CategoryEntry();
        AggregateCategory(categoryEntry0, 0);

        return categoryEntry0;
    }
}
