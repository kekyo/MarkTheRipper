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
using System.Collections.Generic;

namespace MarkTheRipper.Internal;

internal sealed class MarkdownHeaderDateComparer :
    IComparer<MarkdownEntry>
{
    private static readonly MetadataContext empty = new();

    public static readonly MarkdownHeaderDateComparer Instance =
        new MarkdownHeaderDateComparer();

    private MarkdownHeaderDateComparer()
    {
    }

    private static DateTimeOffset GetValue(MarkdownEntry? markdownEntry) =>
        markdownEntry is { } &&
        markdownEntry.GetProperty("date", empty) is DateTimeOffset dto ?
            dto :
            DateTimeOffset.MaxValue;

    public int Compare(MarkdownEntry? x, MarkdownEntry? y)
    {
        var xd = GetValue(x);
        var yd = GetValue(y);
        return xd.CompareTo(yd);
    }
}
