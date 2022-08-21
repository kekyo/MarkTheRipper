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
using MarkTheRipper.Template;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions;

internal static class Format
{
    private static async ValueTask<IExpression> FormatAsync(
        IExpression[] parameters,
        MetadataContext metadata,
        CancellationToken ct)
    {
        if (parameters.Length != 2)
        {
            throw new ArgumentException(
                $"Invalid format function arguments: Count={parameters.Length}");
        }

        var value = await Reducer.ReduceExpressionAsync(parameters[0], metadata, ct).
            ConfigureAwait(false);
        var formatValue = await Reducer.ReduceExpressionAsync(parameters[1], metadata, ct).
            ConfigureAwait(false);
        var format = await Reducer.FormatValueAsync(formatValue, metadata, ct).
            ConfigureAwait(false);
        var lang = metadata.Lookup("lang") is { } langExpression &&
            await Reducer.ReduceExpressionAsync(langExpression, metadata, ct) is { } langValue ?
                langValue is IFormatProvider fp ?
                    fp :
                    new CultureInfo(await Reducer.FormatValueAsync(langValue, metadata, ct).
                        ConfigureAwait(false)) :
                CultureInfo.InvariantCulture;

        return value switch
        {
            IFormattable formattable => new ValueExpression(formattable.ToString(format, lang)),
            _ => parameters[0],
        };
    }

    public static readonly AsyncFunctionDelegate Function =
        FormatAsync;
}
