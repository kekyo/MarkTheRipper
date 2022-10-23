/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;

namespace MarkTheRipper.Expressions;

public sealed class ApplyExpression : IExpression
{
    public readonly IExpression Function;
    public readonly IExpression[] Parameters;

    public ApplyExpression(IExpression function, IExpression[] parameters)
    {
        this.Function = function;
        this.Parameters = parameters;
    }

    string IExpression.PrettyPrint =>
        $"{this.Function.PrettyPrint} {string.Join(" ", this.Parameters.Select(e => e.PrettyPrint))}";
    object? IExpression.ImplicitValue =>
        $"{this.Function.PrettyPrint} {string.Join(" ", this.Parameters.Select(e => e.PrettyPrint))}";

    public bool Equals(ApplyExpression rhs) =>
        this.Function.Equals(rhs.Function) &&
        this.Parameters.SequenceEqual(rhs.Parameters);

    bool IEquatable<IExpression>.Equals(IExpression? other) =>
        other is ApplyExpression rhs &&
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is ApplyExpression rhs &&
        this.Equals(rhs);

    public override int GetHashCode() =>
        this.Parameters.Aggregate(this.Function.GetHashCode(), (agg, v) => agg ^ v.GetHashCode());

    public void Deconstruct(out IExpression function, out IExpression[] parameters)
    {
        function = this.Function;
        parameters = this.Parameters;
    }

    public override string ToString() =>
        $"Apply: {this.Function} {string.Join(" ", (object[])this.Parameters)}";
}
