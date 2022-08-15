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

namespace MarkTheRipper.Metadata;

public sealed class TagEntry :
    IMetadataEntry, IEnumerableEntry
{
    public readonly string Name;
    public readonly MarkdownEntry[] Entries;

    public TagEntry(string name) :
        this(name, Utilities.Empty<MarkdownEntry>())
    {
    }

    public TagEntry(
        string name, MarkdownEntry[] markdownEntries)
    {
        this.Name = name;
        this.Entries = markdownEntries;
    }

    object? IMetadataEntry.ImplicitValue =>
        this.Name;

    public object? GetProperty(string keyName, MetadataContext context) =>
        keyName switch
        {
            "name" => this.Name,
            _ => null,
        };

    public IEnumerable<object> GetChildren(MetadataContext context) =>
        this.Entries;

    public override string ToString() =>
        $"Tag: {this.Name}";
}
