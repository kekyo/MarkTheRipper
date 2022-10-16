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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Metadata;

internal static class EntryAggregator
{
    private static DateTimeOffset GetDate(MarkdownEntry? markdownEntry) =>
        markdownEntry?.Date is DateTimeOffset dto ?
            dto :
            DateTimeOffset.MaxValue;

    public static async ValueTask<Dictionary<string, TagEntry>> AggregateTagsAsync(
        IEnumerable<MarkdownEntry> markdownEntries,
        IMetadataContext metadata,
        CancellationToken ct) =>
        (await Task.WhenAll(markdownEntries.Select(async markdownEntry =>
            await markdownEntry.GetPropertyValueAsync("tags", metadata, Reducer.Instance, ct).
                ConfigureAwait(false) is { } tagsValue ?
                MetadataUtilities.EnumerateValue(tagsValue, metadata).
                    OfType<PartialTagEntry>().
                    Select(tag => (tag, markdownEntry)).
                    Where(entry => !string.IsNullOrWhiteSpace(entry.tag.Name)).
                    ToArray() :
                InternalUtilities.Empty<(PartialTagEntry tag, MarkdownEntry markdownEntry)>())).
            ConfigureAwait(false)).
            SelectMany(entries => entries).
            GroupBy(entry => entry.tag.Name).
            ToDictionary(
                g => g.Key,
                g => new TagEntry(
                    g.Key,
                    g.Select(entry => entry.markdownEntry).
                    OrderBy(
                        markdownEntry => markdownEntry,
                        new DelegatedComparer<MarkdownEntry?>((lhs, rhs) => GetDate(lhs).CompareTo(GetDate(rhs)))).
                    ToArray()));

    private static async ValueTask<CategoryEntry> AggregateCategoryAsync(
        string categoryName,
        IEnumerable<MarkdownEntry> markdownEntries,
        int levelIndex,
        IMetadataContext metadata, 
        CancellationToken ct)
    {
        var categoryLists = await Task.WhenAll(markdownEntries.
            Select(async markdownEntry =>
                (markdownEntry,
                 categoryList:
                    await markdownEntry.GetPropertyValueAsync("category", metadata, Reducer.Instance, ct).
                        ConfigureAwait(false) is PartialCategoryEntry entry ?
                    entry.Unfold(e => e.Parent).Reverse().Skip(1).ToArray() :
                    InternalUtilities.Empty<PartialCategoryEntry>()))).
            ConfigureAwait(false);

        var childCategoryEntries = (await Task.WhenAll(categoryLists.
            Where(entry => entry.categoryList.Length > levelIndex).
            GroupBy(entry => entry.categoryList[levelIndex].Name).
            Select(async g => (key: g.Key, values: await AggregateCategoryAsync(
                    g.Key,
                    g.Select(entry => entry.markdownEntry),
                    levelIndex + 1,
                    metadata,
                    ct).
                    ConfigureAwait(false)))).
            ConfigureAwait(false)).
            ToDictionary(
                g => g.key,
                g => g.values);

        var childEntries = categoryLists.
            Where(entry => entry.categoryList.Length <= levelIndex).
            Select(entry => entry.markdownEntry).
            ToArray();

        return new(categoryName, childCategoryEntries, childEntries);
    }

    public static ValueTask<CategoryEntry> AggregateCategoriesAsync(
        IEnumerable<MarkdownEntry> markdownEntries,
        IMetadataContext metadata,
        CancellationToken ct) =>
        AggregateCategoryAsync("(root)", markdownEntries, 0, metadata, ct);
}
