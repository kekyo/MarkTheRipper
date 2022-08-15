/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using MarkTheRipper.Metadata;
using MarkTheRipper.Template;
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
        string path,
        CancellationToken ct)
    {
        using var rs = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            65536,
            true);
        using var tr = new StreamReader(
            rs,
            Encoding.UTF8,
            true);
        var jr = new JsonTextReader(tr);

        var s = Utilities.GetDefaultJsonSerializer();

        var jt = await JToken.LoadAsync(jr, ct);
        return jt.ToObject<Dictionary<string, object?>>(s) ?? new();
    }

    private static async ValueTask<RootTemplateNode> ReadTemplateAsync(
        string templatePath,
        string templateName,
        CancellationToken ct)
    {
        using var rs = new FileStream(
            templatePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            65536,
            true);
        using var tr = new StreamReader(
            rs,
            Encoding.UTF8,
            true);

        return await Ripper.ParseTemplateAsync(
            templateName,
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
            Select(metadataPath => ReadMetadataAsync(metadataPath, ct).AsTask())).
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

        var templateList = (await Task.WhenAll(
            Directory.EnumerateFiles(
                resourceBasePath, "template-*.html", SearchOption.TopDirectoryOnly).
            Select(async templatePath =>
            {
                var templateName =
                    Path.GetFileNameWithoutExtension(templatePath).
                    Substring("template-".Length);
                var template = await ReadTemplateAsync(templatePath, templateName, ct).
                    ConfigureAwait(false);
                return (templateName, template);
            })).
            ConfigureAwait(false)).
            ToDictionary(entry => entry.templateName, entry => entry.template);
        if (templateList.Count >= 1)
        {
            await output.WriteLineAsync(
                $"Read templates: {string.Join(", ", templateList.Keys)}").
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

        rootMetadata.Set("generated", DateTimeOffset.Now);
        rootMetadata.Set("lang", CultureInfo.CurrentCulture);
        rootMetadata.Set("generator", $"MarkTheRipper {ThisAssembly.AssemblyVersion}");
        rootMetadata.Set("templateList", templateList);
        rootMetadata.Set("template", "page");

        foreach (var kv in metadataList)
        {
            rootMetadata.Set(kv.Key, kv.Value);
        }

        var generator = new BulkRipper(storeToBasePath);

        var (count, maxConcurrentProcessing) = await generator.RipOffAsync(
            contentsBasePathList,
            (relativeContentsPath, relativeGeneratedPath, contentsBasePath, templateName) =>
                output.WriteLineAsync($"Generated: {templateName}: {relativeContentsPath} ==> {relativeGeneratedPath}").
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
