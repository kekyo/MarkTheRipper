﻿/////////////////////////////////////////////////////////////////////////////////////
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

///////////////////////////////////////////////////////////////////////////////////

internal sealed class TextNode : ITemplateNode
{
    private readonly string text;

    public TextNode(string text) =>
        this.text = text;

    public ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        MetadataContext metadata,
        CancellationToken ct) =>
        writer(text, ct);

    public override string ToString() =>
        $"Text: \"{text}\"";
}
