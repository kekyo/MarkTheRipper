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

namespace MarkTheRipper.Functions;

internal static class Format
{
    public static async ValueTask<IExpression> FormatAsync(
        IExpression[] parameters, MetadataContext metadata, CancellationToken ct)
    {
        if (parameters.Length == 0 || parameters.Length >= 3)
        {
            throw new ArgumentException(
                $"Invalid format function arguments: Count={parameters.Length}");
        }

        var value = await parameters[0].ReduceExpressionAsync(metadata, ct).
            ConfigureAwait(false);

        var formatExpression = parameters.ElementAtOrDefault(1);
        var format = formatExpression != null ?
            await formatExpression.ReduceExpressionAndFormatAsync(metadata, ct).
                ConfigureAwait(false) :
            null;

        var fp = await MetadataUtilities.GetFormatProviderAsync(metadata, ct).
            ConfigureAwait(false);

        return value switch
        {
            IFormattable formattable => new ValueExpression(formattable.ToString(format, fp)),
            _ => parameters[0],
        };
    }
}
