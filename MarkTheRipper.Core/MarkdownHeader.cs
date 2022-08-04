/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;

namespace MarkTheRipper;

public sealed class MarkdownHeader : IEquatable<MarkdownHeader>
{
    public readonly string RelativeContentPath;
    public readonly IReadOnlyDictionary<string, object?> Metadata;

    public MarkdownHeader(
        string relativeContentPath,
        IReadOnlyDictionary<string, object?> metadata)
    {
        this.RelativeContentPath = relativeContentPath;
        this.Metadata = metadata;
    }

    public bool Equals(MarkdownHeader? other) =>
        other is { } rhs &&
        this.RelativeContentPath.Equals(rhs.RelativeContentPath) &&
        this.Metadata.
            OrderBy(m => m.Key).
            SequenceEqual(rhs.Metadata.OrderBy(m => m.Key));

    public override bool Equals(object? obj) =>
        obj is MarkdownHeader rhs && this.Equals(rhs);

    public override int GetHashCode() =>
        this.Metadata.Aggregate(
            this.RelativeContentPath.GetHashCode(),
            (agg, v) => agg ^ v.Key.GetHashCode() ^ v.Value?.GetHashCode() ?? 0);
}
