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
using MarkTheRipper.Internal;
using MarkTheRipper.Metadata;
using MarkTheRipper.TextTreeNodes;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    public ValueTask<RootTextNode> ParseLayoutAsync(
        PathEntry layoutPathHint,
        TextReader layoutReader,
        CancellationToken ct) =>
        Parser.ParseTextTreeAsync(
            layoutPathHint,
            ct => layoutReader.ReadLineAsync().WithCancellation(ct),
            (_, _) => false,
            ct);

    private static void InjectAdditionalMetadata(
        Dictionary<string, IExpression> markdownMetadata,
        PathEntry markdownPath)
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
                Utilities.UTF8,
                true);

            return await Parser.ParseMarkdownHeaderAsync(
                markdownPath,
                ct => markdownReader.ReadLineAsync().WithCancellation(ct),
                ct).
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
                    Utilities.UTF8,
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
                    Utilities.UTF8);
                markdownWriter.NewLine = Environment.NewLine;

                await Parser.ParseAndAppendMarkdownHeaderAsync(
                    ct => markdownReader.ReadLineAsync().WithCancellation(ct),
                    new Dictionary<string, string>
                    {
                        { "date", generatedDate.ToString(null, CultureInfo.InvariantCulture) },
                    },
                    (text, ct) => markdownWriter.WriteLineAsync(text).WithCancellation(ct),
                    ct);

                await markdownWriter.FlushAsync().
                    WithCancellation(ct).
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

        InjectAdditionalMetadata(metadata, markdownPath);

        return new(metadata, contentsBasePath);
    }

    private static MetadataContext SpawnWithAdditionalMetadata(
        Dictionary<string, IExpression> markdownMetadata,
        MetadataContext parentMetadata,
        PathEntry markdownPath)
    {
        var mc = parentMetadata.Spawn();

        InjectAdditionalMetadata(markdownMetadata, markdownPath);

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
    /// <returns>Applied layout path.</returns>
    public async ValueTask<PathEntry> RenderContentAsync(
        PathEntry markdownPath,
        TextReader markdownReader,
        MetadataContext metadata,
        TextWriter outputHtmlWriter,
        CancellationToken ct)
    {
        // Step 1: Parse markdown metadata and code fragment locations.
        var (markdownMetadata, markdownBody, inCodeFragments) =
            await Parser.ParseMarkdownBodyAsync(
                markdownPath,
                ct => markdownReader.ReadLineAsync().WithCancellation(ct),
                ct).
                ConfigureAwait(false);

        // Step 2: Parse markdown body to AST (ITextTreeNode).
        var tr = new StringReader(markdownBody);
        var markdownBodyTree = await Parser.ParseTextTreeAsync(
            markdownPath,
            _ => new ValueTask<string?>(tr.ReadLine()),
            (l, c) => inCodeFragments.Any(icf => icf(l, c)),
            ct);

        // Step 3: Spawn new metadata context derived parent.
        var mc = SpawnWithAdditionalMetadata(
            markdownMetadata,
            metadata,
            markdownPath);

        // Step 4: Render markdown with looking up metadata context.
        var renderedMarkdownBody = await markdownBodyTree.RenderOverallAsync(
            mc, ct).
            ConfigureAwait(false);

        // Step 5: Parse renderred markdown to AST (MarkDig)
        var markdownDocument = MarkdownParser.Parse(
            renderedMarkdownBody);

        // Step 6: Render HTML from AST.
        var contentBodyWriter = new StringWriter();
        var renderer = new HtmlRenderer(contentBodyWriter);
        contentBodyWriter.NewLine = Environment.NewLine;  // HACK: MarkDig will update NewLine signature...
        renderer.Render(markdownDocument);

        // Step 7: Set renderred HTML into metadata context.
        mc.Set("contentBody",
            new HtmlContentExpression(contentBodyWriter.ToString()));

        // Step 8: Get layout AST (ITextTreeNode).
        var layoutNode = await mc.GetLayoutAsync(ct).
            ConfigureAwait(false);

        // Step 9: Render markdown from layout AST with overall metadata.
        var overallHtmlContent = await layoutNode.RenderOverallAsync(
            mc, ct).
            ConfigureAwait(false);

        // Step 10: Final output.
        await outputHtmlWriter.WriteAsync(overallHtmlContent.ToString()).
            WithCancellation(ct).
            ConfigureAwait(false);

        return layoutNode.Path;
    }

    /// <summary>
    /// Render markdown content.
    /// </summary>
    /// <param name="markdownEntry">Markdown entry</param>
    /// <param name="metadata">Metadata context</param>
    /// <param name="storeToBasePath">Generated html content base path</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied layout path.</returns>
    public async ValueTask<PathEntry> RenderContentAsync(
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
            Utilities.UTF8,
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
            Utilities.UTF8);
        outputHtmlWriter.NewLine = Environment.NewLine;

        var appliedLayoutPath = await this.RenderContentAsync(
            markdownEntry.MarkdownPath,
            markdownReader,
            metadata,
            outputHtmlWriter,
            ct).
            ConfigureAwait(false);

        await outputHtmlWriter.FlushAsync().
            ConfigureAwait(false);

        return appliedLayoutPath;
    }
}
