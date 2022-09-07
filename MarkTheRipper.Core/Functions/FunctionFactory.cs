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

public delegate ValueTask<IExpression> AsyncFunctionDelegate(
    IExpression[] parameters,
    MetadataContext metadata,
    CancellationToken ct);

public static class FunctionFactory
{
    public static AsyncFunctionDelegate
        CastTo(AsyncFunctionDelegate func) =>
        func;

    public static Func<object?[], Func<string, Task<object?>>, IFormatProvider, CancellationToken, Task<object?>>
        CastTo(
            Func<object?[], Func<string, Task<object?>>, IFormatProvider, CancellationToken, Task<object?>> func) =>
            func;
}
