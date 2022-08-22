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
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions;

internal static class Lookup
{
    private static async ValueTask<IExpression> LookupAsync(
        IExpression[] parameters,
        MetadataContext metadata,
        CancellationToken ct)
    {
        if (parameters.Length != 1)
        {
            throw new ArgumentException(
                $"Invalid lookup function arguments: Count={parameters.Length}");
        }

        var name = await Reducer.ReduceExpressionAndFormatAsync(parameters[0], metadata, ct).
            ConfigureAwait(false);
        var nameString = await Reducer.FormatValueAsync(name, metadata, ct).
            ConfigureAwait(false);

        return metadata.Lookup(nameString) is { } resolvedExpression ?
            resolvedExpression : new ValueExpression(name);
    }

    public static readonly AsyncFunctionDelegate Function =
        LookupAsync;
}
