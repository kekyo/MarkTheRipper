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

internal static class Lookup
{
    private static async ValueTask<object?> LookupAsync(
        IExpression[] parameters, MetadataContext metadata, CancellationToken ct)
    {
        if (parameters.Length != 1)
        {
            throw new ArgumentException(
                $"Invalid lookup function arguments: Count={parameters.Length}");
        }

        var parameter = parameters[0];
        if (Reducer.ReduceExpression(parameter, metadata) is { } rawValue &&
            await Reducer.FormatValueAsync(
                rawValue, parameters.Skip(1).ToArray(), metadata, ct).
                ConfigureAwait(false) is { } name &&
                metadata.Lookup(name) is { } value)
        {
            return value;
        }

        return parameter;
    }

    public static readonly AsyncFunctionDelegate Function =
        LookupAsync;
}
