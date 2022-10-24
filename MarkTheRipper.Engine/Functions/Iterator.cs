/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Internal;
using MarkTheRipper.Metadata;
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions;

internal static class Iterator
{
    private static async ValueTask<IExpression> GetNextToEntryAsync(
        bool forward,
        IExpression[] parameters,
        IMetadataContext metadata,
        IReducer reducer,
        CancellationToken ct)
    {
        async ValueTask<IExpression> GetNextToEntryAsync(MarkdownEntry targetEntry)
        {
            if (await targetEntry.GetPropertyValueAsync("markdownPath", metadata, reducer, ct) is PathEntry markdownPath &&
                await targetEntry.GetPropertyValueAsync("date", metadata, reducer, ct) is PartialDateEntry date &&
                await targetEntry.GetPropertyValueAsync("category", metadata, reducer, ct) is PartialCategoryEntry category &&
                await category.GetPropertyValueAsync("entries", metadata, reducer, ct) is MarkdownEntry[] entries)
            {
                var currentIndex = Array.FindIndex(
                    entries,
                    entry => entry.MarkdownPath.Equals(markdownPath));
                if (currentIndex >= 0)
                {
                    static IEnumerable IterateEntries(int offset, MarkdownEntry[] entries, bool forward)
                    {
                        if (forward)
                        {
                            for (var index = offset + 1; index < entries.Length; index++)
                            {
                                yield return entries[index];
                            }
                        }
                        else
                        {
                            for (var index = offset - 1; index >= 0; index--)
                            {
                                yield return entries[index];
                            }
                        }
                    }
                    return new ValueExpression(IterateEntries(currentIndex, entries, forward));
                }
            }

            return new ValueExpression(null);
        }

        if (parameters.FirstOrDefault() is { } p0 &&
            await reducer.ReduceExpressionAsync(p0, metadata, ct) is MarkdownEntry targetEntry1)
        {
            return await GetNextToEntryAsync(targetEntry1);
        }
        else if (metadata.Lookup("self") is { } self &&
            await reducer.ReduceExpressionAsync(self, metadata, ct) is MarkdownEntry targetEntry2)
        {
            return await GetNextToEntryAsync(targetEntry2);
        }

        return new ValueExpression(null);
    }

    public static async ValueTask<IExpression> TakeAsync(
        IExpression[] parameters,
        IMetadataContext metadata,
        IReducer reducer,
        CancellationToken ct)
    {
        if (parameters.Length != 2)
        {
            throw new ArgumentException(
                $"Invalid take function arguments: Count={parameters.Length}");
        }

        if (await reducer.ReduceIntegerExpressionAsync(
            parameters[1], metadata, ct) is { } count &&
            await reducer.ReduceExpressionAsync(
            parameters[0], metadata, ct) is IEnumerable enumerable)
        {
            return new ValueExpression(enumerable.Cast<object?>().Take((int)count));
        }
        else
        {
            return new ValueExpression(InternalUtilities.Empty<object?>());
        }
    }
}
