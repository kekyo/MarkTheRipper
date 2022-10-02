/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Functions;
using MarkTheRipper.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Expressions;

public static class Reducer
{
    private static readonly char[] dotOperator = new[] { '.' };

    private static async ValueTask<object?> ReducePropertyAsync(
        string[] elements,
        int index,
        IExpression currentExpression,
        MetadataContext metadata,
        CancellationToken ct) =>
        currentExpression is ValueExpression(var currentValue) ?
            index < elements.Length &&
            currentValue is IMetadataEntry entry &&
            await entry.GetPropertyValueAsync(elements[index++], metadata, ct).
                ConfigureAwait(false) is { } value ?
                value :
                currentValue :
            currentExpression.ImplicitValue;

    private static ValueTask<object?> ReducePropertiesAsync(
        string[] elements,
        MetadataContext metadata,
        CancellationToken ct) =>
        0 < elements.Length &&
        metadata.Lookup(elements[0]) is { } expression ?
            ReducePropertyAsync(elements, 1, expression, metadata, ct) :
            new ValueTask<object?>(elements[0]);

#if DEBUG
    private static async ValueTask<object?[]> ReduceExpressionsAsync(
        IExpression[] expressions,
        MetadataContext metadata,
        CancellationToken ct)
    {
        var results = new List<object?>();
        foreach (var expression in expressions)
        {
            var result = await ReduceExpressionAsync(expression, metadata, ct).
                ConfigureAwait(false);
            results.Add(result);
        }
        return results.ToArray();
    }
#else
    private static ValueTask<object?[]> ReduceExpressionsAsync(
        IExpression[] expressions,
        MetadataContext metadata,
        CancellationToken ct) =>
        new ValueTask<object?[]>(
            Task.WhenAll(expressions.Select(expression =>
                ReduceExpressionAsync(expression, metadata, ct).AsTask())));
#endif

    private static async ValueTask<object?> ReduceArrayAsync(
        IExpression[] elements,
        MetadataContext metadata,
        CancellationToken ct) =>
        await ReduceExpressionsAsync(elements, metadata, ct).
            ConfigureAwait(false);

    private static async ValueTask<object?> ReduceApplyAsync(
        IExpression function,
        IExpression[] parameters,
        MetadataContext metadata,
        CancellationToken ct)
    {
        var f = await ReduceExpressionAsync(function, metadata, ct).
            ConfigureAwait(false);
        return f switch
        {
            AsyncFunctionDelegate func => await ReduceExpressionAsync(
                await func(parameters, metadata, ct).ConfigureAwait(false),
                metadata, ct).
                ConfigureAwait(false),
            Func<object?[], Func<string, Task<object?>>, IFormatProvider, CancellationToken, Task<object?>> func =>
                await func(
                    await ReduceExpressionsAsync(parameters, metadata, ct).
                        ConfigureAwait(false),
                    keyName => metadata.Lookup(keyName) is { } valueExpression ?
                        ReduceExpressionAsync(valueExpression, metadata, ct).AsTask() :
                        Task.FromResult(default(object)),
                    await MetadataUtilities.GetFormatProviderAsync(metadata, ct).
                        ConfigureAwait(false),
                    ct),
            _ => throw new InvalidOperationException("Could not apply non-function object."),
        };
    }

    public static ValueTask<object?> ReduceExpressionAsync(
        this IExpression expression,
        MetadataContext metadata,
        CancellationToken ct) =>
        expression switch
        {
            VariableExpression(var name) =>
                ReducePropertiesAsync(
                    name.Split(dotOperator, StringSplitOptions.RemoveEmptyEntries),
                    metadata, ct),
            ValueExpression(var value) =>
                new ValueTask<object?>(value),
            ArrayExpression(var elements) =>
                ReduceArrayAsync(elements, metadata, ct),
            ApplyExpression(var function, var parameters) =>
                ReduceApplyAsync(function, parameters, metadata, ct),
            _ => throw new InvalidOperationException(),
        };

    internal static object? UnsafeReduceExpression(
        IExpression expression,
        MetadataContext metadata) =>
        ReduceExpressionAsync(expression, metadata, default).Result;

    public static async ValueTask<string> ReduceExpressionAndFormatAsync(
        this IExpression expression,
        MetadataContext metadata,
        CancellationToken ct)
    {
        var reduced = await ReduceExpressionAsync(expression, metadata, ct).
            ConfigureAwait(false);
        return await MetadataUtilities.FormatValueAsync(reduced, metadata, ct).
            ConfigureAwait(false);
    }

    internal static string UnsafeReduceExpressionAndFormat(
        IExpression expression,
        MetadataContext metadata) =>
        ReduceExpressionAndFormatAsync(expression, metadata, default).Result;
}
