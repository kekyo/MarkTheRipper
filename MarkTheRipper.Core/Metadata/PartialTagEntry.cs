/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Metadata;

internal sealed class PartialTagEntry :
    IMetadataEntry
{
    public readonly string Name;

    public PartialTagEntry(string name) =>
        this.Name = name;

    public ValueTask<object?> GetImplicitValueAsync(CancellationToken ct) =>
        new(this.Name);

    private async ValueTask<TagEntry?> GetRealTagEntryAsync(
        MetadataContext metadata, CancellationToken ct) =>
        metadata.Lookup("tagList") is { } tagListExpression &&
        await Reducer.ReduceExpressionAsync(tagListExpression, metadata, ct).
            ConfigureAwait(false) is IReadOnlyDictionary<string, TagEntry> tagList &&
        tagList.TryGetValue(this.Name, out var tag) ?
            tag : null;

    public async ValueTask<object?> GetPropertyValueAsync(
        string keyName, MetadataContext metadata, CancellationToken ct) =>
        await this.GetRealTagEntryAsync(metadata, ct).
            ConfigureAwait(false) is { } tag &&
        await tag.GetPropertyValueAsync(keyName, metadata, ct).
            ConfigureAwait(false) is { } value ?
            value : keyName switch
            {
                "name" => this.Name,
                _ => null,
            };

    public override string ToString() =>
        $"PartialTag={this.Name}";
}
