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

internal sealed class ForEachNode : ITemplateNode
{
    private readonly IExpression[] parameters;
    private readonly ITemplateNode[] childNodes;

    public ForEachNode(
        IExpression[] parameters,
        ITemplateNode[] childNodes)
    {
        this.parameters = parameters;
        this.childNodes = childNodes;
    }

    public async ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        MetadataContext metadata,
        CancellationToken ct)
    {
        if (this.parameters.FirstOrDefault() is { } expression0 &&
            await Reducer.ReduceExpressionAsync(expression0, metadata, ct).
                ConfigureAwait(false) is { } rawValue &&
            Reducer.EnumerateValue(rawValue, metadata) is { } enumerable)
        {
            var iterationMetadata = metadata.Spawn();

            var boundName =
                this.parameters.ElementAtOrDefault(1)?.PrettyPrint ??
                "item";

            var index = 0;
            foreach (var iterationValue in enumerable)
            {
                iterationMetadata.SetValue(
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
        $"ForEach: {{{string.Join(" ", (object[])this.parameters)}}}";
}
