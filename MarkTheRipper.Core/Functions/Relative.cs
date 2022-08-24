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
using MarkTheRipper.Template;
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

    private static async ValueTask<IExpression> CalculateAsync(
        IExpression[] parameters,
        MetadataContext metadata,
        CancellationToken ct)
    {
        if (parameters.Length != 1)
        {
            throw new ArgumentException(
                $"Invalid relative function arguments: Count={parameters.Length}");
        }

        if (metadata.Lookup("path") is { } currentPathExpression)
        {
            if (await currentPathExpression.ReduceExpressionAsync(metadata, ct).
                ConfigureAwait(false) is { } currentPathValue &&
                await parameters[0].ReduceExpressionAsync(metadata, ct).
                ConfigureAwait(false) is { } targetPathValue)
            {
                var relativePath = (currentPathValue, targetPathValue) switch
                {
                    (PathEntry cpe, PathEntry tpe) =>
                        InternalCalculate(cpe, tpe),
                    (PathEntry cpe, { } tp) =>
                        InternalCalculate(cpe, new PathEntry(
                            await Reducer.FormatValueAsync(tp, metadata, ct).
                                ConfigureAwait(false))),
                    (string cp, PathEntry tpe) =>
                        InternalCalculate(new PathEntry(
                            await Reducer.FormatValueAsync(cp, metadata, ct).
                                ConfigureAwait(false)), tpe),
                    (string cp, string tp) =>
                        InternalCalculate(new PathEntry(
                            await Reducer.FormatValueAsync(cp, metadata, ct).
                                ConfigureAwait(false)), new PathEntry(
                            await Reducer.FormatValueAsync(tp, metadata, ct).
                                ConfigureAwait(false))),
                    _ => throw new InvalidOperationException(
                        "Could not calculate relative path"),
                };

                return new ValueExpression(relativePath);
            }
        }

        // Could not find current path (calculation base path).
        return parameters[0];
    }

    public static readonly AsyncFunctionDelegate Function =
        CalculateAsync;
}
