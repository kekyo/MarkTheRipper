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
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions;

public delegate ValueTask<IExpression> FunctionDelegate(
    IExpression[] parameters,
    IMetadataContext metadata,
    IReducer reducer,
    CancellationToken ct);
