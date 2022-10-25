/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Metadata;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions;

internal static class Relative
{
    internal static PathEntry InternalCalculate(
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

    public static async ValueTask<IExpression> RelativeAsync(
        IExpression[] parameters, IMetadataContext metadata, IReducer reducer, CancellationToken ct)
    {
        if (parameters.Length != 1)
        {
            throw new ArgumentException(
                $"Invalid relative function arguments: Count={parameters.Length}");
        }

        if (metadata.Lookup("path") is { } currentPathExpression)
        {
            if (await reducer.ReduceExpressionAsync(currentPathExpression, metadata, ct) is { } currentPathValue &&
                await reducer.ReduceExpressionAsync(parameters[0], metadata, ct) is { } targetPathValue)
            {
                var relativePath = (currentPathValue, targetPathValue) switch
                {
                    (PathEntry cpe, PathEntry tpe) =>
                        InternalCalculate(cpe, tpe),
                    (PathEntry cpe, { } tp) => InternalCalculate(cpe,
                        new PathEntry(
                            await MetadataUtilities.FormatValueAsync(tp, metadata, ct))),
                    (string cp, PathEntry tpe) => InternalCalculate(
                        new PathEntry(
                            await MetadataUtilities.FormatValueAsync(cp, metadata, ct)), tpe),
                    (string cp, string tp) => InternalCalculate(
                        new PathEntry(
                            await MetadataUtilities.FormatValueAsync(cp, metadata, ct)),
                        new PathEntry(
                            await MetadataUtilities.FormatValueAsync(tp, metadata, ct))),
                    _ => throw new InvalidOperationException(
                        "Could not calculate relative path"),
                };

                return new ValueExpression(relativePath);
            }
        }

        // Could not find current path (calculation base path).
        return parameters[0];
    }
}
