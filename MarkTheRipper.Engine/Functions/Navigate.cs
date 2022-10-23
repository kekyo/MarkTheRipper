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

internal static class Navigate
{
    private static async ValueTask<IExpression> GetNextToEntryAsync(
        int offset,
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
                    var targetIndex = currentIndex + offset;
                    if (targetIndex >= 0 && targetIndex < entries.Length)
                    {
                        return new ValueExpression(entries[targetIndex]);
                    }
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

    public static ValueTask<IExpression> OlderAsync(
        IExpression[] parameters,
        IMetadataContext metadata,
        IReducer reducer,
        CancellationToken ct) =>
        GetNextToEntryAsync(-1, parameters, metadata, reducer, ct);

    public static ValueTask<IExpression> NewerAsync(
        IExpression[] parameters,
        IMetadataContext metadata,
        IReducer reducer,
        CancellationToken ct) =>
        GetNextToEntryAsync(1, parameters, metadata, reducer, ct);
}
