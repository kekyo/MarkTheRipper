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
using System.Collections.Generic;
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

    internal PartialCategoryEntry[] Path =>
        this.Unfold(pc => pc.Parent).Reverse().Skip(1).ToArray();

    object? IMetadataEntry.ImplicitValue =>
        string.Join("/", this.Path.Select(pc => pc.Name));

    private CategoryEntry? GetRealCategoryEntry(MetadataContext context) =>
        this.Path.
        Aggregate(
            context.Lookup("rootCategory") as CategoryEntry,
            (agg, pc) => (agg != null && agg.Children.TryGetValue(pc.Name, out var child)) ? child : null!);

    public object? GetProperty(string keyName, MetadataContext context) =>
        this.GetRealCategoryEntry(context)?.GetProperty(keyName, context) ??
        keyName switch
        {
            "name" => this.Name,
            "parent" => this.Parent,
            "path" => this.Path,
            _ => null,
        };

    public override string ToString() =>
        $"PartialCategory: {string.Join("/", this.Path.Select(pc => pc.Name))}";
}
