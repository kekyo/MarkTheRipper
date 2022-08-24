/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Internal;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    public ValueTask<object?> GetImplicitValueAsync(CancellationToken ct) =>
        new(string.Join("/", this.Breadcrumbs.Select(pc => pc.Name)));

    private async ValueTask<CategoryEntry?> GetRealCategoryEntryAsync(
        MetadataContext metadata, CancellationToken ct) =>
        this.Breadcrumbs.Aggregate(
            metadata.Lookup("rootCategory") is { } rootCategoryExpression &&
            await rootCategoryExpression.ReduceExpressionAsync(metadata, ct) is CategoryEntry entry ?
                entry : null,
            (agg, pc) => (agg != null && agg.Children.TryGetValue(pc.Name, out var child)) ? child : null!);

    public async ValueTask<object?> GetPropertyValueAsync(
        string keyName, MetadataContext metadata, CancellationToken ct) =>
        await this.GetRealCategoryEntryAsync(metadata, ct).
            ConfigureAwait(false) is { } entry &&
        await entry.GetPropertyValueAsync(keyName, metadata, ct).
            ConfigureAwait(false) is { } value ?
            value :
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
