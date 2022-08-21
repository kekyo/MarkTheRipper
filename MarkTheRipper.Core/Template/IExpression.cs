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
using System.Linq;

namespace MarkTheRipper.Template;

public interface IExpression : IEquatable<IExpression>
{
    string PrettyPrint { get; }
    object? ImplicitValue { get; }
}

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

    public bool Equals(ValueExpression rhs) =>
        (this.Value, rhs.Value) switch
        {
            (null, null) => true,
            (null, _) => false,
            (_, null) => false,
            (_, _) => this.Value.Equals(rhs.Value),
        };

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
            string str => $"\"{str}\"",
            char ch => $"'{ch}'",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            null => "(null)",
            _ => this.Value.ToString() ?? string.Empty,
        };
}

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
        $"[{string.Join(",", (object[])this.Elements)}]";
}

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
        (this.Function.ImplicitValue, this.Parameters.Select(e => e.ImplicitValue).ToArray());

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
        $"{this.Function} {string.Join(" ", (object[])this.Parameters)}";
}
