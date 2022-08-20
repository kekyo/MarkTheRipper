/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using MarkTheRipper.Metadata;
using MarkTheRipper.Template;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions;

internal static class CalculateRelativePath
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

    private static async ValueTask<object?> CalculateAsync(
        IExpression[] parameters, MetadataContext context, CancellationToken ct)
    {
        if (parameters.Length != 1)
        {
            throw new ArgumentException(
                $"Invalid relative function arguments: Count={parameters.Length}");
        }

        var parameter = parameters[0];
        if (context.Lookup("path") is PathEntry currentPath)
        {
            if (Reducer.ReduceExpression(parameter, context) is { } rawValue &&
                await Reducer.FormatValueAsync(
                    rawValue, parameters.Skip(1).ToArray(), context, ct).
                    ConfigureAwait(false) is { } value)
            {
                return InternalCalculate(currentPath, new PathEntry(value));
            }
        }

        return parameter;
    }

    public static readonly AsyncFunctionDelegate Function =
        CalculateAsync;
}
