﻿/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper;

/// <summary>
/// Asynchronous safe and fast directory creator.
/// </summary>
public sealed class SafeDirectoryCreator
{
    private readonly AsyncResourceAllocator allocator = new();

    /// <summary>
    /// Create specified directory.
    /// </summary>
    /// <param name="dirPath">Directory path</param>
    /// <param name="ct">CancellationToken</param>
    public ValueTask CreateIfNotExistAsync(
        string dirPath, CancellationToken ct) =>
        this.allocator.AllocateAsync(dirPath, () =>
        {
            if (!Directory.Exists(dirPath))
            {
                try
                {
                    Directory.CreateDirectory(dirPath);
                }
                catch
                {
                }
            }
            return default;
        }, ct);
}
