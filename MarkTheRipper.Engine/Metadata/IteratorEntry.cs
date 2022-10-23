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
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Metadata;

internal sealed class IteratorEntry :
    IMetadataEntry
{
    public readonly int Index;
    public readonly int Count;
    public readonly object? Value;

    public IteratorEntry(int index, int count, object? value)
    {
        this.Index = index;
        this.Count = count;
        this.Value = value;
    }

    public ValueTask<object?> GetImplicitValueAsync(
        IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        new(this.Value);

    public async ValueTask<object?> GetPropertyValueAsync(
        string keyName, IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        this.Value is IMetadataEntry entry &&
        await entry.GetPropertyValueAsync(keyName, metadata, reducer, ct) is { } value ?
            value :
            keyName switch
            {
                "index" => this.Index,
                "count" => this.Count,
                _ => null,
            };

    public override string ToString() =>
        $"Iterator={this.Index}";
}
