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
        IEnumerable<MarkdownHeader> parsedHeaders) =>
        parsedHeaders.SelectMany(header =>
             header.GetMetadata("tags") is { } tagsValue ?
                Utilities.EnumerateValue(tagsValue).
                Select(tagName =>
                    (tagName: Utilities.FormatValue(tagName, null, CultureInfo.InvariantCulture)!,
                     header)).
                Where(entry => !string.IsNullOrWhiteSpace(entry.tagName)) :
                Utilities.Empty<(string, MarkdownHeader)>()).
            GroupBy(entry => entry.tagName).
            ToDictionary(
                g => g.Key,
                g => new TagEntry(
                    g.Key,
                    g.Select(entry => entry.header).
                    OrderBy(header => header, MarkdownHeaderDateComparer.Instance).
                    ToArray()));

    private static CategoryEntry AggregateCategory(
        string categoryName, IEnumerable<MarkdownHeader> headers, int levelIndex)
    {
        var categoryLists = headers.
            Select(header =>
                (header,
                 categoryList: Utilities.EnumerateValue(header.GetMetadata("category")).
                    Select(categoryName => Utilities.FormatValue(categoryName, null, CultureInfo.InvariantCulture)!).
                    ToArray())).
            ToArray();

        var childEntries = categoryLists.
            Where(entry => entry.categoryList.Length > levelIndex).
            GroupBy(entry => entry.categoryList[levelIndex]).
            ToDictionary(
                g => g.Key!,
                g => AggregateCategory(
                    g.Key!,
                    g.Select(entry => entry.header),
                    levelIndex + 1));

        var childHeaders = categoryLists.
            Where(entry => entry.categoryList.Length <= levelIndex).
            Select(entry => entry.header).
            ToArray();

        return new(categoryName, childEntries, childHeaders);
    }

    public static CategoryEntry AggregateCategories(
        IEnumerable<MarkdownHeader> headers) =>
        AggregateCategory("", headers, 0);
}
