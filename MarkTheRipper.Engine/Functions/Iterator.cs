/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Internal;
using MarkTheRipper.Metadata;
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions;

internal static class Iterator
{
    public static async ValueTask<IExpression> TakeAsync(
        IExpression[] parameters,
        IMetadataContext metadata,
        IReducer reducer,
        CancellationToken ct)
    {
        if (parameters.Length != 2)
        {
            throw new ArgumentException(
                $"Invalid take function arguments: Count={parameters.Length}");
        }

        if (await reducer.ReduceIntegerExpressionAsync(
            parameters[1], metadata, ct) is { } count &&
            await reducer.ReduceExpressionAsync(
            parameters[0], metadata, ct) is IEnumerable enumerable)
        {
            return new ValueExpression(enumerable.Cast<object?>().Take((int)count));
        }
        else
        {
            return new ValueExpression(InternalUtilities.Empty<object?>());
        }
    }
}
