/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Metadata;

namespace MarkTheRipper;

public sealed class RippingContext
{
    public readonly MarkdownEntry MarkdownEntry;
    public readonly MetadataContext MetadataContext;

    public RippingContext(
        MarkdownEntry markdownEntry,
        MetadataContext metadataContext)
    {
        this.MarkdownEntry = markdownEntry;
        this.MetadataContext = metadataContext;
    }
}
