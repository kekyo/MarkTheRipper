﻿/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using MarkTheRipper.Metadata;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Layout;

public sealed class RootLayoutNode :
    ILayoutNode, IMetadataEntry
{
    public readonly string Name;

    private readonly ILayoutNode[] nodes;

    public RootLayoutNode(
        string name, ILayoutNode[] nodes)
    {
        this.Name = name;
        this.nodes = nodes;
    }

    public ValueTask<object?> GetImplicitValueAsync(
        MetadataContext metadata, CancellationToken ct) =>
        new(this.Name);

    public ValueTask<object?> GetPropertyValueAsync(
        string keyName, MetadataContext context, CancellationToken ct) =>
        keyName switch
        {
            "name" => new(this.Name),
            _ => Utilities.NullAsync,
        };

    public async ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        MetadataContext metadata,
        CancellationToken ct)
    {
        foreach (var node in nodes)
        {
            await node.RenderAsync(writer, metadata, ct).
                ConfigureAwait(false);
        }
    }

    public override string ToString() =>
        $"RootLayout: {this.Name}";
}