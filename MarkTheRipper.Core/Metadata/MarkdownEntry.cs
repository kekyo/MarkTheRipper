/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarkTheRipper.Metadata;

public sealed class MarkdownEntry :
    IMetadataEntry, IEquatable<MarkdownEntry>
{
    public readonly IReadOnlyDictionary<string, object?> Metadata;

    internal readonly string contentBasePath;

    public MarkdownEntry(
        Dictionary<string, object?> metadata,
        string contentBasePath)
    {
        this.Metadata = metadata;
        this.contentBasePath = contentBasePath;
    }

    internal PathEntry MarkdownPath =>
        this.Metadata.TryGetValue("markdownPath", out var value) &&
            value is PathEntry markdownPath ?
            markdownPath : PathEntry.Unknown;

    internal PathEntry StoreToPath =>
        this.Metadata.TryGetValue("path", out var value) &&
            value is PathEntry path ?
            path : PathEntry.Unknown;

    internal string Title =>
        this.Metadata.TryGetValue("title", out var value) &&
            Expression.FormatValue(value, null, MetadataContext.Empty) is { } title ? 
            title : null ?? "(Untitled)";

    object? IMetadataEntry.ImplicitValue =>
        this.Title;

    public object? GetProperty(string keyName, MetadataContext context) =>
        this.Metadata.TryGetValue(keyName, out var value) ?
            value : null;

    public bool Equals(MarkdownEntry? other) =>
        other is { } rhs &&
        this.Metadata.
            OrderBy(m => m.Key).
            SequenceEqual(rhs.Metadata.OrderBy(m => m.Key));

    public override bool Equals(object? obj) =>
        obj is MarkdownEntry rhs && Equals(rhs);

    public override int GetHashCode() =>
        this.Metadata.Aggregate(0, 
            (agg, v) => agg ^ v.Key.GetHashCode() ^ v.Value?.GetHashCode() ?? 0);

    public override string ToString() =>
        $"Markdown={this.Title}";
}
