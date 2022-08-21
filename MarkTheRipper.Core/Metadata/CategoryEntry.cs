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
using System.Threading;
using System.Threading.Tasks;

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

    internal CategoryEntry[] Breadcrumbs
    {
        get
        {
            var entries = this.Unfold(pc => pc.Parent).Reverse().ToList();
            if (entries.Count >= 2)
            {
                entries.RemoveAt(0);
            }
            return entries.ToArray();
        }
    }

    public ValueTask<object?> GetImplicitValueAsync(CancellationToken ct) =>
        new(string.Join("/", this.Breadcrumbs.Select(c => c.Name)));

    public ValueTask<object?> GetPropertyValueAsync(
        string keyName, MetadataContext metadata, CancellationToken ct) =>
        keyName switch
        {
            "name" => new(this.Name),
            "children" => new(this.Children.Values),
            "entries" => new(this.Entries),
            "parent" => new(this.parent),
            "breadcrumbs" => new(this.Breadcrumbs),
            _ => Utilities.NullAsync,
        };

    public override string ToString() =>
        $"Category={string.Join("/", this.Breadcrumbs.Select(c => c.Name))}";
}
