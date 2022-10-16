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
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Metadata;

public sealed class HtmlContentEntry :
    IMetadataEntry
{
    public readonly string Content;

    public HtmlContentEntry(string content) =>
        this.Content = content;

    public ValueTask<object?> GetImplicitValueAsync(
        IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        new(this.Content);

    public ValueTask<object?> GetPropertyValueAsync(
        string keyName, IMetadataContext metadata, IReducer reducer, CancellationToken ct) =>
        InternalUtilities.NullAsync;

    public void Deconstruct(out string content) =>
        content = this.Content;

    public override string ToString() =>
        $"HtmlContent={this.Content}";
}
