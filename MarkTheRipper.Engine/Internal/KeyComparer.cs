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

internal sealed class KeyComparer<TKey, TValue> :
    IEqualityComparer<KeyValuePair<TKey, TValue>>
    where TKey : notnull
{
    private KeyComparer()
    {
    }

    public bool Equals(
        KeyValuePair<TKey, TValue> x,
        KeyValuePair<TKey, TValue> y) =>
        x.Key.Equals(y.Key);

    public int GetHashCode(
        KeyValuePair<TKey, TValue> obj) =>
        obj.Key.GetHashCode();

    public static readonly KeyComparer<TKey, TValue> Instance = new();
}
