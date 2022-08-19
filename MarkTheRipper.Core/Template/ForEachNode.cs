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

namespace MarkTheRipper.Template;

internal sealed class ForEachNode : ITemplateNode
{
    private readonly string keyName;
    private readonly string boundName;
    private readonly ITemplateNode[] childNodes;

    public ForEachNode(
        string keyName,
        string boundName,
        ITemplateNode[] childNodes)
    {
        this.keyName = keyName;
        this.boundName = boundName;
        this.childNodes = childNodes;
    }

    public async ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        MetadataContext metadata,
        CancellationToken ct)
    {
        if (Reducer.Reduce(keyName, metadata) is { } rawValue &&
            Reducer.EnumerateValue(rawValue, metadata) is { } enumerable)
        {
            var iterationMetadata = metadata.Spawn();

            var index = 0;
            foreach (var iterationValue in enumerable)
            {
                iterationMetadata.Set(
                    this.boundName,
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
        $"ForEach: {{{keyName}}}";
}
