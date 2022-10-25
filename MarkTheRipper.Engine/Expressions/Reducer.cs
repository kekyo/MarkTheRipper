/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Metadata;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Expressions;

internal sealed class Reducer : IReducer
{
    private static readonly char[] dotOperator = new[] { '.' };

    private Reducer()
    {
    }

    private async ValueTask<object?> ReducePropertiesAsync(
        string[] elements,
        object? value0,
        IMetadataContext metadata,
        CancellationToken ct)
    {
        var currentValue = value0;
        for (var index = 1; index < elements.Length; index++)
        {
            if (currentValue is IMetadataEntry entry &&
                await entry.GetPropertyValueAsync(
                    elements[index], metadata, this, ct) is { } childValue)
            {
                currentValue = childValue;
            }
            else
            {
                return elements[index];
            }
        }
        return currentValue;
    }

    private ValueTask<object?> ReducePropertiesAsync(
        string[] elements,
        IMetadataContext metadata,
        CancellationToken ct) =>
        metadata.Lookup(elements[0]) is { } expression ?
            (expression is ValueExpression(var value) ?
                this.ReducePropertiesAsync(elements, value, metadata, ct) :
                new ValueTask<object?>(expression.ImplicitValue)) :
            new ValueTask<object?>(elements[0]);

    private async ValueTask<object?> ReduceApplyAsync(
        IExpression functionExpression,
        IExpression[] parameterExpressions,
        IMetadataContext metadata,
        CancellationToken ct) =>
        await this.InvokeFunctionAsync(
            await this.ReduceExpressionAsync(
                functionExpression, metadata, ct),
            parameterExpressions, metadata, ct) is (true, var result) ?
                result :
                throw new InvalidOperationException("Could not apply non-function object.");

    private async ValueTask<object?> ReduceArrayAsync(
        IExpression[] elements,
        IMetadataContext metadata,
        CancellationToken ct) =>
        await this.ReduceExpressionsAsync(
            elements, metadata, ct);

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
        var reduced = await this.ReduceExpressionAsync(expression, metadata, ct);
        return await MetadataUtilities.FormatValueAsync(reduced, metadata, ct);
    }

    internal string UnsafeReduceExpressionAndFormat(
        IExpression expression,
        IMetadataContext metadata) =>
        this.ReduceExpressionAndFormatAsync(expression, metadata, default).Result;

    public static readonly Reducer Instance = new();
}
