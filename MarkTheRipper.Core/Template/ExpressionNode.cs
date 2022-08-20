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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Template;

internal sealed class ExpressionNode : ITemplateNode
{
    private readonly IExpression[] expressions;

    public ExpressionNode(IExpression[] expressions) =>
        this.expressions = expressions;

    public async ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        MetadataContext metadata,
        CancellationToken ct)
    {
        if (this.expressions.FirstOrDefault() is { } expression0)
        {
            if (Reducer.ReduceExpression(expression0, metadata) is { } rawValue &&
                await Reducer.FormatValueAsync(
                    rawValue, this.expressions.Skip(1).ToArray(), metadata, ct).
                    ConfigureAwait(false) is { } value)
            {
                await writer(value, ct).
                    ConfigureAwait(false);
            }
            else
            {
                await writer(expression0.ToString()!, ct).
                    ConfigureAwait(false);
            }
        }
    }

    public override string ToString() =>
        $"Replacer={{{string.Join(" ", (object[])this.expressions)}}}";
}
