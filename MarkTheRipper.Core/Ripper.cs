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
using MarkTheRipper.Expressions;
using MarkTheRipper.Functions;
using MarkTheRipper.Internal;
using MarkTheRipper.Metadata;
using MarkTheRipper.Layout;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace MarkTheRipper;

/// <summary>
/// Rip off and generate static site.
/// </summary>
public sealed class Ripper
{
    public ValueTask<RootLayoutNode> ParseLayoutAsync(
        string layoutName,
        TextReader layoutReader,
        CancellationToken ct) =>
        Parser.ParseLayoutAsync(layoutName, layoutReader, ct);

    private static void InjectAdditionalMetadata(
        Dictionary<string, IExpression> markdownMetadata,
        PathEntry markdownPath,
        string? contentBody)
    {
        var storeToPathHint = new PathEntry(
            Path.Combine(
                Path.GetDirectoryName(markdownPath.PhysicalPath) ??
                Path.DirectorySeparatorChar.ToString(),
                Path.GetFileNameWithoutExtension(markdownPath.PhysicalPath) + ".html"));

        markdownMetadata["markdownPath"] = new ValueExpression(markdownPath);
        markdownMetadata["path"] = new ValueExpression(storeToPathHint);

        // Special: Automatic insertion for category when not available.
        if (!markdownMetadata.ContainsKey("category"))
        {
            var relativeDirectoryPath =
                Path.GetDirectoryName(markdownPath.PhysicalPath) ??
                Path.DirectorySeparatorChar.ToString();
            var pathElements = relativeDirectoryPath.
                Split(Utilities.PathSeparators, StringSplitOptions.RemoveEmptyEntries);
            markdownMetadata.Add("category", new ValueExpression(
                pathElements.Aggregate(
                    new PartialCategoryEntry(),
                    (agg, v) => new PartialCategoryEntry(v, agg))));
        }

        if (contentBody != null)
        {
            markdownMetadata["contentBody"] = new ValueExpression(contentBody);
        }
    }

    /// <summary>
    /// Parse markdown markdownEntry.
    /// </summary>
    /// <param name="contentsBasePath">Markdown content path</param>
    /// <param name="markdownPath">Markdown content path</param>
    /// <param name="generatedDate">Generated date</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied layout name.</returns>
    public async ValueTask<MarkdownEntry> ParseMarkdownHeaderAsync(
        string contentsBasePath,
        PathEntry markdownPath,
        DateTimeOffset generatedDate,
        CancellationToken ct)
    {
        var path = Path.Combine(contentsBasePath, markdownPath.PhysicalPath);

        async ValueTask<Dictionary<string, IExpression>> ParseAsync()
        {
            using var markdownStream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                65536,
                true);
            var markdownReader = new StreamReader(
                markdownStream,
                Encoding.UTF8,
                true);

            return await Parser.ParseMarkdownHeaderAsync(
                markdownPath, markdownReader, ct).
                ConfigureAwait(false);
        }

        var metadata = await ParseAsync().
            ConfigureAwait(false);

        if (MarkdownEntry.GetPublishedState(metadata) &&
            !metadata.TryGetValue("date", out var _))
        {
            try
            {
                using var markdownStream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    65536,
                    true);
                var markdownReader = new StreamReader(
                    markdownStream,
                    Encoding.UTF8,
                    true);

                using var markdownOutputStream = new FileStream(
                    path + ".tmp",
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    65536,
                    true);
                var markdownWriter = new StreamWriter(
                    markdownOutputStream,
                    Encoding.UTF8);

                await Parser.ParseAndAppendMarkdownHeaderAsync(
                    markdownReader,
                    new Dictionary<string, string>
                    {
                        { "date", generatedDate.ToString(null, CultureInfo.InvariantCulture) },
                    },
                    markdownWriter,
                    ct);

                await markdownWriter.FlushAsync().
                    ConfigureAwait(false);
            }
            catch
            {
                File.Delete(path + ".tmp");
                throw;
            }

            File.Delete(path);
            File.Move(path + ".tmp", path);
        }

        InjectAdditionalMetadata(metadata, markdownPath, null);

        return new(metadata, contentsBasePath);
    }

    private static async ValueTask<RootLayoutNode> GetLayoutAsync(
        MetadataContext metadata, CancellationToken ct)
    {
        if (metadata.Lookup("layoutList") is { } layoutListExpression &&
            await Reducer.ReduceExpressionAsync(layoutListExpression, metadata, ct).
            ConfigureAwait(false) is IReadOnlyDictionary<string, RootLayoutNode> tl)
        {
            if (metadata.Lookup("layout") is { } layoutExpression &&
                await Reducer.ReduceExpressionAsync(layoutExpression, metadata, ct).
                    ConfigureAwait(false) is { } layoutValue)
            {
                if (layoutValue is RootLayoutNode layout)
                {
                    return layout;
                }
                else if (layoutValue is PartialLayoutEntry entry)
                {
                    if (tl.TryGetValue(entry.Name, out layout!))
                    {
                        return layout;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Layout `{entry.Name}` was not found.");
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Invalid layout object. Value={layoutValue.GetType().Name}");
                }
            }
            else if (tl.TryGetValue("page", out var layout))
            {
                return layout;
            }
            else
            {
                throw new InvalidOperationException(
                    "Layout `page` was not found.");
            }
        }
        else
        {
            throw new InvalidOperationException(
                "Layout list was not found.");
        }
    }

    private static MetadataContext SpawnWithAdditionalMetadata(
        Dictionary<string, IExpression> markdownMetadata,
        MetadataContext parentMetadata,
        PathEntry markdownPath,
        string? contentBody)
    {
        var mc = parentMetadata.Spawn();

        mc.SetValue("relative", FunctionFactory.CreateAsyncFunction(Relative.RelativeAsync));
        mc.SetValue("lookup", FunctionFactory.CreateAsyncFunction(Lookup.LookupAsync));
        mc.SetValue("format", FunctionFactory.CreateAsyncFunction(Format.FormatAsync));

        InjectAdditionalMetadata(
            markdownMetadata,
            markdownPath,
            contentBody);
        foreach (var kv in markdownMetadata)
        {
            mc.Set(kv.Key, kv.Value);
        }

        return mc;
    }

    /// <summary>
    /// Render markdown content.
    /// </summary>
    /// <param name="markdownPath">Markdown content path</param>
    /// <param name="markdownReader">Markdown content reader</param>
    /// <param name="metadata">Metadata context</param>
    /// <param name="outputHtmlWriter">Generated html content writer</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied layout name.</returns>
    public async ValueTask<string> RenderContentAsync(
        PathEntry markdownPath,
        TextReader markdownReader,
        MetadataContext metadata,
        TextWriter outputHtmlWriter,
        CancellationToken ct)
    {
        var (markdownMetadata, markdownBody) = await Parser.ParseMarkdownBodyAsync(
            markdownPath, markdownReader, ct).
            ConfigureAwait(false);

        var markdownDocument = MarkdownParser.Parse(markdownBody);

        var contentBody = new StringBuilder();
        var renderer = new HtmlRenderer(new StringWriter(contentBody));
        renderer.Render(markdownDocument);

        var layout = await GetLayoutAsync(metadata, ct).
            ConfigureAwait(false);

        var mc = SpawnWithAdditionalMetadata(
            markdownMetadata,
            metadata,
            markdownPath,
            contentBody.ToString());

        await layout.RenderAsync(
            (text, ct) => outputHtmlWriter.WriteAsync(text).WithCancellation(ct),
            mc,
            ct).
            ConfigureAwait(false);

        return layout.Name;
    }

    /// <summary>
    /// Render markdown content.
    /// </summary>
    /// <param name="markdownEntry">Markdown entry</param>
    /// <param name="metadata">Metadata context</param>
    /// <param name="storeToBasePath">Generated html content base path</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied layout name.</returns>
    public async ValueTask<string> RenderContentAsync(
        MarkdownEntry markdownEntry,
        MetadataContext metadata,
        string storeToBasePath,
        CancellationToken ct)
    {
        using var markdownStream = new FileStream(
            Path.Combine(
                markdownEntry.contentBasePath,
                markdownEntry.MarkdownPath.PhysicalPath),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            65536,
            true);
        var markdownReader = new StreamReader(
            markdownStream,
            Encoding.UTF8,
            true);

        using var outputHtmlStream = new FileStream(
            Path.Combine(
                storeToBasePath,
                markdownEntry.StoreToPath.PhysicalPath),
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            65536,
            true);
        var outputHtmlWriter = new StreamWriter(
            outputHtmlStream,
            Encoding.UTF8);

        var appliedLayoutName = await this.RenderContentAsync(
            markdownEntry.MarkdownPath,
            markdownReader,
            metadata,
            outputHtmlWriter,
            ct).
            ConfigureAwait(false);

        await outputHtmlWriter.FlushAsync().
            ConfigureAwait(false);

        return appliedLayoutName;
    }
}
