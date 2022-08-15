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

namespace MarkTheRipper.Metadata;

public sealed class MarkdownEntry :
    IMetadataEntry, IEquatable<MarkdownEntry>
{
    public readonly string RelativeContentPath;
    public readonly IReadOnlyDictionary<string, object?> Metadata;

    internal readonly string contentBasePath;

    public MarkdownEntry(
        string relativeContentPath,
        Dictionary<string, object?> metadata,
        string contentBasePath)
    {
        this.RelativeContentPath = relativeContentPath;
        this.Metadata = metadata;
        this.contentBasePath = contentBasePath;
    }

    object? IMetadataEntry.ImplicitValue =>
        this.Metadata.TryGetValue("title", out var value) ?
            value : null;

    public object? GetProperty(string keyName, MetadataContext context) =>
        keyName switch
        {
            "relativePath" => this.RelativeContentPath,
            _ => this.Metadata.TryGetValue(keyName, out var value) ?
                value : null,
        };

    public bool Equals(MarkdownEntry? other) =>
        other is { } rhs &&
        this.RelativeContentPath.Equals(rhs.RelativeContentPath) &&
        this.Metadata.
            OrderBy(m => m.Key).
            SequenceEqual(rhs.Metadata.OrderBy(m => m.Key));

    public override bool Equals(object? obj) =>
        obj is MarkdownEntry rhs && Equals(rhs);

    public override int GetHashCode() =>
        this.Metadata.Aggregate(
            this.RelativeContentPath.GetHashCode(),
            (agg, v) => agg ^ v.Key.GetHashCode() ^ v.Value?.GetHashCode() ?? 0);
}
