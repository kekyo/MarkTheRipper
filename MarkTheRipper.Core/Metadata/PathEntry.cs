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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Metadata;

public sealed class PathEntry :
    IMetadataEntry, IEquatable<PathEntry>
{
    private static readonly char[] separators = new[]
    {
        System.IO.Path.DirectorySeparatorChar,
        System.IO.Path.AltDirectorySeparatorChar,
    };

    public readonly string[] PathElements;

    public PathEntry(string path) =>
        this.PathElements = path.Split(separators, StringSplitOptions.RemoveEmptyEntries);

    internal PathEntry(string[] pathElements) =>
        this.PathElements = pathElements;

    public string Path =>
        string.Join("/", this.PathElements);

    internal string PhysicalPath =>
        System.IO.Path.Combine(this.PathElements);

    public ValueTask<object?> GetImplicitValueAsync(
        MetadataContext metadata, CancellationToken ct) =>
        new(this.Path);

    public ValueTask<object?> GetPropertyValueAsync(
        string keyName, MetadataContext metadata, CancellationToken ct) =>
        Utilities.NullAsync;

    public bool Equals(PathEntry? other) =>
        other is { } rhs &&
        this.PathElements.SequenceEqual(rhs.PathElements);

    public override bool Equals(object? obj) =>
        obj is PathEntry rhs &&
        this.PathElements.SequenceEqual(rhs.PathElements);

    public override int GetHashCode() =>
        this.PathElements.Aggregate(0, (agg, v) => v.GetHashCode() ^ agg);

    public override string ToString() =>
        $"Path={this.Path}";

    public static readonly PathEntry Unknown =
        new PathEntry("(unknown)");
}
