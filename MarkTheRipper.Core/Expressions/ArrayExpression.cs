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

public sealed class ArrayExpression : IExpression
{
    public readonly IExpression[] Elements;

    public ArrayExpression(IExpression[] elements) =>
        this.Elements = elements;

    string IExpression.PrettyPrint =>
        string.Join(",", this.Elements.Select(e => e.PrettyPrint));
    object? IExpression.ImplicitValue =>
        this.Elements.Select(e => e.ImplicitValue).ToArray();

    public bool Equals(ArrayExpression rhs) =>
        this.Elements.SequenceEqual(rhs.Elements);

    bool IEquatable<IExpression>.Equals(IExpression? other) =>
        other is ArrayExpression rhs &&
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is ArrayExpression rhs &&
        this.Equals(rhs);

    public override int GetHashCode() =>
        this.Elements.Aggregate(0, (agg, v) => agg ^ v.GetHashCode());

    public void Deconstruct(out IExpression[] elements) =>
        elements = this.Elements;

    public override string ToString() =>
        $"Array: [{string.Join(",", (object[])this.Elements)}]";
}
