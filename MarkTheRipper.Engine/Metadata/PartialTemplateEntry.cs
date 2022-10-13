/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.TextTreeNodes;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Metadata;

internal sealed class PartialLayoutEntry :
    IMetadataEntry
{
    public readonly string Name;

    public PartialLayoutEntry(string name) =>
        this.Name = name;

    public ValueTask<object?> GetImplicitValueAsync(
        IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        new(this.Name);

    private async ValueTask<RootTextNode?> GetRealLayoutNodeAsync(
        IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        metadata.Lookup("layoutList") is { } layoutListExpression &&
        await reducer.ReduceExpressionAsync(layoutListExpression, metadata, ct).
            ConfigureAwait(false) is IReadOnlyDictionary<string, RootTextNode> layoutList &&
        layoutList.TryGetValue(this.Name, out var layout) ?
            layout : null;

    public async ValueTask<object?> GetPropertyValueAsync(
        string keyName, IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        await this.GetRealLayoutNodeAsync(metadata, reducer, ct).
            ConfigureAwait(false) is { } layout &&
        await layout.GetPropertyValueAsync(keyName, metadata, reducer, ct).
            ConfigureAwait(false) is { } value ?
            value :
            keyName switch
            {
                "name" => this.Name,
                _ => null,
            };

    public override string ToString() =>
        $"PartialLayout={this.Name}";
}
