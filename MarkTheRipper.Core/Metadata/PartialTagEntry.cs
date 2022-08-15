/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

namespace MarkTheRipper.Metadata;

internal sealed class PartialTagEntry :
    IMetadataEntry
{
    public readonly string Name;

    public PartialTagEntry(string name) =>
        this.Name = name;

    object? IMetadataEntry.ImplicitValue =>
        this.Name;

    private TagEntry? GetRealTagEntry(MetadataContext context) =>
        context.Lookup("tagList") is IReadOnlyDictionary<string, TagEntry> tagList &&
        tagList.TryGetValue(this.Name, out var tag) ?
            tag : null;

    public object? GetProperty(string keyName, MetadataContext context) =>
        this.GetRealTagEntry(context)?.GetProperty(keyName, context) ??
        keyName switch
        {
            "name" => this.Name,
            _ => null,
        };

    public override string ToString() =>
        $"PartialTag: {this.Name}";
}
