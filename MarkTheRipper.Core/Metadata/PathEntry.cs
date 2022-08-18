/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;

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

    private PathEntry(string[] pathElements) =>
        this.PathElements = pathElements;

    public string Path =>
        string.Join("/", this.PathElements);

    internal string RealPath =>
        System.IO.Path.Combine(this.PathElements);

    object? IMetadataEntry.ImplicitValue =>
        this.Path;

    private static PathEntry CalculateRelativePath(
        PathEntry fromPath, PathEntry toPath)
    {
        var basePathElements = fromPath.PathElements.
            Take(fromPath.PathElements.Length - 1).
            ToArray();

        var commonPathElementCount = basePathElements.
            Zip(toPath.PathElements.Take(toPath.PathElements.Length - 1),
                (bp, tp) => bp == tp).
            Count();

        var relativePathElements =
            Enumerable.Range(0, basePathElements.Length - commonPathElementCount).
            Select(_ => "..").
            Concat(toPath.PathElements.Skip(commonPathElementCount)).
            ToArray();

        return new(relativePathElements);
    }

    public object? GetProperty(string keyName, MetadataContext context) =>
        keyName switch
        {
            // HACK: See Ripper.InjectMetadata.
            "relative" => context.Lookup("__currentContentPath") is PathEntry contentPath ?
                CalculateRelativePath(contentPath, this) : null,
            _ => null,
        };

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
