/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Template;
using System.Collections.Generic;

namespace MarkTheRipper.Metadata;

internal sealed class PartialTemplateEntry :
    IMetadataEntry
{
    public readonly string Name;

    public PartialTemplateEntry(string name) =>
        this.Name = name;

    object? IMetadataEntry.ImplicitValue =>
        this.Name;

    public object? GetProperty(string keyName, MetadataContext context) =>
        context.Lookup("templateList") is IReadOnlyDictionary<string, RootTemplateNode> templateList &&
        templateList.TryGetValue(keyName, out var template) ?
            template :
            keyName switch
            {
                "name" => this.Name,
                _ => null,
            };

    public override string ToString() =>
        $"PartialTemplate={this.Name}";
}
