/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Metadata;

internal sealed class IteratorEntry :
    IMetadataEntry
{
    public readonly int Index;
    public readonly object? Value;

    public IteratorEntry(int index, object? value)
    {
        this.Index = index;
        this.Value = value;
    }

    public ValueTask<object?> GetImplicitValueAsync(
        MetadataContext metadata, CancellationToken ct) =>
        new(this.Value);

    public async ValueTask<object?> GetPropertyValueAsync(
        string keyName, MetadataContext context, CancellationToken ct) =>
        this.Value is IMetadataEntry entry &&
        await entry.GetPropertyValueAsync(keyName, context, ct).
            ConfigureAwait(false) is { } value ?
            value :
            keyName switch
            {
                "index" => new(this.Index),
                _ => Utilities.NullAsync,
            };

    public override string ToString() =>
        $"Iterator={this.Index}";
}
