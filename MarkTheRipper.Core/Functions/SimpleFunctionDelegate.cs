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

public delegate ValueTask<object?> SimpleFunctionDelegate(
    object?[] parameters,
    Func<string, ValueTask<object?>> lookup,
    IFormatProvider formatProvider,
    CancellationToken ct);
