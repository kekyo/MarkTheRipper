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

public readonly struct MarkdownContent
{
    public readonly IReadOnlyDictionary<string, object?> Metadata;
    public readonly string Body;

    public MarkdownContent(
        IReadOnlyDictionary<string, object?> metadata,
        string body)
    {
        this.Metadata = metadata;
        this.Body = body;
    }
}
