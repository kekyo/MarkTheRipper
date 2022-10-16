/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Metadata;
using System.Threading.Tasks;
using System.Threading;

namespace MarkTheRipper.Expressions;

public interface IReducer
{
    ValueTask<object?> ReduceExpressionAsync(
        IExpression expression,
        IMetadataContext metadata,
        CancellationToken ct);

    ValueTask<string> ReduceExpressionAndFormatAsync(
        IExpression expression,
        IMetadataContext metadata,
        CancellationToken ct);
}
