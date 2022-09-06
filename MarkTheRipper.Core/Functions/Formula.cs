﻿/////////////////////////////////////////////////////////////////////////////////////
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

internal static class Formula
{
    private static async ValueTask<IExpression> ComputeAsync(
        IExpression[] parameters,
        Func<long, long, long> int64Accumrator,
        Func<double, double, double> doubleAccumrator,
        MetadataContext metadata, CancellationToken ct)
    {
        if (parameters.Length == 0)
        {
            throw new ArgumentException(
                $"Invalid formula function arguments: Count={parameters.Length}");
        }

        if (parameters.Length == 1)
        {
            return parameters[0];
        }

        var values = await Task.WhenAll(
            parameters.Select(p => p.ReduceExpressionAsync(metadata, ct).AsTask())).
            ConfigureAwait(false);

        var fp = await MetadataUtilities.GetFormatProviderAsync(metadata, ct).
            ConfigureAwait(false);

        if (values.All(v => v is int || v is long ||
            (v is string s && long.TryParse(s, out var _))))
        {
            var result = values.
                Select(v => Convert.ToInt64(v, fp)).
                Aggregate(int64Accumrator);
            return new ValueExpression(result);
        }
        else
        {
            var result = values.
                Select(v => Convert.ToDouble(v, fp)).
                Aggregate(doubleAccumrator);
            return new ValueExpression(result);
        }
    }

    public static ValueTask<IExpression> AddAsync(
        IExpression[] parameters, MetadataContext metadata, CancellationToken ct) =>
        ComputeAsync(
            parameters,
            (lhs, rhs) => lhs + rhs,
            (lhs, rhs) => lhs + rhs,
            metadata,
            ct);

    public static ValueTask<IExpression> SubtractAsync(
        IExpression[] parameters, MetadataContext metadata, CancellationToken ct) =>
        ComputeAsync(
            parameters,
            (lhs, rhs) => lhs - rhs,
            (lhs, rhs) => lhs - rhs,
            metadata,
            ct);

    public static ValueTask<IExpression> MultipleAsync(
        IExpression[] parameters, MetadataContext metadata, CancellationToken ct) =>
        ComputeAsync(
            parameters,
            (lhs, rhs) => lhs * rhs,
            (lhs, rhs) => lhs * rhs,
            metadata,
            ct);

    public static ValueTask<IExpression> DivideAsync(
        IExpression[] parameters, MetadataContext metadata, CancellationToken ct) =>
        ComputeAsync(
            parameters,
            (lhs, rhs) => lhs / rhs,
            (lhs, rhs) => lhs / rhs,
            metadata,
            ct);

    public static ValueTask<IExpression> ModuloAsync(
        IExpression[] parameters, MetadataContext metadata, CancellationToken ct) =>
        ComputeAsync(
            parameters,
            (lhs, rhs) => lhs % rhs,
            (lhs, rhs) => lhs % rhs,
            metadata,
            ct);
}