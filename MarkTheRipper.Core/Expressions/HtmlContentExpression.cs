/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Metadata;
using System;

namespace MarkTheRipper.Expressions;

public sealed class HtmlContentExpression : IExpression
{
    public readonly string Content;

    public HtmlContentExpression(string content) =>
        this.Content = content;

    string IExpression.PrettyPrint =>
        this.Content;
    object? IExpression.ImplicitValue =>
        new HtmlContentEntry(this.Content);

    public bool Equals(HtmlContentExpression rhs) =>
        this.Content.Equals(rhs.Content);

    bool IEquatable<IExpression>.Equals(IExpression? other) =>
        other is HtmlContentExpression rhs &&
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is HtmlContentExpression rhs &&
        this.Equals(rhs);

    public override int GetHashCode() =>
        this.Content.GetHashCode();

    public void Deconstruct(out HtmlContentEntry content) =>
        content = new HtmlContentEntry(this.Content);

    public override string ToString() =>
        $"\"{this.Content}\"";
}
