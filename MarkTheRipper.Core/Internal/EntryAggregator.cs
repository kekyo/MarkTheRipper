/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MarkTheRipper.Internal;

internal static class EntryAggregator
{
    public static Dictionary<string, TagEntry> AggregateTags(
        IEnumerable<MarkdownEntry> markdownEntries) =>
        markdownEntries.SelectMany(markdownEntry =>
             markdownEntry.GetProperty("tags") is { } tagsValue ?
                Utilities.EnumerateValue(tagsValue).
                Select(tagName =>
                    (tagName: Utilities.FormatValue(tagName, null, CultureInfo.InvariantCulture)!,
                     markdownEntry)).
                Where(entry => !string.IsNullOrWhiteSpace(entry.tagName)) :
                Utilities.Empty<(string, MarkdownEntry)>()).
            GroupBy(entry => entry.tagName).
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
        int levelIndex)
    {
        var categoryLists = markdownEntries.
            Select(markdownEntry =>
                (markdownEntry,
                 categoryList: Utilities.EnumerateValue(markdownEntry.GetProperty("category")).
                    Select(categoryName => Utilities.FormatValue(categoryName, null, CultureInfo.InvariantCulture)!).
                    ToArray())).
            ToArray();

        var childCategoryEntries = categoryLists.
            Where(entry => entry.categoryList.Length > levelIndex).
            GroupBy(entry => entry.categoryList[levelIndex]).
            ToDictionary(
                g => g.Key!,
                g => AggregateCategory(
                    g.Key!,
                    g.Select(entry => entry.markdownEntry),
                    levelIndex + 1));

        var childEntries = categoryLists.
            Where(entry => entry.categoryList.Length <= levelIndex).
            Select(entry => entry.markdownEntry).
            ToArray();

        return new(categoryName, childCategoryEntries, childEntries);
    }

    public static CategoryEntry AggregateCategories(
        IEnumerable<MarkdownEntry> markdownEntries) =>
        AggregateCategory("", markdownEntries, 0);
}
