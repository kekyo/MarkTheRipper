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

public interface IEntry
{
    object? GetProperty(string keyName);
}

public interface IEnumerableEntry<T>
{
    IEnumerable<T> GetEntries();
}

public sealed class TagEntry :
    IEntry, IEnumerableEntry<MarkdownEntry>
{
    public readonly string Name;
    public readonly MarkdownEntry[] Entries;

    public TagEntry(
        string name, MarkdownEntry[] markdownEntries)
    {
        this.Name = name;
        this.Entries = markdownEntries;
    }

    public object? GetProperty(string keyName) =>
        keyName switch
        {
            "name" => this.Name,
            _ => null,
        };

    public IEnumerable<MarkdownEntry> GetEntries() =>
        this.Entries;
}

public sealed class CategoryEntry :
    IEntry, IEnumerableEntry<MarkdownEntry>
{
    public readonly string Name;
    public readonly IReadOnlyDictionary<string, CategoryEntry> Children;
    public readonly MarkdownEntry[] Entries;

    public CategoryEntry(
        string name,
        IReadOnlyDictionary<string, CategoryEntry> children,
        MarkdownEntry[] markdownEntries)
    {
        this.Name = name;
        this.Children = children;
        this.Entries = markdownEntries;
    }

    public object? GetProperty(string keyName) =>
        keyName switch
        {
            "name" => this.Name,
            "children" => this.Children,
            _ => null,
        };

    public IEnumerable<MarkdownEntry> GetEntries() =>
        this.Entries;
}

public sealed class MarkdownEntry :
    IEntry, IEquatable<MarkdownEntry>
{
    public readonly string RelativePath;
    public readonly IReadOnlyDictionary<string, object?> Metadata;

    internal readonly string ContentBasePath;

    public MarkdownEntry(
        string relativePath,
        IReadOnlyDictionary<string, object?> metadata,
        string contentBasePath)
    {
        this.RelativePath = relativePath;
        this.Metadata = metadata;
        this.ContentBasePath = contentBasePath;
    }

    public object? GetProperty(string keyName) =>
        keyName switch
        {
            "relativePath" => this.RelativePath,
            _ => this.Metadata.TryGetValue(keyName, out var value) ?
                value : null,
        };

    public bool Equals(MarkdownEntry? other) =>
        other is { } rhs &&
        this.RelativePath.Equals(rhs.RelativePath) &&
        this.Metadata.
            OrderBy(m => m.Key).
            SequenceEqual(rhs.Metadata.OrderBy(m => m.Key));

    public override bool Equals(object? obj) =>
        obj is MarkdownEntry rhs && this.Equals(rhs);

    public override int GetHashCode() =>
        this.Metadata.Aggregate(
            this.RelativePath.GetHashCode(),
            (agg, v) => agg ^ v.Key.GetHashCode() ^ v.Value?.GetHashCode() ?? 0);
}
