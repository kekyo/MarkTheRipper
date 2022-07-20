/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Markdig.Parsers;
using Markdig.Renderers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper;

/// <summary>
/// Rip off and generate static site.
/// </summary>
public sealed class Ripper
{
    private readonly struct MarkdownContent
    {
        public readonly string Body;
        public readonly IReadOnlyDictionary<string, string> Metadata;

        public MarkdownContent(IReadOnlyDictionary<string, string> metadata, string body)
        {
            this.Metadata = metadata;
            this.Body = body;
        }
    }

    private readonly string storeToBasePath;
    private readonly string template;
    private readonly IReadOnlyDictionary<string, string> baseMetadata;

    public Ripper(string storeToBasePath, string template,
        IReadOnlyDictionary<string, string> baseMetadata)
    {
        this.storeToBasePath = Path.GetFullPath(storeToBasePath);
        this.template = template;
        this.baseMetadata = baseMetadata;
    }

    private static async ValueTask<MarkdownContent> LoadMarkdownContentAsync(
        TextReader tr,
        IReadOnlyDictionary<string, string> baseMetadata,
        CancellationToken ct)
    {
        // `---`
        while (true)
        {
            var line = await tr.ReadLineAsync().
                WaitAsync(ct).
                ConfigureAwait(false);
            if (line == null)
            {
                throw new FormatException();
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                if (line.Trim().StartsWith("---"))
                {
                    break;
                }
            }
        }

        var metadata = baseMetadata.ToDictionary(
            kv => kv.Key, kv => kv.Value,
            StringComparer.OrdinalIgnoreCase);

        // `title: Hello world`
        while (true)
        {
            var line = await tr.ReadLineAsync().
                WaitAsync(ct).
                ConfigureAwait(false);
            if (line == null)
            {
                throw new FormatException();
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                var keyIndex = line.IndexOf(':');
                if (keyIndex >= 1)
                {
                    var key = line.Substring(0, keyIndex).Trim();
                    var value = line.Substring(keyIndex + 1).Trim();

                    // `title: "Hello world"`
                    if (value.StartsWith("\""))
                    {
                        value = value.Trim('"');
                    }
                    // `title: 'Hello world'`
                    else if (value.StartsWith("'"))
                    {
                        value = value.Trim('\'');
                    }
                    // `title: [Hello world]`
                    // * Assume JavaScript array-like: `tags: [aaa, bbb]` --> `tags: aaa, bbb`
                    else if (value.StartsWith("["))
                    {
                        value = value.TrimStart('[').TrimEnd(']');
                    }

                    metadata[key] = value;
                }
                else
                {
                    // `---`
                    if (line.Trim().StartsWith("---"))
                    {
                        break;
                    }
                    else
                    {
                        throw new FormatException();
                    }
                }
            }
        }

        var sb = new StringBuilder();
        while (true)
        {
            var line = await tr.ReadLineAsync().
                WaitAsync(ct).
                ConfigureAwait(false);
            if (line == null)
            {
                break;
            }

            // Skip empty lines, will detect start of body
            if (!string.IsNullOrWhiteSpace(line))
            {
                sb.AppendLine(line);
                break;
            }
        }

        while (true)
        {
            var line = await tr.ReadLineAsync().
                WaitAsync(ct).
                ConfigureAwait(false);

            // EOF
            if (line == null)
            {
                break;
            }

            // (Sanitizing EOL)
            sb.AppendLine(line);
        }

        return new(metadata, sb.ToString());
    }

    public static async ValueTask<string> RipOffContentAsync(
        TextReader markdown, string template,
        IReadOnlyDictionary<string, string> baseMetadata,
        CancellationToken ct)
    {
        var markdownContent = await LoadMarkdownContentAsync(
            markdown, baseMetadata, ct).
            ConfigureAwait(false);

        var markdownDocument = MarkdownParser.Parse(markdownContent.Body);

        var body = new StringBuilder();
        var renderer = new HtmlRenderer(new StringWriter(body));
        renderer.Render(markdownDocument);

        var capacityHint =
            template.Length +
            body.Length +
            markdownContent.Metadata.Sum(kv => kv.Value.Length - kv.Key.Length) +
            100;
        var html = new StringBuilder(template, capacityHint);
        foreach (var kv in markdownContent.Metadata)
        {
            html.Replace($"{{{kv.Key}}}", kv.Value);
        }
        html.Replace("{body}", body.ToString());

        return html.ToString();
    }

    public static ValueTask<string> RipOffContentAsync(
        string markdown, string template,
        IReadOnlyDictionary<string, string> baseMetadata,
        CancellationToken ct) =>
        RipOffContentAsync(
            new StringReader(markdown), template, baseMetadata, ct);

    public static async ValueTask RipOffContentAsync(
        TextReader markdown, string template,
        TextWriter html,
        IReadOnlyDictionary<string, string> baseMetadata,
        CancellationToken ct)
    {
        using var hw = new StringWriter();

        var htmlContent = await RipOffContentAsync(
            markdown, template, baseMetadata, ct).
            ConfigureAwait(false);

        await html.WriteAsync(htmlContent).
            ConfigureAwait(false);
    }

    public static async ValueTask RipOffContentAsync(
        string markdownPath, string template,
        string outputHtmlPath,
        IReadOnlyDictionary<string, string> baseMetadata,
        CancellationToken ct)
    {
        using var ms = new FileStream(
            markdownPath,
            FileMode.Open, FileAccess.Read, FileShare.Read,
            65536, true);
        using var tr = new StreamReader(
            ms, Encoding.UTF8, true);

        using var hs = new FileStream(
            outputHtmlPath,
            FileMode.Create, FileAccess.ReadWrite, FileShare.None,
            65536, true);
        using var tw = new StreamWriter(
            hs, Encoding.UTF8);

        await RipOffContentAsync(tr, template, tw, baseMetadata, ct).
            ConfigureAwait(false);

        await tw.FlushAsync().
            ConfigureAwait(false);
    }

    private ValueTask RipOffRelativeContentAsync(
        string relativeContentPath, string contentsBasePath, CancellationToken ct)
    {
        var contentPath = Path.Combine(contentsBasePath, relativeContentPath);
        var storeToRelativeBasePath = Path.GetDirectoryName(
            Path.Combine(this.storeToBasePath, relativeContentPath))!;
        var storeToFileName = Path.GetFileNameWithoutExtension(relativeContentPath);
        var storeToPath = Path.Combine(storeToRelativeBasePath, storeToFileName + ".html");

        return RipOffContentAsync(
            contentPath, this.template, storeToPath, this.baseMetadata, ct);
    }

    private async ValueTask CopyRelativeContentAsync(
        string relativeContentPath, string contentsBasePath, CancellationToken ct)
    {
        var contentPath = Path.Combine(contentsBasePath, relativeContentPath);
        var storeToPath = Path.Combine(this.storeToBasePath, relativeContentPath);

        using var cs = new FileStream(contentPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
        using var ss = new FileStream(storeToPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 65536, true);

        var buffer = new byte[65536];
        while (true)
        {
            var read = await cs.ReadAsync(buffer, 0, buffer.Length, ct).
                ConfigureAwait(false);
            if (read <= 0)
            {
                break;
            }

            await ss.WriteAsync(buffer, 0, read, ct).
                ConfigureAwait(false);
        }

        await ss.FlushAsync(ct).
            ConfigureAwait(false);
    }

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="contentsBasePathList">Markdown content placed directory path list</param>
    public ValueTask<int> RipOffAsync(
        params string[] contentsBasePathList) =>
        this.RipOffAsync(contentsBasePathList, (_, _) => default, default);

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="generated">Generated callback</param>
    /// <param name="contentsBasePathList">Markdown content placed directory path list</param>
    public ValueTask<int> RipOffAsync(
        Func<string, string, ValueTask> generated,
        params string[] contentsBasePathList) =>
        this.RipOffAsync(contentsBasePathList, generated, default);

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="generated">Generated callback</param>
    /// <param name="ct">CancellationToken</param>
    /// <param name="contentsBasePathList">Markdown content placed directory path list</param>
    public ValueTask<int> RipOffAsync(
        Func<string, string, ValueTask> generated,
        CancellationToken ct, params string[] contentsBasePathList) =>
        this.RipOffAsync(contentsBasePathList, generated, ct);

    /// <summary>
    /// Rip off and generate from Markdown contents.
    /// </summary>
    /// <param name="contentsBasePathList">Markdown content placed directory path iterator</param>
    /// <param name="generated">Generated callback</param>
    /// <param name="ct">CancellationToken</param>
    public async ValueTask<int> RipOffAsync(
        IEnumerable<string> contentsBasePathList,
        Func<string, string, ValueTask> generated,
        CancellationToken ct)
    {
        var dc = new DirectoryCreator();

        var candidates = contentsBasePathList.
            Where(contentsBasePath => Directory.Exists(contentsBasePath)).
            SelectMany(contentsBasePath => Directory.EnumerateFiles(
                contentsBasePath, "*.*", SearchOption.AllDirectories).
                Select(path => (contentsBasePath, path)));

        async Task RunOnceAsync(string contentsBasePath, string contentsPath)
        {
            var relativeContentPath =
                contentsPath.Substring(0, contentsBasePath.Length + 1);

            var storeToPath = Path.Combine(this.storeToBasePath, relativeContentPath);
            var storeToDirPath = Path.GetDirectoryName(storeToPath)!;

            await dc!.CreateIfNotExistAsync(storeToDirPath, ct).
                ConfigureAwait(false);

            if (Path.GetExtension(relativeContentPath) == ".md")
            {
                await this.RipOffRelativeContentAsync(
                    relativeContentPath, contentsBasePath, ct).
                    ConfigureAwait(false);
            }
            else
            {
                await this.CopyRelativeContentAsync(
                    relativeContentPath, contentsBasePath, ct).
                    ConfigureAwait(false);
            }

            await generated(relativeContentPath, contentsBasePath).
                ConfigureAwait(false);
        }

        var count = 0;
#if DEBUG
        foreach (var candidate in candidates)
        {
            count++;
            await RunOnceAsync(candidate.contentsBasePath, candidate.path).
                ConfigureAwait(false);
        }
#else
        await Task.WhenAll(candidates.
            Select(candidate => { count++; return RunOnceAsync(candidate.contentsBasePath, candidate.path); })).
            ConfigureAwait(false);
#endif
        return count;
    }
}
