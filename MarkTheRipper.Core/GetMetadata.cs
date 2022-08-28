/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

namespace MarkTheRipper;

public struct MetadataResult
{
    private static MetadataResult GetNull(string keyName) =>
        new(null, GetNull);

    public readonly object? Value;
    public readonly GetMetadataDelegate GetMetadata;

    public MetadataResult(
        object? value, GetMetadataDelegate getMetadata)
    {
        this.Value = value;
        this.GetMetadata = getMetadata;
    }

    public MetadataResult(object? value)
    {
        this.Value = value;
        this.GetMetadata = GetNull;
    }

    public void Deconstruct(
        out object? value, out GetMetadataDelegate getMetadata)
    {
        value = this.Value;
        getMetadata = this.GetMetadata;
    }
}

public delegate MetadataResult GetMetadataDelegate(string keyName);
