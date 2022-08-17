/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

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

    object? IMetadataEntry.ImplicitValue =>
        this.Value;

    public object? GetProperty(string keyName, MetadataContext context) =>
        (this.Value as IMetadataEntry)?.GetProperty(keyName, context) ??
        keyName switch
        {
            "index" => this.Index,
            _ => null,
        };

    public override string ToString() =>
        $"Iterator={this.Index}";
}
