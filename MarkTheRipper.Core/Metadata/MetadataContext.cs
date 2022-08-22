/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Template;
using System.Collections.Generic;

namespace MarkTheRipper.Metadata;

public sealed class MetadataContext
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

    public void SetValue(string keyName, object? value) =>
        this.metadata[keyName] = new ValueExpression(value);

    public IExpression? Lookup(string keyName) =>
        this.metadata.TryGetValue(keyName, out var value) ?
            value :
            this.parent?.Lookup(keyName);

    public MetadataContext Spawn() =>
        new MetadataContext(this);

    public static readonly MetadataContext Empty =
        new MetadataContext();
}
