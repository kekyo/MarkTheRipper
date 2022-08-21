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
    private readonly IExpression expression;

    public ExpressionNode(IExpression expression) =>
        this.expression = expression;

    public async ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        MetadataContext metadata,
        CancellationToken ct)
    {
        if (await Reducer.ReduceExpressionAsync(expression, metadata, ct).
                ConfigureAwait(false) is { } rawValue &&
            await Reducer.FormatValueAsync(rawValue, metadata, ct).
                ConfigureAwait(false) is { } value)
        {
            await writer(value, ct).
                ConfigureAwait(false);
        }
        else
        {
            await writer(expression.PrettyPrint, ct).
                ConfigureAwait(false);
        }
    }

    public override string ToString() =>
        $"Replacer={{{this.expression}}}";
}
