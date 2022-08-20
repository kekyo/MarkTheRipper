/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Globalization;
using System.Linq;

namespace MarkTheRipper.Template;

public interface IExpression
{
    string ImplicitValue { get; }
}

public sealed class VariableExpression : IExpression
{
    public readonly string Name;

    public VariableExpression(string name) =>
        this.Name = name;

    string IExpression.ImplicitValue =>
        this.Name;

    public void Deconstruct(out string name) =>
        name = this.Name;

    public override string ToString() =>
        this.Name;
}

public sealed class ValueExpression : IExpression
{
    public readonly object? Value;

    public ValueExpression(object? value) =>
        this.Value = value;

    string IExpression.ImplicitValue =>
        this.Value switch
        {
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            null => string.Empty,
            _ => this.Value.ToString() ?? string.Empty,
        };

    public string Type =>
        this.Value?.GetType().Name ?? "(null)";

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

    string IExpression.ImplicitValue =>
        string.Join(",", this.Elements.Select(e => e.ImplicitValue));

    public void Deconstruct(out IExpression[] elements) =>
        elements = this.Elements;

    public override string ToString() =>
        $"[{string.Join(",", this.Elements.Select(e => e.ImplicitValue))}]";
}

public sealed class ListExpression : IExpression
{
    public readonly IExpression[] Values;

    public ListExpression(IExpression[] values) =>
        this.Values = values;

    string IExpression.ImplicitValue =>
        string.Join(" ", this.Values.Select(e => e.ImplicitValue));

    public void Deconstruct(out IExpression[] values) =>
        values = this.Values;

    public override string ToString() =>
        $"({string.Join(" ", this.Values.Select(e => e.ImplicitValue))})";
}
