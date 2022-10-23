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
using System.Collections.Generic;
using System.Linq;

namespace MarkTheRipper.Metadata;

public sealed class MetadataContext :
    IMetadataContext
{
    private readonly MetadataContext? parent;
    private readonly Dictionary<string, IExpression> metadata = new();

    public MetadataContext()
    {
    }

    private MetadataContext(MetadataContext parent) =>
        this.parent = parent;

    public void Set(string keyName, IExpression expression) =>
        this.metadata[keyName] = expression;

    public IExpression? Lookup(string keyName) =>
        this.metadata.TryGetValue(keyName, out var value) ?
            value :
            this.parent?.Lookup(keyName);

    public IMetadataContext Spawn() =>
        new MetadataContext(this);

    public override string ToString() =>
        string.Join(",",
            this.metadata.Concat(this.parent.
                Unfold(mc => mc.parent).
                SelectMany(mc => mc.metadata)).
            Distinct(KeyComparer<string, IExpression>.Instance).
            Select(kv => $"{kv.Key}=[{kv.Value}]"));

    public static readonly MetadataContext Empty =
        new MetadataContext();
}
