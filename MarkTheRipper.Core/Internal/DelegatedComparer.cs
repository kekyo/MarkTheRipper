/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace MarkTheRipper.Internal;

internal sealed class DelegatedComparer<T> : IComparer<T>
{
    private readonly Func<T, T, int> comparer;

    public DelegatedComparer(Func<T, T, int> comparer) =>
        this.comparer = comparer;

    public int Compare(T? x, T? y) =>
        this.comparer(x!, y!);
}
