/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions;

public static class FunctionFactory
{
    public static SimpleFunctionDelegate
        CastTo(SimpleFunctionDelegate func) =>
        func;

    public static FunctionDelegate
        CastTo(FunctionDelegate func) =>
        func;

    public static Func<object?[], Func<string, Task<object?>>, IFormatProvider, CancellationToken, Task<object?>>
        CastTo(
            Func<object?[], Func<string, Task<object?>>, IFormatProvider, CancellationToken, Task<object?>> func) =>
            func;
}
