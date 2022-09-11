/////////////////////////////////////////////////////////////////////////////////////
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

namespace MarkTheRipper.TextTreeNodes;

public sealed class RootTextNode :
    ITextTreeNode, IMetadataEntry
{
    public readonly PathEntry Path;

    private readonly ITextTreeNode[] nodes;

    public RootTextNode(
        PathEntry path, ITextTreeNode[] nodes)
    {
        this.Path = path;
        this.nodes = nodes;
    }

    public ValueTask<object?> GetImplicitValueAsync(
        MetadataContext metadata, CancellationToken ct) =>
        new(this.Path);

    public ValueTask<object?> GetPropertyValueAsync(
        string keyName, MetadataContext context, CancellationToken ct) =>
        keyName switch
        {
            "name" => new(this.Path),
            _ => InternalUtilities.NullAsync,
        };

    public async ValueTask RenderAsync(
        Action<string> writer,
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
        $"RootText: {this.Path}";
}
