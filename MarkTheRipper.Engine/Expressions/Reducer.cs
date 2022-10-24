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

    private async ValueTask<object?> ReducePropertyAsync(
        string[] elements,
        int index,
        object? currentValue,
        IMetadataContext metadata,
        CancellationToken ct)
    {
        if (index < elements.Length)
        {
            if (currentValue is IMetadataEntry entry &&
                await entry.GetPropertyValueAsync(elements[index], metadata, this, ct) is { } childValue)
            {
                return childValue;
            }
            else
            {
                return elements[index];
            }
        }
        else
        {
            return currentValue;
        }
    }

    private ValueTask<object?> ReducePropertiesAsync(
        string[] elements,
        IMetadataContext metadata,
        CancellationToken ct)
    {
        if (metadata.Lookup(elements[0]) is { } expression)
        {
            if (expression is ValueExpression(var value))
            {
                return this.ReducePropertyAsync(elements, 1, value, metadata, ct);
            }
            else
            {
                return new ValueTask<object?>(expression.ImplicitValue);
            }
        }
        else
        {
            return new ValueTask<object?>(elements[0]);
        }
    }

    private async ValueTask<object?> ReduceApplyAsync(
        IExpression functionExpression,
        IExpression[] parameterExpressions,
        IMetadataContext metadata,
        CancellationToken ct)
    {
        var function = await this.ReduceExpressionAsync(
            functionExpression, metadata, ct);
        if (await this.InvokeFunctionAsync(
            function, parameterExpressions, metadata, ct) is (true, var result))
        {
            return result;
        }
        else
        {
            throw new InvalidOperationException("Could not apply non-function object.");
        }
    }

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
