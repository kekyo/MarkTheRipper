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

internal static class Formula
{
    private static async ValueTask<IExpression> ComputeAsync(
        IExpression[] parameters,
        Func<long, long, long> int64Accumrator,
        Func<double, double, double> doubleAccumrator,
        IMetadataContext metadata, IReducer reducer, CancellationToken ct)
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
            parameters.Select(p => reducer.ReduceExpressionAsync(p, metadata, ct).AsTask()));

        var cultureInfo = await metadata.GetLanguageAsync(ct);

        if (values.All(v => v is int || v is long ||
            (v is string s && long.TryParse(s, out var _))))
        {
            var result = values.
                Select(v => Convert.ToInt64(v, cultureInfo)).
                Aggregate(int64Accumrator);
            return new ValueExpression(result);
        }
        else
        {
            var result = values.
                Select(v => Convert.ToDouble(v, cultureInfo)).
                Aggregate(doubleAccumrator);
            return new ValueExpression(result);
        }
    }

    public static ValueTask<IExpression> AddAsync(
        IExpression[] parameters, IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        ComputeAsync(
            parameters,
            (lhs, rhs) => lhs + rhs,
            (lhs, rhs) => lhs + rhs,
            metadata,
            reducer,
            ct);

    public static ValueTask<IExpression> SubtractAsync(
        IExpression[] parameters, IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        ComputeAsync(
            parameters,
            (lhs, rhs) => lhs - rhs,
            (lhs, rhs) => lhs - rhs,
            metadata,
            reducer,
            ct);

    public static ValueTask<IExpression> MultipleAsync(
        IExpression[] parameters, IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        ComputeAsync(
            parameters,
            (lhs, rhs) => lhs * rhs,
            (lhs, rhs) => lhs * rhs,
            metadata,
            reducer,
            ct);

    public static ValueTask<IExpression> DivideAsync(
        IExpression[] parameters, IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        ComputeAsync(
            parameters,
            (lhs, rhs) => lhs / rhs,
            (lhs, rhs) => lhs / rhs,
            metadata,
            reducer,
            ct);

    public static ValueTask<IExpression> ModuloAsync(
        IExpression[] parameters, IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        ComputeAsync(
            parameters,
            (lhs, rhs) => lhs % rhs,
            (lhs, rhs) => lhs % rhs,
            metadata,
            reducer,
            ct);
}
