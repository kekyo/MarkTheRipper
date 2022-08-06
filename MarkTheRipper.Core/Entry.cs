/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;

namespace MarkTheRipper;

public sealed class TagEntry :
    IEnumerable<MarkdownHeader>
{
    public readonly string TagName;
    public readonly MarkdownHeader[] Headers;

    public TagEntry(
        string tagName, MarkdownHeader[] headers)
    {
        this.TagName = tagName;
        this.Headers = headers;
    }

    public IEnumerator<MarkdownHeader> GetEnumerator() =>
        ((IEnumerable<MarkdownHeader>)this.Headers).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        this.GetEnumerator();
}

public sealed class CategoryEntry
{
    public readonly string CategoryName;
    public readonly IReadOnlyDictionary<string, CategoryEntry> Children;
    public readonly MarkdownHeader[] Headers;

    public CategoryEntry(
        string categoryName,
        IReadOnlyDictionary<string, CategoryEntry> children,
        MarkdownHeader[] headers)
    {
        this.CategoryName = categoryName;
        this.Children = children;
        this.Headers = headers;
    }
}
