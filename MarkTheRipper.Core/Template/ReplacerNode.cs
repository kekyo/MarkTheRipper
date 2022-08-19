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

internal sealed class ReplacerNode : ITemplateNode
{
    private readonly string keyName;
    private readonly string? parameter;

    public ReplacerNode(string keyName, string? parameter)
    {
        this.keyName = keyName;
        this.parameter = parameter;
    }

    public async ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        MetadataContext metadata,
        CancellationToken ct)
    {
        async ValueTask RenderAsync(string keyName)
        {
            if (Expression.Reduce(keyName, metadata) is { } rawValue &&
                await Expression.FormatValueAsync(
                    rawValue, this.parameter, metadata, ct).ConfigureAwait(false) is { } value)
            {
                await writer(value, ct).
                    ConfigureAwait(false);
            }
            // Not found from all metadata.
            else
            {
                await writer(keyName, ct).
                    ConfigureAwait(false);
            }
        }

        if (this.keyName[0] == '*')
        {
            var keyName = this.keyName.Substring(1);
            if (Expression.Reduce(keyName, metadata) is { } rawNestedKeyName &&
                await Expression.FormatValueAsync(
                    rawNestedKeyName, null, metadata, ct).ConfigureAwait(false) is { } nestedKeyName)
            {
                await RenderAsync(nestedKeyName);
            }
            // Not found from all metadata.
            else
            {
                await writer($"<!-- Reference key: {keyName} -->", ct).
                    ConfigureAwait(false);
            }
        }
        else
        {
            await RenderAsync(this.keyName);
        }
    }

    public override string ToString() =>
        this.parameter is { } ?
            $"Replacer: {{{this.keyName}:{this.parameter}}}" :
            $"Replacer: {{{this.keyName}}}";
}
