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
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions;

internal static class Lookup
{
    public static async ValueTask<IExpression> LookupAsync(
        IExpression[] parameters,
        IMetadataContext metadata,
        IReducer reducer,
        CancellationToken ct)
    {
        if (parameters.Length != 1)
        {
            throw new ArgumentException(
                $"Invalid lookup function arguments: Count={parameters.Length}");
        }

        var name = await reducer.ReduceExpressionAsync(parameters[0], metadata, ct).
            ConfigureAwait(false);
        var nameString = await MetadataUtilities.FormatValueAsync(name, metadata, ct).
            ConfigureAwait(false);

        return metadata.Lookup(nameString) is { } resolvedExpression ?
            resolvedExpression : new ValueExpression(name);
    }
}
