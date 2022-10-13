﻿/////////////////////////////////////////////////////////////////////////////////////
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

internal sealed class Reducer : IReducer
{
    private static readonly char[] dotOperator = new[] { '.' };

    private Reducer()
    {
    }

    private async ValueTask<object?> ReducePropertyAsync(
        string[] elements,
        int index,
        IExpression currentExpression,
        IMetadataContext metadata,
        CancellationToken ct) =>
        currentExpression is ValueExpression(var currentValue) ?
            index < elements.Length &&
            currentValue is IMetadataEntry entry &&
            await entry.GetPropertyValueAsync(elements[index++], metadata, this, ct).
                ConfigureAwait(false) is { } value ?
                value :
                currentValue :
            currentExpression.ImplicitValue;

    private ValueTask<object?> ReducePropertiesAsync(
        string[] elements,
        IMetadataContext metadata,
        CancellationToken ct) =>
        0 < elements.Length &&
        metadata.Lookup(elements[0]) is { } expression ?
            this.ReducePropertyAsync(elements, 1, expression, metadata, ct) :
            new ValueTask<object?>(elements[0]);

#if DEBUG
    private async ValueTask<object?[]> ReduceExpressionsAsync(
        IExpression[] expressions,
        IMetadataContext metadata,
        CancellationToken ct)
    {
        var results = new List<object?>();
        foreach (var expression in expressions)
        {
            var result = await this.ReduceExpressionAsync(expression, metadata, ct).
                ConfigureAwait(false);
            results.Add(result);
        }
        return results.ToArray();
    }
#else
    private ValueTask<object?[]> ReduceExpressionsAsync(
        IExpression[] expressions,
        IMetadataContext metadata,
        CancellationToken ct) =>
        new ValueTask<object?[]>(
            Task.WhenAll(expressions.Select(expression =>
                this.ReduceExpressionAsync(expression, metadata, ct).AsTask())));
#endif

    private async ValueTask<object?> ReduceArrayAsync(
        IExpression[] elements,
        IMetadataContext metadata,
        CancellationToken ct) =>
        await this.ReduceExpressionsAsync(elements, metadata, ct).
            ConfigureAwait(false);

    private async ValueTask<object?> ReduceApplyAsync(
        IExpression function,
        IExpression[] parameters,
        IMetadataContext metadata,
        CancellationToken ct)
    {
        var f = await this.ReduceExpressionAsync(function, metadata, ct).
            ConfigureAwait(false);
        return f switch
        {
            FunctionDelegate func => await this.ReduceExpressionAsync(
                await func(parameters, metadata, this, ct).ConfigureAwait(false),
                metadata, ct).
                ConfigureAwait(false),
            SimpleFunctionDelegate func =>
                await func(
                    await this.ReduceExpressionsAsync(parameters, metadata, ct).
                        ConfigureAwait(false),
                    keyName => metadata.Lookup(keyName) is { } valueExpression ?
                        this.ReduceExpressionAsync(valueExpression, metadata, ct) :
                        default,
                    await MetadataUtilities.GetFormatProviderAsync(metadata, ct).
                        ConfigureAwait(false),
                    ct),
            Func<object?[], Func<string, Task<object?>>, IFormatProvider, CancellationToken, Task<object?>> func =>
                await func(
                    await this.ReduceExpressionsAsync(parameters, metadata, ct).
                        ConfigureAwait(false),
                    keyName => metadata.Lookup(keyName) is { } valueExpression ?
                        this.ReduceExpressionAsync(valueExpression, metadata, ct).AsTask() :
                        Task.FromResult(default(object)),
                    await MetadataUtilities.GetFormatProviderAsync(metadata, ct).
                        ConfigureAwait(false),
                    ct),
            _ => throw new InvalidOperationException("Could not apply non-function object."),
        };
    }

    public ValueTask<object?> ReduceExpressionAsync(
        IExpression expression,
        IMetadataContext metadata,
        CancellationToken ct) =>
        expression switch
        {
            VariableExpression(var name) =>
                this.ReducePropertiesAsync(
                    name.Split(dotOperator, StringSplitOptions.RemoveEmptyEntries),
                    metadata, ct),
            ValueExpression(var value) =>
                new ValueTask<object?>(value),
            ArrayExpression(var elements) =>
                this.ReduceArrayAsync(elements, metadata, ct),
            ApplyExpression(var function, var parameters) =>
                this.ReduceApplyAsync(function, parameters, metadata, ct),
            _ => throw new InvalidOperationException(),
        };

    internal object? UnsafeReduceExpression(
        IExpression expression,
        IMetadataContext metadata) =>
        this.ReduceExpressionAsync(expression, metadata, default).Result;

    public async ValueTask<string> ReduceExpressionAndFormatAsync(
        IExpression expression,
        IMetadataContext metadata,
        CancellationToken ct)
    {
        var reduced = await this.ReduceExpressionAsync(expression, metadata, ct).
            ConfigureAwait(false);
        return await MetadataUtilities.FormatValueAsync(reduced, metadata, ct).
            ConfigureAwait(false);
    }

    internal string UnsafeReduceExpressionAndFormat(
        IExpression expression,
        IMetadataContext metadata) =>
        this.ReduceExpressionAndFormatAsync(expression, metadata, default).Result;

    public static readonly Reducer Instance = new();
}
