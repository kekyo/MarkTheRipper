/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Metadata;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Template;

public sealed class RootTemplateNode : ITemplateNode
{
    public readonly string Name;
    public readonly string OriginalText;

    private readonly ITemplateNode[] nodes;

    public RootTemplateNode(string name, string originalText, ITemplateNode[] nodes)
    {
        Name = name;
        OriginalText = originalText;
        this.nodes = nodes;
    }

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
        $"RootTemplate: {this.Name}";
}
