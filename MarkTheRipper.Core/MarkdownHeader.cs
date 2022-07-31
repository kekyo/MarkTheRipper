/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

namespace MarkTheRipper;

public readonly struct MarkdownHeader
{
    public readonly string RelativeContentPath;
    public readonly IReadOnlyDictionary<string, object?> Metadata;

    public MarkdownHeader(
        string relativeContentPath,
        IReadOnlyDictionary<string, object?> metadata)
    {
        this.RelativeContentPath = relativeContentPath;
        this.Metadata = metadata;
    }
}
