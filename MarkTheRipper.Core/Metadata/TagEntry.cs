/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Metadata;

public sealed class TagEntry :
    IMetadataEntry
{
    public readonly string Name;
    public readonly MarkdownEntry[] Entries;

    public TagEntry(string name) :
        this(name, Utilities.Empty<MarkdownEntry>())
    {
    }

    public TagEntry(
        string name, MarkdownEntry[] markdownEntries)
    {
        this.Name = name;
        this.Entries = markdownEntries;
    }

    public ValueTask<object?> GetImplicitValueAsync(CancellationToken ct) =>
        new(this.Name);

    public ValueTask<object?> GetPropertyValueAsync(
        string keyName, MetadataContext metadata, CancellationToken ct) =>
        keyName switch
        {
            "name" => new(this.Name),
            "entries" => new(this.Entries),
            _ => Utilities.NullAsync,
        };

    public override string ToString() =>
        $"Tag={this.Name}";
}
