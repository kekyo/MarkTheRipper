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
using System.Linq;

namespace MarkTheRipper.Metadata;

internal sealed class PartialCategoryEntry :
    IMetadataEntry
{
    public readonly string Name;
    public readonly PartialCategoryEntry? Parent;

    public PartialCategoryEntry() :
        this("(root)", null)
    {
    }

    public PartialCategoryEntry(
        string name, PartialCategoryEntry? parent)
    {
        this.Name = name;
        this.Parent = parent;
    }

    internal PartialCategoryEntry[] Breadcrumbs
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

    object? IMetadataEntry.ImplicitValue =>
        string.Join("/", this.Breadcrumbs.Select(pc => pc.Name));

    private CategoryEntry? GetRealCategoryEntry(MetadataContext context) =>
        this.Breadcrumbs.
        Aggregate(
            context.Lookup("rootCategory") as CategoryEntry,
            (agg, pc) => (agg != null && agg.Children.TryGetValue(pc.Name, out var child)) ? child : null!);

    public object? GetProperty(string keyName, MetadataContext context) =>
        this.GetRealCategoryEntry(context)?.GetProperty(keyName, context) ??
        keyName switch
        {
            "name" => this.Name,
            "parent" => this.Parent,
            "breadcrumbs" => this.Breadcrumbs,
            _ => null,
        };

    public override string ToString() =>
        $"PartialCategory={string.Join("/", this.Breadcrumbs.Select(pc => pc.Name))}";
}
