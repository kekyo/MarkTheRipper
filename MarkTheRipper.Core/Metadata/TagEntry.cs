/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;

namespace MarkTheRipper.Metadata;

public sealed class TagEntry :
    IMetadataEntry
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
            "entries" => this.Entries,
            _ => null,
        };

    public override string ToString() =>
        $"Tag: {this.Name}";
}
