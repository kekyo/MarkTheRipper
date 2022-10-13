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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Metadata;

public sealed class MarkdownEntry :
    IMetadataEntry, IEquatable<MarkdownEntry>
{
    private readonly IReadOnlyDictionary<string, IExpression> metadata;

    internal readonly string contentBasePath;

    public MarkdownEntry(
        IReadOnlyDictionary<string, IExpression> metadata,
        string contentBasePath)
    {
        this.metadata = metadata;
        this.contentBasePath = contentBasePath;
    }

    public MarkdownEntry(
        IReadOnlyDictionary<string, object?> metadata,
        string contentBasePath)
    {
        this.metadata = metadata.ToDictionary(
            entry => entry.Key,
            entry => (IExpression)new ValueExpression(entry.Value));
        this.contentBasePath = contentBasePath;
    }

    public IReadOnlyDictionary<string, object?> Metadata =>
        this.metadata.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.ImplicitValue);

    internal PathEntry MarkdownPath =>
        this.metadata.TryGetValue("markdownPath", out var markdownPathExpression) &&
        Reducer.Instance.UnsafeReduceExpression(
            markdownPathExpression, MetadataContext.Empty) is { } value &&
            value is PathEntry markdownPath ?
            markdownPath : PathEntry.Unknown;

    internal PathEntry StoreToPath =>
        this.metadata.TryGetValue("path", out var pathExpression) &&
        Reducer.Instance.UnsafeReduceExpression(
            pathExpression, MetadataContext.Empty) is { } value &&
            value is PathEntry path ?
            path : PathEntry.Unknown;

    internal static bool GetPublishedState(
        IReadOnlyDictionary<string, IExpression> metadata) =>
        // true if `published` does not exist, or if it exists and true
        metadata.TryGetValue("published", out var publishedExpression) ?
            Reducer.Instance.UnsafeReduceExpression(
                publishedExpression, MetadataContext.Empty) is bool published && published :
        true;

    internal bool DoesNotPublish =>
        !GetPublishedState(this.metadata);

    internal string Title =>
        this.metadata.TryGetValue("title", out var titleExpression) &&
        Reducer.Instance.UnsafeReduceExpressionAndFormat(
            titleExpression, MetadataContext.Empty) is { } title ? 
            title : "(Untitled)";

    internal DateTimeOffset? Date =>
        this.metadata.TryGetValue("date", out var dateExpression) &&
        Reducer.Instance.UnsafeReduceExpression(
            dateExpression, MetadataContext.Empty) is DateTimeOffset date ?
            date : null;

    public async ValueTask<object?> GetImplicitValueAsync(
        IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        this.metadata.TryGetValue("title", out var valueExpression) &&
            await reducer.ReduceExpressionAndFormatAsync(
                valueExpression, metadata, ct).
                ConfigureAwait(false) is { } title ?
            title : null ?? "(Untitled)";

    public ValueTask<object?> GetPropertyValueAsync(
        string keyName, IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        this.metadata.TryGetValue(keyName, out var valueExpression) ?
            reducer.ReduceExpressionAsync(valueExpression, metadata, ct) :
            InternalUtilities.NullAsync;

    public bool Equals(MarkdownEntry? other) =>
        other is { } rhs &&
        this.metadata.
            OrderBy(m => m.Key).
            SequenceEqual(rhs.metadata.OrderBy(m => m.Key));

    public override bool Equals(object? obj) =>
        obj is MarkdownEntry rhs &&
        Equals(rhs);

    public override int GetHashCode() =>
        this.metadata.Aggregate(0, 
            (agg, v) => agg ^ v.Key.GetHashCode() ^ v.Value.GetHashCode());

    public override string ToString() =>
        $"Markdown={this.Title}";
}
