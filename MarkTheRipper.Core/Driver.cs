/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
    private static async ValueTask<IReadOnlyDictionary<string, object?>> ReadMetadataAsync(
        string path, CancellationToken ct)
    {
        using var rs = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
        using var tr = new StreamReader(
            rs, Encoding.UTF8, true);
        var jr = new JsonTextReader(tr);

        var s = Utilities.GetDefaultJsonSerializer();

        var jt = await JToken.LoadAsync(jr, ct);
        return jt.ToObject<Dictionary<string, object?>>(s) ?? new();
    }

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

        var now = DateTimeOffset.Now;
        var baseMetadata = new Dictionary<string, object?>()
        {
            { "now", now },
            { "lang", CultureInfo.CurrentCulture },
            { "generator", $"MarkTheRipper {ThisAssembly.AssemblyVersion}" },
        };

        var metadataList = await Task.WhenAll(
            Directory.EnumerateFiles(
                resourceBasePath, "metadata*.json", SearchOption.TopDirectoryOnly).
            Select(metadataPath => ReadMetadataAsync(metadataPath, ct).AsTask())).
            ConfigureAwait(false);
        baseMetadata = metadataList.
            SelectMany(entries => entries).
            Concat(baseMetadata).
            DistinctBy(entry => entry.Key).
            ToDictionary(entry => entry.Key, entry => entry.Value);
        if (metadataList.Length >= 1)
        {
            await output.WriteLineAsync(
                $"Read metadata: {baseMetadata.Count} / {metadataList.Length}").
                WithCancellation(ct).
                ConfigureAwait(false);
            await output.WriteLineAsync().
                WithCancellation(ct).
                ConfigureAwait(false);
        }

        var templates = (await Task.WhenAll(
            Directory.EnumerateFiles(
                resourceBasePath, "template-*.html", SearchOption.TopDirectoryOnly).
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

        var generator = new BulkRipper(
            storeToBasePath,
            templateName => templates.TryGetValue(templateName, out var template) ? template : null,
            keyName => baseMetadata.TryGetValue(keyName, out var value) ? value : null);

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
