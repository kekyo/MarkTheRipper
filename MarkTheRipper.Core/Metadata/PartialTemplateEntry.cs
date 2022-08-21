/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using MarkTheRipper.Template;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Metadata;

internal sealed class PartialTemplateEntry :
    IMetadataEntry
{
    public readonly string Name;

    public PartialTemplateEntry(string name) =>
        this.Name = name;

    public ValueTask<object?> GetImplicitValueAsync(CancellationToken ct) =>
        new(this.Name);

    private async ValueTask<RootTemplateNode?> GetRealTemplateNodeAsync(
        MetadataContext metadata, CancellationToken ct) =>
        metadata.Lookup("templateList") is { } templateListExpression &&
        await Reducer.ReduceExpressionAsync(templateListExpression, metadata, ct).
            ConfigureAwait(false) is IReadOnlyDictionary<string, RootTemplateNode> templateList &&
        templateList.TryGetValue(this.Name, out var template) ?
            template : null;

    public async ValueTask<object?> GetPropertyValueAsync(
        string keyName, MetadataContext metadata, CancellationToken ct) =>
        await this.GetRealTemplateNodeAsync(metadata, ct).
            ConfigureAwait(false) is { } template &&
        await template.GetPropertyValueAsync(keyName, metadata, ct).
            ConfigureAwait(false) is { } value ?
            value :
            keyName switch
            {
                "name" => this.Name,
                _ => null,
            };

    public override string ToString() =>
        $"PartialTemplate={this.Name}";
}
