﻿/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Metadata;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.TextTreeNodes;

public interface ITextTreeNode
{
    ValueTask RenderAsync(
        Action<string> writer,
        IMetadataContext metadata,
        CancellationToken ct);
}
