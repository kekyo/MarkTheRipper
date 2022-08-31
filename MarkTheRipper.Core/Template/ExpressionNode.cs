/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Metadata;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Layout;

internal sealed class ExpressionNode : ILayoutNode
{
    private readonly IExpression expression;

    public ExpressionNode(IExpression expression) =>
        this.expression = expression;

    public async ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        MetadataContext metadata,
        CancellationToken ct)
    {
        if (await Reducer.ReduceExpressionAndFormatAsync(this.expression, metadata, ct).
                ConfigureAwait(false) is { } value)
        {
            await writer(value, ct).
                ConfigureAwait(false);
        }
        else
        {
            await writer(this.expression.PrettyPrint, ct).
                ConfigureAwait(false);
        }
    }

    public override string ToString() =>
        $"Replacer={{{this.expression}}}";
}
