﻿/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Metadata;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Expressions;

public delegate ValueTask<IExpression> AsyncFunctionDelegate(
    IExpression[] parameters,
    MetadataContext context,
    CancellationToken ct);

public interface IExpression : IEquatable<IExpression>
{
    string PrettyPrint { get; }
    object? ImplicitValue { get; }
}
