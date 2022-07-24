/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper;

/// <summary>
/// Higher level interface for Ripper.
/// </summary>
public static class Driver
{
    private static async ValueTask<RootTemplateNode> ReadTemplateAsync(
        string path, CancellationToken ct)
    {
        using var rs = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
        using var tr = new StreamReader(
            rs, Encoding.UTF8, true);

        return await Ripper.ParseTemplateAsync(path, tr, ct).
            ConfigureAwait(false);
    }

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
        TextWriter output, string storeToBasePath, string resourceBasePath,
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

        var utcnow = DateTimeOffset.UtcNow;
        var baseMetadata = new Dictionary<string, object?>(
            StringComparer.OrdinalIgnoreCase)
            {
                { "utcnow", utcnow },
            };

        var templates = (await Task.WhenAll(
            Directory.EnumerateFiles(
                resourceBasePath, "template-*.html", SearchOption.AllDirectories).
            Select(async templatePath =>
            {
                var templateName =
                    Path.GetFileNameWithoutExtension(templatePath).
                    Substring("template-".Length);
                var template = await ReadTemplateAsync(templatePath, ct).
                    ConfigureAwait(false);
                return (templateName, template);
            })).
            ConfigureAwait(false)).
            ToDictionary(entry => entry.templateName, entry => entry.template);
        if (templates.Count >= 1)
        {
            await output.WriteLineAsync(
                $"Read templates: {string.Join(", ", templates.Keys)}").
                WithCancellation(ct).
                ConfigureAwait(false);
            await output.WriteLineAsync().
                WithCancellation(ct).
                ConfigureAwait(false);
        }

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
            storeToBasePath,
            templates,
            baseMetadata);

        var (count, maxConcurrentProcessing) = await generator.RipOffAsync(
            contentsBasePathList,
            (relativeContentsPath, relativeGeneratedPath, contentsBasePath, templateName) =>
                output.WriteLineAsync($"Generated: {templateName}: {relativeContentsPath} ==> {relativeGeneratedPath}").
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
