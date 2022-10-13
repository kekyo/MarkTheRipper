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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.TextTreeNodes;

internal sealed class ForEachNode : ITextTreeNode
{
    private readonly IExpression[] parameters;
    private readonly ITextTreeNode[] childNodes;

    public ForEachNode(
        IExpression[] parameters,
        ITextTreeNode[] childNodes)
    {
        this.parameters = parameters;
        this.childNodes = childNodes;
    }

    public async ValueTask RenderAsync(
        Action<string> writer,
        IMetadataContext metadata,
        CancellationToken ct)
    {
        if (this.parameters.FirstOrDefault() is { } expression0 &&
            await Reducer.Instance.ReduceExpressionAsync(expression0, metadata, ct).
                ConfigureAwait(false) is { } rawValue &&
            MetadataUtilities.EnumerateValue(rawValue, metadata) is { } enumerable)
        {
            var iterationMetadata = metadata.Spawn();

            var boundName =
                this.parameters.ElementAtOrDefault(1)?.PrettyPrint ??
                "item";

            var count = enumerable switch
            {
                Array arr => arr.Length,
                IReadOnlyCollection<object?> corr => corr.Count,
                ICollection<object?> corr => corr.Count,
                ICollection corr => corr.Count,
                _ => enumerable.Count(),
            };

            var index = 0;
            foreach (var iterationValue in enumerable)
            {
                iterationMetadata.SetValue(
                    boundName,
                    new IteratorEntry(index, count, iterationValue));

                foreach (var childNode in childNodes)
                {
                    await childNode.RenderAsync(writer, iterationMetadata, ct).
                        ConfigureAwait(false);
                }

                index++;
            }
        }
    }

    public override string ToString() =>
        $"ForEach: {{{string.Join(" ", (object[])this.parameters)}}}";
}
