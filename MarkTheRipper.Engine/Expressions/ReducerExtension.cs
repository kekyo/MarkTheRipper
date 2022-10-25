/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Functions;
using MarkTheRipper.Internal;
using MarkTheRipper.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Expressions;

public static class ReducerExtension
{
#if DEBUG
    internal static async ValueTask<object?[]> ReduceExpressionsAsync(
        this IReducer reducer,
        IExpression[] expressions,
        IMetadataContext metadata,
        CancellationToken ct)
    {
        var results = new List<object?>();
        foreach (var expression in expressions)
        {
            var result = await reducer.ReduceExpressionAsync(expression, metadata, ct);
            results.Add(result);
        }
        return results.ToArray();
    }
#else
    internal static ValueTask<object?[]> ReduceExpressionsAsync(
        this IReducer reducer,
        IExpression[] expressions,
        IMetadataContext metadata,
        CancellationToken ct) =>
        new ValueTask<object?[]>(
            Task.WhenAll(expressions.Select(expression =>
                reducer.ReduceExpressionAsync(expression, metadata, ct).AsTask())));
#endif

    internal static async ValueTask<(bool invoked, object? result)> InvokeFunctionAsync(
        this IReducer reducer,
        object? function,
        IExpression[] parameterExpressions,
        IMetadataContext metadata,
        CancellationToken ct) =>
        function switch
        {
            FunctionDelegate func => (true, await reducer.ReduceExpressionAsync(
                await func(parameterExpressions, metadata, reducer, ct),
                metadata, ct)),
            SimpleFunctionDelegate func => (true, await func(
                await reducer.ReduceExpressionsAsync(parameterExpressions, metadata, ct),
                keyName => metadata.Lookup(keyName) is { } valueExpression ?
                    reducer.ReduceExpressionAsync(valueExpression, metadata, ct) :
                    default,
                await metadata.GetLanguageAsync(ct),
                ct)),
            Func<object?[], Func<string, Task<object?>>, IFormatProvider, CancellationToken, Task<object?>> func =>
                (true, await func(
                    await reducer.ReduceExpressionsAsync(parameterExpressions, metadata, ct),
                    keyName => metadata.Lookup(keyName) is { } valueExpression ?
                        reducer.ReduceExpressionAsync(valueExpression, metadata, ct).AsTask() :
                        Task.FromResult(default(object)),
                    await metadata.GetLanguageAsync(ct),
                    ct)),
            _ => (false, function),
        };

    public static async ValueTask<object?> ReduceExpressionAndFinalApplyAsync(
        this IReducer reducer,
        IExpression expression,
        IMetadataContext metadata,
        CancellationToken ct) =>
        await reducer.InvokeFunctionAsync(
            // Final fixup: try reduce arity=0 function.
            await reducer.ReduceExpressionAsync(expression, metadata, ct),
            InternalUtilities.Empty<IExpression>(), metadata, ct) switch
        {
            (_, var result) => result,
        };

    public static async ValueTask<long?> ReduceIntegerExpressionAsync(
        this IReducer reducer,
        IExpression expression,
        IMetadataContext metadata,
        CancellationToken ct) =>
        await reducer.ReduceExpressionAsync(expression, metadata, ct) switch
        {
            int value => value,
            long value => value,
            _ => null,
        };
}
