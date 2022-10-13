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

namespace MarkTheRipper.TextTreeNodes;

internal sealed class LiteralTextNode : ITextTreeNode
{
    private readonly string text;

    public LiteralTextNode(string text) =>
        this.text = text;

    public ValueTask RenderAsync(
        Action<string> writer,
        MetadataContext metadata,
        CancellationToken ct)
    {
        writer(text);
        return default;
    }

    public override string ToString() =>
        $"LiteralText: \"{text}\"";
}
