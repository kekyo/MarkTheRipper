/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using System;
using System.ComponentModel;

namespace MarkTheRipper.Metadata;

public interface IMetadataContext
{
    void Set(string keyName, IExpression expression);

    IExpression? Lookup(string keyName);

    IMetadataContext Spawn();
}
