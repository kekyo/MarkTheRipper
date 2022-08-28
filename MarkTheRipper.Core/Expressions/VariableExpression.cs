/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;

namespace MarkTheRipper.Expressions;

public sealed class VariableExpression : IExpression
{
    public readonly string Name;

    public VariableExpression(string name) =>
        this.Name = name;

    string IExpression.PrettyPrint =>
        this.Name;
    object? IExpression.ImplicitValue =>
        this.Name;

    public bool Equals(VariableExpression rhs) =>
        this.Name == rhs.Name;

    bool IEquatable<IExpression>.Equals(IExpression? other) =>
        other is VariableExpression rhs &&
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is VariableExpression rhs &&
        this.Equals(rhs);

    public override int GetHashCode() =>
        this.Name.GetHashCode();

    public void Deconstruct(out string name) =>
        name = this.Name;

    public override string ToString() =>
        this.Name;
}
