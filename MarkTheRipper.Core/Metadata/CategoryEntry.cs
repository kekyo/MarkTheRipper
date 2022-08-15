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

public sealed class CategoryEntry :
    IMetadataEntry
{
    public readonly string Name;
    public readonly IReadOnlyDictionary<string, CategoryEntry> Children;
    public readonly MarkdownEntry[] Entries;

    private CategoryEntry? parent;

    public CategoryEntry(
        string name,
        IReadOnlyDictionary<string, CategoryEntry> children,
        MarkdownEntry[] markdownEntries)
    {
        this.Name = name;
        this.Children = children;
        this.Entries = markdownEntries;

        foreach (var child in children.Values)
        {
            child.parent = this;
        }
    }

    internal CategoryEntry? Parent =>
        this.parent;

    internal CategoryEntry[] Path =>
        this.Unfold(c => c.parent).Reverse().Skip(1).ToArray();

    object? IMetadataEntry.ImplicitValue =>
        string.Join("/", this.Path.Select(c => c.Name));

    public object? GetProperty(string keyName, MetadataContext context) =>
        keyName switch
        {
            "name" => this.Name,
            "children" => this.Children.Values,
            "entries" => this.Entries,
            "parent" => this.parent,
            "path" => this.Path,
            _ => null,
        };

    public override string ToString() =>
        $"Category: {string.Join("/", this.Path.Select(c => c.Name))}";
}
