/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using System.Collections.Generic;
using System.Linq;

namespace MarkTheRipper.Metadata;

internal static class EntryAggregator
{
    public static Dictionary<string, TagEntry> AggregateTags(
        IEnumerable<MarkdownEntry> markdownEntries,
        MetadataContext context) =>
        markdownEntries.SelectMany(markdownEntry =>
             markdownEntry.GetProperty("tags", context) is { } tagsValue ?
                Expression.EnumerateValue(tagsValue, context).
                OfType<PartialTagEntry>().
                Select(tag => (tag, markdownEntry)).
                Where(entry => !string.IsNullOrWhiteSpace(entry.tag.Name)) :
                Utilities.Empty<(PartialTagEntry, MarkdownEntry)>()).
            GroupBy(entry => entry.tag.Name).
            ToDictionary(
                g => g.Key,
                g => new TagEntry(
                    g.Key,
                    g.Select(entry => entry.markdownEntry).
                    OrderBy(markdownEntry => markdownEntry, MarkdownHeaderDateComparer.Instance).
                    ToArray()));

    private static CategoryEntry AggregateCategory(
        string categoryName,
        IEnumerable<MarkdownEntry> markdownEntries,
        int levelIndex,
        MetadataContext context)
    {
        var categoryLists = markdownEntries.
            Select(markdownEntry =>
                (markdownEntry,
                 categoryList:
                    markdownEntry.GetProperty("category", context) is PartialCategoryEntry entry ?
                    entry.Unfold(e => e.Parent).Reverse().Skip(1).ToArray() :
                    Utilities.Empty<PartialCategoryEntry>())).
            ToArray();

        var childCategoryEntries = categoryLists.
            Where(entry => entry.categoryList.Length > levelIndex).
            GroupBy(entry => entry.categoryList[levelIndex].Name).
            ToDictionary(
                g => g.Key,
                g => AggregateCategory(
                    g.Key,
                    g.Select(entry => entry.markdownEntry),
                    levelIndex + 1,
                    context));

        var childEntries = categoryLists.
            Where(entry => entry.categoryList.Length <= levelIndex).
            Select(entry => entry.markdownEntry).
            ToArray();

        return new(categoryName, childCategoryEntries, childEntries);
    }

    public static CategoryEntry AggregateCategories(
        IEnumerable<MarkdownEntry> markdownEntries,
        MetadataContext context) =>
        AggregateCategory("(root)", markdownEntries, 0, context);
}
