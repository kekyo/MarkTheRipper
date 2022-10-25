/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Internal;
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
        Action<string> writer,
        IMetadataContext metadata,
        CancellationToken ct)
    {
        var reduced = await Reducer.Instance.ReduceExpressionAndFinalApplyAsync(
            expression, metadata, ct);
        if (reduced is HtmlContentEntry(var contentString))
        {
            if (metadata.Lookup("htmlContents") is ValueExpression(Dictionary<string, string> htmlContents))
            {
                var idString = $"@@{Guid.NewGuid().ToString("N")}@@";
                htmlContents.Add(idString, contentString);

                writer(idString);
                return;
            }
        }

        var reducedString = await MetadataUtilities.FormatValueAsync(
            reduced, metadata, ct);

        var escapedString = Utilities.EscapeHtmlString(reducedString);

        writer(escapedString);
    }

    public override string ToString() =>
        $"Expression={{{this.expression}}}";
}
