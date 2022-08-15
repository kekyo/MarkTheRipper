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
        if (keyName[0] == '*')
        {
            var keyName = this.keyName.Substring(1);
            if (Expression.Reduce(keyName, metadata) is { } rawNestedKeyName &&
                Expression.FormatValue(rawNestedKeyName, null, metadata) is { } nestedKeyName)
            {
                if (Expression.Reduce(nestedKeyName, metadata) is { } rawValue &&
                    Expression.FormatValue(rawValue, parameter, metadata) is { } value)
                {
                    await writer(value, ct).
                        ConfigureAwait(false);
                }
                // Not found from all metadata.
                else
                {
                    await writer(nestedKeyName, ct).
                        ConfigureAwait(false);
                }
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
            if (Expression.Reduce(keyName, metadata) is { } rawValue &&
                Expression.FormatValue(rawValue, parameter, metadata) is { } value)
            {
                await writer(value, ct).
                    ConfigureAwait(false);
            }
            // Not found from all metadata.
            else
            {
                await writer($"<!-- Key: {keyName} -->", ct).
                    ConfigureAwait(false);
            }
        }
    }

    public override string ToString() =>
        parameter is { } ?
            $"Replacer: {{{keyName}:{parameter}}}" :
            $"Replacer: {{{keyName}}}";
}
