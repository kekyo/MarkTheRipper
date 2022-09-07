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
using MarkTheRipper.TextTreeNodes;
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
    public ValueTask<RootTextNode> ParseLayoutAsync(
        PathEntry layoutPathHint,
        TextReader layoutReader,
        CancellationToken ct) =>
        Parser.ParseTextTreeAsync(layoutPathHint, layoutReader, (_, _) => false, ct);

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

        InjectAdditionalMetadata(metadata, markdownPath);

        return new(metadata, contentsBasePath);
    }

    private static async ValueTask<RootTextNode> GetLayoutAsync(
        MetadataContext metadata, CancellationToken ct)
    {
        if (metadata.Lookup("layoutList") is { } layoutListExpression &&
            await Reducer.ReduceExpressionAsync(layoutListExpression, metadata, ct).
            ConfigureAwait(false) is IReadOnlyDictionary<string, RootTextNode> tl)
        {
            if (metadata.Lookup("layout") is { } layoutExpression &&
                await Reducer.ReduceExpressionAsync(layoutExpression, metadata, ct).
                    ConfigureAwait(false) is { } layoutValue)
            {
                if (layoutValue is RootTextNode layout)
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
        PathEntry markdownPath)
    {
        var mc = parentMetadata.Spawn();

        mc.SetValue("relative", FunctionFactory.CastTo(Relative.RelativeAsync));
        mc.SetValue("lookup", FunctionFactory.CastTo(Lookup.LookupAsync));
        mc.SetValue("format", FunctionFactory.CastTo(Format.FormatAsync));
        mc.SetValue("add", FunctionFactory.CastTo(Formula.AddAsync));
        mc.SetValue("sub", FunctionFactory.CastTo(Formula.SubtractAsync));
        mc.SetValue("mul", FunctionFactory.CastTo(Formula.MultipleAsync));
        mc.SetValue("div", FunctionFactory.CastTo(Formula.DivideAsync));
        mc.SetValue("mod", FunctionFactory.CastTo(Formula.ModuloAsync));

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
                markdownPath, markdownReader, ct).
                ConfigureAwait(false);

        // Step 2: Parse markdown body to AST (ITextTreeNode).
        var markdownBodyTree = await Parser.ParseTextTreeAsync(
            markdownPath,
            new StringReader(markdownBody),
            (l, c) => inCodeFragments.Any(icf => icf(l, c)),
            ct);

        // Step 3: Spawn new metadata context derived parent.
        var mc = SpawnWithAdditionalMetadata(
            markdownMetadata,
            metadata,
            markdownPath);

        // Step 4: Render markdown with looking up metadata context.
        var renderedMarkdownBodyWriter = new StringWriter();
        renderedMarkdownBodyWriter.NewLine = Environment.NewLine;
        await markdownBodyTree.RenderAsync(
            (text, _) => { renderedMarkdownBodyWriter.Write(text); return default; }, mc, ct).
            ConfigureAwait(false);

        // Step 5: Parse renderred markdown to AST (MarkDig)
        var markdownDocument = MarkdownParser.Parse(
            renderedMarkdownBodyWriter.ToString());

        // Step 6: Render HTML from AST.
        var contentBodyWriter = new StringWriter();
        var renderer = new HtmlRenderer(contentBodyWriter);
        contentBodyWriter.NewLine = Environment.NewLine;  // HACK: MarkDig will update NewLine signature...
        renderer.Render(markdownDocument);

        // Step 7: Set renderred HTML into metadata context.
        mc.SetValue("contentBody", contentBodyWriter.ToString());

        // Step 8: Get layout AST (ITextTreeNode).
        var layoutNode = await GetLayoutAsync(metadata, ct).
            ConfigureAwait(false);

        // Step 9: Setup HTML content dictionary (will be added by HtmlContentExpression)
        var htmlContents = new Dictionary<string, string>();
        mc.SetValue("htmlContents", htmlContents);

        // Step 10: Render markdown from layout AST with overall metadata.
        var overallHtmlContent = new StringBuilder();
        var overallHtmlContentWriter = new StringWriter(overallHtmlContent);
        overallHtmlContentWriter.NewLine = Environment.NewLine;
        await layoutNode.RenderAsync(
            (text, _) => { overallHtmlContentWriter.Write(text); return default; }, mc, ct).
            ConfigureAwait(false);
        await overallHtmlContentWriter.FlushAsync().
            WithCancellation(ct).
            ConfigureAwait(false);

        // Step 11: Replace all contains if required.
        foreach (var entry in htmlContents)
        {
            overallHtmlContent.Replace(entry.Key, entry.Value);
        }

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
