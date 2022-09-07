/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using MarkTheRipper.TextTreeNodes;
using MarkTheRipper.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarkTheRipper.Expressions;
using MarkTheRipper.Functions;

namespace MarkTheRipper;

/// <summary>
/// Higher level interface for Ripper.
/// </summary>
public static class Driver
{
    private static async ValueTask<RootTextNode> ReadLayoutAsync(
        Ripper ripper,
        PathEntry layoutPath,
        CancellationToken ct)
    {
        using var rs = new FileStream(
            layoutPath.PhysicalPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            65536,
            true);
        using var tr = new StreamReader(
            rs,
            Utilities.UTF8,
            true);

        return await ripper.ParseLayoutAsync(
            layoutPath,
            tr,
            ct).
            ConfigureAwait(false);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Run Ripper.
    /// </summary>
    /// <param name="output">Log output target</param>
    /// <param name="storeToBasePath">Store to base path</param>
    /// <param name="resourceBasePath">Resource base path</param>
    /// <param name="contentsBasePathList">Markdown content placed directory path iterator</param>
    /// <param name="requiredBeforeCleanup">Before cleanup</param>
    /// <param name="ct">CancellationToken</param>
    public static async ValueTask RunAsync(
        TextWriter output,
        string storeToBasePath,
        string resourceBasePath,
        IEnumerable<string> contentsBasePathList,
        bool requiredBeforeCleanup,
        CancellationToken ct)
    {
        await output.WriteLineAsync(
            $"Contents base path: {string.Join(", ", contentsBasePathList)}").
            WithCancellation(ct).
            ConfigureAwait(false);
        await output.WriteLineAsync(
            $"Resource base path: {resourceBasePath}").
            WithCancellation(ct).
            ConfigureAwait(false);
        await output.WriteLineAsync(
            $"Store to base path: {storeToBasePath}").
            WithCancellation(ct).
            ConfigureAwait(false);
        await output.WriteLineAsync().
            WithCancellation(ct).
            ConfigureAwait(false);

        var sw = new Stopwatch();
        sw.Start();

        //////////////////////////////////////////////////////////////

        var metadataList = (await Task.WhenAll(
            Directory.EnumerateFiles(
                resourceBasePath, "metadata*.json", SearchOption.TopDirectoryOnly).
            Select(metadataPath => MetadataUtilities.ReadMetadataAsync(metadataPath, ct).AsTask())).
            ConfigureAwait(false)).
            SelectMany(metadata => metadata).
            DistinctBy(entry => entry.Key).
            ToArray();
        if (metadataList.Length >= 1)
        {
            await output.WriteLineAsync(
                $"Read metadata: {metadataList.Length}").
                WithCancellation(ct).
                ConfigureAwait(false);
            await output.WriteLineAsync().
                WithCancellation(ct).
                ConfigureAwait(false);
        }

        //////////////////////////////////////////////////////////////

        var ripper = new Ripper();

        var layoutList = (await Task.WhenAll(
            Directory.EnumerateFiles(
                resourceBasePath, "layout-*.html", SearchOption.TopDirectoryOnly).
            Select(async layoutPath =>
            {
                var layoutName =
                    Path.GetFileNameWithoutExtension(layoutPath).
                    Substring("layout-".Length);
                var layout = await ReadLayoutAsync(ripper, new PathEntry(layoutPath), ct).
                    ConfigureAwait(false);
                return (layoutName, layout);
            })).
            ConfigureAwait(false)).
            ToDictionary(entry => entry.layoutName, entry => entry.layout);
        if (layoutList.Count >= 1)
        {
            await output.WriteLineAsync(
                $"Read layouts: {string.Join(", ", layoutList.Keys)}").
                WithCancellation(ct).
                ConfigureAwait(false);
            await output.WriteLineAsync().
                WithCancellation(ct).
                ConfigureAwait(false);
        }

        //////////////////////////////////////////////////////////////

        if (requiredBeforeCleanup && Directory.Exists(storeToBasePath))
        {
            foreach (var path in Directory.EnumerateDirectories(
                storeToBasePath, "*.*", SearchOption.TopDirectoryOnly))
            {
                Directory.Delete(path, true);
            }

            foreach (var path in Directory.EnumerateFiles(
               storeToBasePath, "*.*", SearchOption.TopDirectoryOnly))
            {
                File.Delete(path);
            }

            await output.WriteLineAsync(
                $"Clean up store directory: {storeToBasePath}").
                WithCancellation(ct).
                ConfigureAwait(false);
            await output.WriteLineAsync().
                WithCancellation(ct).
                ConfigureAwait(false);
        }

        //////////////////////////////////////////////////////////////

        var rootMetadata = new MetadataContext();
        MetadataUtilities.SetDefaults(rootMetadata);

        rootMetadata.SetValue("layoutList", layoutList);

        foreach (var kv in metadataList)
        {
            rootMetadata.Set(kv.Key, kv.Value);
        }

        var generator = new BulkRipper(ripper, storeToBasePath);

        var (count, maxConcurrentProcessing) = await generator.RipOffAsync(
            contentsBasePathList,
            (relativeContentsPath, relativeGeneratedPath, contentsBasePath, layoutName) =>
                output.WriteLineAsync($"Generated: {layoutName}: {relativeContentsPath} ==> {relativeGeneratedPath}").
                WithCancellation(ct),
            rootMetadata,
            ct).
            ConfigureAwait(false);
        
        //////////////////////////////////////////////////////////////

        sw.Stop();

        await output.WriteLineAsync().
            WithCancellation(ct).
            ConfigureAwait(false);

        if (count >= 1)
        {
            var perContent = TimeSpan.FromTicks(sw.ElapsedTicks / count);
            await output.WriteLineAsync(
                $"Finished: Contents={count}, Elapsed={sw.Elapsed}, PerContent={perContent}, Concurrent={maxConcurrentProcessing}").
                WithCancellation(ct).
                ConfigureAwait(false);
        }
        else
        {
            await output.WriteLineAsync(
                $"Finished: Contents=0, Elapsed={sw.Elapsed}").
                WithCancellation(ct).
                ConfigureAwait(false);
        }
    }
}
