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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper;

/// <summary>
/// Bulk rip off and generate static site.
/// </summary>
public sealed class BulkRipper
{
    private readonly string storeToBasePath;
    private readonly Ripper ripper;

    public BulkRipper(
        string storeToBasePath,
        IReadOnlyDictionary<string, RootTemplateNode> templates,
        IReadOnlyDictionary<string, object?> baseMetadata)
    {
        this.storeToBasePath = Path.GetFullPath(storeToBasePath);
        this.ripper = new Ripper(templates, baseMetadata);
    }

    private async ValueTask<(string storeToRelativePath, string templateName)> RipOffRelativeContentAsync(
        string relativeContentPath,
        string contentsBasePath,
        CancellationToken ct)
    {
        var contentPath = Path.Combine(contentsBasePath, relativeContentPath);
        var storeToBasePath = Path.GetDirectoryName(
            Path.Combine(this.storeToBasePath, relativeContentPath))!;
        var storeToFileName = Path.GetFileNameWithoutExtension(relativeContentPath);
        var storeToPath = Path.Combine(storeToBasePath, storeToFileName + ".html");
        var storeToRelativePath = storeToPath.Substring(this.storeToBasePath.Length + 1);

        var templateName = await this.ripper.RipOffContentAsync(
            contentPath, storeToPath, ct).
            ConfigureAwait(false);

        return (storeToRelativePath, templateName);
    }

    /// <summary>
    /// Copy content into target path.
    /// </summary>
    /// <param name="contentStream">Content stream</param>
    /// <param name="storeToPath">Store to path</param>
    /// <param name="ct">CancellationToken</param>
    public static async ValueTask CopyContentToAsync(
        Stream contentStream,
        string storeToPath,
        CancellationToken ct)
    {
        using var storeToStream = new FileStream(
            storeToPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 65536, true);

        var buffer = new byte[65536];
        while (true)
        {
            var read = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct).
                ConfigureAwait(false);
            if (read <= 0)
            {
                break;
            }

            await storeToStream.WriteAsync(buffer, 0, read, ct).
                ConfigureAwait(false);
        }

        await storeToStream.FlushAsync(ct).
            ConfigureAwait(false);
    }

    /// <summary>
    /// Copy content into target path.
    /// </summary>
    /// <param name="relativeContentPath">Relative content path</param>
    /// <param name="contentsBasePath">Content base path</param>
    /// <param name="storeToBasePath">Store to path</param>
    /// <param name="ct">CancellationToken</param>
    public static async ValueTask CopyRelativeContentAsync(
        string relativeContentPath,
        string contentsBasePath,
        string storeToBasePath,
        CancellationToken ct)
    {
        var contentPath = Path.Combine(
            contentsBasePath, relativeContentPath);
        var storeToPath = Path.Combine(
            storeToBasePath, relativeContentPath);

        using var cs = new FileStream(
            contentPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);

        await CopyContentToAsync(cs, storeToPath, ct).
            ConfigureAwait(false);
    }

    private ValueTask CopyRelativeContentAsync(
        string relativeContentPath,
        string contentsBasePath,
        CancellationToken ct) =>
        CopyRelativeContentAsync(
            relativeContentPath, contentsBasePath, this.storeToBasePath, ct);

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="contentsBasePathList">Markdown content placed directory path list</param>
    /// <remarks>Coverage</remarks>
    public ValueTask<(int count, int maxConcurrentProcessing)> RipOffAsync(
        params string[] contentsBasePathList) =>
        this.RipOffAsync(contentsBasePathList, (_, _, _, _) => default, default);

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="generated">Generated callback</param>
    /// <param name="contentsBasePathList">Markdown content placed directory path list</param>
    /// <remarks>Coverage</remarks>
    public ValueTask<(int count, int maxConcurrentProcessing)> RipOffAsync(
        Func<string, string, string, string, ValueTask> generated,
        params string[] contentsBasePathList) =>
        this.RipOffAsync(contentsBasePathList, generated, default);

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="generated">Generated callback</param>
    /// <param name="ct">CancellationToken</param>
    /// <param name="contentsBasePathList">Markdown content placed directory path list</param>
    /// <remarks>Coverage</remarks>
    public ValueTask<(int count, int maxConcurrentProcessing)> RipOffAsync(
        Func<string, string, string, string, ValueTask> generated,
        CancellationToken ct, params string[] contentsBasePathList) =>
        this.RipOffAsync(contentsBasePathList, generated, ct);

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="contentsBasePathList">Markdown content placed directory path iterator</param>
    /// <param name="generated">Generated callback</param>
    /// <param name="ct">CancellationToken</param>
    /// <remarks>Coverage</remarks>
    public async ValueTask<(int count, int maxConcurrentProcessing)> RipOffAsync(
        IEnumerable<string> contentsBasePathList,
        Func<string, string, string, string, ValueTask> generated,
        CancellationToken ct)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var dc = new SafeDirectoryCreator();

        var candidates = contentsBasePathList.
            Select(contentsBasePath => Path.GetFullPath(contentsBasePath)).
            Where(contentsBasePath => Directory.Exists(contentsBasePath)).
            SelectMany(contentsBasePath => Directory.EnumerateFiles(
                contentsBasePath, "*.*", SearchOption.AllDirectories).
                Select(path => (contentsBasePath, path)));

        async ValueTask RunOnceAsync(string contentsBasePath, string contentsPath)
        {
            var relativeContentPath = contentsPath.Substring(
                contentsBasePath.Length +
                (contentsBasePath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? 0 : 1));

            var storeToPath = Path.Combine(this.storeToBasePath, relativeContentPath);
            var storeToDirPath = Path.GetDirectoryName(storeToPath)!;

            await dc!.CreateIfNotExistAsync(storeToDirPath, ct).
                ConfigureAwait(false);

            if (Path.GetExtension(relativeContentPath) == ".md")
            {
                var (relativeGeneratedPath, templateName) =
                    await this.RipOffRelativeContentAsync(
                        relativeContentPath,
                        contentsBasePath,
                        ct).
                        ConfigureAwait(false);

                await generated(
                    relativeContentPath,
                    relativeGeneratedPath,
                    contentsBasePath,
                    templateName).
                    ConfigureAwait(false);
            }
            else
            {
                await this.CopyRelativeContentAsync(
                    relativeContentPath, contentsBasePath, ct).
                    ConfigureAwait(false);
            }
        }

        var count = 0;
        var concurrentProcessing = 0;
        var maxConcurrentProcessing = 0;
        async Task RunOnceWithMeasurementAsync(string contentsBasePath, string contentsPath)
        {
            count++;
            var cp = Interlocked.Increment(ref concurrentProcessing);
            maxConcurrentProcessing = Math.Max(maxConcurrentProcessing, cp);

            try
            {
                await RunOnceAsync(contentsBasePath, contentsPath).
                    ConfigureAwait(false);
            }
            catch
            {
                Interlocked.Decrement(ref concurrentProcessing);
                throw;
            }

            Interlocked.Decrement(ref concurrentProcessing);
        }

#if DEBUG
        foreach (var candidate in candidates)
        {
            await RunOnceWithMeasurementAsync(candidate.contentsBasePath, candidate.path).
                ConfigureAwait(false);
        }
#else
        await Task.WhenAll(candidates.
            Select(candidate => RunOnceWithMeasurementAsync(candidate.contentsBasePath, candidate.path))).
            ConfigureAwait(false);
#endif

        return (count, maxConcurrentProcessing);
    }
}
