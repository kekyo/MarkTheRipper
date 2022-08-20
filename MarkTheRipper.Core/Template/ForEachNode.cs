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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Template;

internal sealed class ForEachNode : ITemplateNode
{
    private readonly IExpression[] expressions;
    private readonly ITemplateNode[] childNodes;

    public ForEachNode(
        IExpression[] expressions,
        ITemplateNode[] childNodes)
    {
        this.expressions = expressions;
        this.childNodes = childNodes;
    }

    public async ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        MetadataContext metadata,
        CancellationToken ct)
    {
        if (this.expressions.FirstOrDefault() is { } expression0 &&
            Reducer.ReduceExpression(expression0, metadata) is { } rawValue &&
            Reducer.EnumerateValue(rawValue, metadata) is { } enumerable)
        {
            var iterationMetadata = metadata.Spawn();

            var boundName =
                this.expressions.ElementAtOrDefault(1)?.ImplicitValue ??
                "item";

            var index = 0;
            foreach (var iterationValue in enumerable)
            {
                iterationMetadata.Set(
                    boundName,
                    new IteratorEntry(index, iterationValue));

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
        $"ForEach: {{{string.Join(" ", (object[])this.expressions)}}}";
}
