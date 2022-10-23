/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Globalization;

namespace MarkTheRipper.Expressions;

public sealed class ValueExpression : IExpression
{
    public readonly object? Value;

    public ValueExpression(object? value)
    {
        Debug.Assert(value is not IExpression);
        this.Value = value;
    }

    string IExpression.PrettyPrint =>
        this.Value switch
        {
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            null => string.Empty,
            _ => this.Value.ToString() ?? string.Empty,
        };
    object? IExpression.ImplicitValue =>
        this.Value;

    public string Type =>
        this.Value?.GetType().Name ?? "(null)";

    public bool Equals(ValueExpression rhs)
    {
        // Dirty statements instead pattern matching,
        // because avoid reference ValueTuple assembly.
        if (this.Value == null)
        {
            if (rhs.Value == null)
            {
                return true;
            }
            return false;
        }
        if (rhs.Value == null)
        {
            return false;
        }
        return this.Value.Equals(rhs.Value);
    }

    bool IEquatable<IExpression>.Equals(IExpression? other) =>
        other is ValueExpression rhs &&
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is ValueExpression rhs &&
        this.Equals(rhs);

    public override int GetHashCode() =>
        this.Value?.GetHashCode() ?? 0;

    public void Deconstruct(out object? value) =>
        value = this.Value;

    public override string ToString() =>
        this.Value switch
        {
            string str => $"Value: \"{str}\"",
            char ch => $"Value: '{ch}'",
            IFormattable f => $"Value: {f.ToString(null, CultureInfo.InvariantCulture)}",
            null => "Value: (null)",
            _ => $"Value: {this.Value}",
        };
}
