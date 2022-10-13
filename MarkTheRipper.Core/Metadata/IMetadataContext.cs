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

public static class MetadataContextExtension
{
    public static void SetValue(
        this IMetadataContext metadata, string keyName, object? value) =>
        metadata.Set(keyName, new ValueExpression(value));

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Invalid usage, use Set() instead.", true)]
    public static void SetValue(
        this IMetadataContext metadata, string keyName, IExpression expression) =>
        throw new InvalidOperationException("Invalid usage, use Set() instead.");
}
