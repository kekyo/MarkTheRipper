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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.TextTreeNodes;

internal sealed class ExpressionNode : ITextTreeNode
{
    private readonly IExpression expression;

    public ExpressionNode(IExpression expression) =>
        this.expression = expression;

    public async ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        MetadataContext metadata,
        CancellationToken ct)
    {
        var reduced = await expression.ReduceExpressionAsync(metadata, ct).
            ConfigureAwait(false);
        if (reduced is HtmlContentExpression(var content))
        {
            if (metadata.Lookup("htmlContents") is ValueExpression(Dictionary<string, string> htmlContents))
            {
                var idString = $"__{Guid.NewGuid().ToString("N")}__";

                htmlContents.Add(idString, content);

                await writer(idString, ct).
                    ConfigureAwait(false);
            }
        }
        else
        {
            var reducedString = await MetadataUtilities.FormatValueAsync(
                reduced, metadata, ct).
                ConfigureAwait(false);
            await writer(reducedString, ct).
                ConfigureAwait(false);
        }
    }

    public override string ToString() =>
        $"Expression={{{this.expression}}}";
}
