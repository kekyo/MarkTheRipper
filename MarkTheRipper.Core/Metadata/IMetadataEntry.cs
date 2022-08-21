/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Metadata;

public interface IMetadataEntry
{
    ValueTask<object?> GetImplicitValueAsync(
        CancellationToken ct);

    ValueTask<object?> GetPropertyValueAsync(
        string keyName, MetadataContext context, CancellationToken ct);
}
