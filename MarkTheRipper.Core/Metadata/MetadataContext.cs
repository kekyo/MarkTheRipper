/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

namespace MarkTheRipper.Metadata;

public sealed class MetadataContext
{
    private readonly MetadataContext? parent;
    private readonly Dictionary<string, object?> metadata = new();

    public MetadataContext()
    {
    }

    private MetadataContext(MetadataContext parent) =>
        this.parent = parent;

    public void Set(string keyName, object? value) =>
        this.metadata[keyName] = value;

    public object? Lookup(string keyName) =>
        this.metadata.TryGetValue(keyName, out var value) ?
            value :
            this.parent?.Lookup(keyName);

    public T? Lookup<T>(string keyName)
        where T : notnull =>
        this.Lookup(keyName) is T value ?
            value :
            default(T?);

    public MetadataContext Spawn() =>
        new MetadataContext(this);
}
