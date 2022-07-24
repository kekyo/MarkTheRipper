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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper;

/// <summary>
/// Higher level interface for Ripper.
/// </summary>
public static class Driver
{
    /// <summary>
    /// Run Ripper.
    /// </summary>
    /// <param name="output">Log output target</param>
    /// <param name="storeToBasePath">Store to base path</param>
    /// <param name="templateBasePath">Template base path</param>
    /// <param name="baseMetadata">Base metadata</param>
    /// <param name="contentsBasePathList">Markdown content placed directory path iterator</param>
    /// <param name="requiredBeforeCleanup">Before cleanup</param>
    /// <param name="ct">CancellationToken</param>
    public static async ValueTask RunAsync(
        TextWriter output, string storeToBasePath, string templateBasePath,
        IReadOnlyDictionary<string, object?> baseMetadata,
        IEnumerable<string> contentsBasePathList,
        bool requiredBeforeCleanup,
        CancellationToken ct)
    {
        await output.WriteLineAsync(
            $"Contents base path: {string.Join(", ", contentsBasePathList)}").
            WithCancellation(ct).
            ConfigureAwait(false);
        await output.WriteLineAsync(
            $"Template base path: {templateBasePath}").
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

        static async ValueTask<Template> ReadTemplateAsync(
            string path, CancellationToken ct)
        {
            using var rs = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
            using var tr = new StreamReader(
                rs, Encoding.UTF8, true);

            return await Ripper.ParseTemplateAsync(path, tr, ct).
                ConfigureAwait(false);
        }

        var templatePath = Path.Combine(templateBasePath, "page-template.html");
        var template = await ReadTemplateAsync(templatePath, ct).
            ConfigureAwait(false);

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

        var generator = new Ripper(
            storeToBasePath, template, baseMetadata);
        var (count, maxConcurrentProcessing) = await generator.RipOffAsync(
            contentsBasePathList,
            (relativeContentsPath, relativeGeneratedPath, _) =>
                output.WriteLineAsync($"Generated: {relativeContentsPath} ==> {relativeGeneratedPath}").
                WithCancellation(ct),
            ct).
            ConfigureAwait(false);

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
