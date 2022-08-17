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
using MarkTheRipper.Internal;
using MarkTheRipper.Metadata;
using MarkTheRipper.Template;
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
    public static ValueTask<RootTemplateNode> ParseTemplateAsync(
        string templateName, TextReader template, CancellationToken ct) =>
        Parser.ParseTemplateAsync(templateName, template, ct);

    public static ValueTask<RootTemplateNode> ParseTemplateAsync(
        string templateName, string templateText, CancellationToken ct) =>
        Parser.ParseTemplateAsync(templateName, new StringReader(templateText), ct);

    /// <summary>
    /// Parse markdown markdownEntry.
    /// </summary>
    /// <param name="contentBasePathHint">Markdown content base path (Hint)</param>
    /// <param name="relativeContentPath">Markdown content path</param>
    /// <param name="markdownReader">Markdown content reader</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied template name.</returns>
    public async ValueTask<MarkdownEntry> ParseMarkdownHeaderAsync(
        string contentBasePathHint,
        PathEntry relativeContentPath,
        TextReader markdownReader,
        CancellationToken ct)
    {
        var metadata = await Parser.ParseMarkdownHeaderAsync(
            relativeContentPath, markdownReader, ct).
            ConfigureAwait(false);

        return new(metadata, contentBasePathHint);
    }

    /// <summary>
    /// Parse markdown markdownEntry.
    /// </summary>
    /// <param name="contentsBasePath">Markdown content path</param>
    /// <param name="relativeContentPath">Markdown content path</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied template name.</returns>
    public async ValueTask<MarkdownEntry> ParseMarkdownHeaderAsync(
        string contentsBasePath,
        PathEntry relativeContentPath,
        CancellationToken ct)
    {
        using var markdownStream = new FileStream(
            Path.Combine(contentsBasePath, relativeContentPath.RealPath),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            65536,
            true);
        var markdownReader = new StreamReader(
            markdownStream,
            Encoding.UTF8,
            true);

        return await this.ParseMarkdownHeaderAsync(
            contentsBasePath,
            relativeContentPath,
            markdownReader,
            ct).
            ConfigureAwait(false);
    }

    private static MetadataContext InjectMetadata(
        MetadataContext parentMetadata,
        PathEntry relativeContentPathHint,
        string contentBody,
        IReadOnlyDictionary<string, object?> markdownMetadata)
    {
        var mc = parentMetadata.Spawn();
        mc.Set("contentBody", contentBody);

        // HACK: Relative path calculation in PathEntry needs this metadata.
        mc.Set("currentContentPath", relativeContentPathHint);

        // Special: category
        var relativeDirectoryPath =
            Path.GetDirectoryName(relativeContentPathHint.RealPath) ??
            Path.DirectorySeparatorChar.ToString();
        var pathElements = relativeDirectoryPath.
            Split(Utilities.PathSeparators, StringSplitOptions.RemoveEmptyEntries);
        mc.Set("category", pathElements.
            Aggregate(
                new PartialCategoryEntry(),
                (agg, v) => new PartialCategoryEntry(v, agg)));

        foreach (var kv in markdownMetadata)
        {
            mc.Set(kv.Key, kv.Value);
        }

        return mc;
    }

    private static RootTemplateNode GetTemplate(MetadataContext context)
    {
        if (context.Lookup("templateList") is IReadOnlyDictionary<string, RootTemplateNode> tl)
        {
            if (context.Lookup("template") is { } tn &&
                Expression.FormatValue(tn, null, context) is { } templateName)
            {
                if (tl.TryGetValue(templateName, out var template))
                {
                    return template;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Template `{templateName}` was not found.");
                }
            }
            else if (tl.TryGetValue("page", out var template2))
            {
                return template2;
            }
            else
            {
                throw new InvalidOperationException(
                    "Template `page` was not found.");
            }
        }
        else
        {
            throw new InvalidOperationException(
                "Template list was not found.");
        }
    }

    /// <summary>
    /// Render markdown content.
    /// </summary>
    /// <param name="relativeContentPathHint">Markdown content path</param>
    /// <param name="markdownReader">Markdown content reader</param>
    /// <param name="metadata">Metadata context</param>
    /// <param name="outputHtmlWriter">Generated html content writer</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied template name.</returns>
    public async ValueTask<string> RenderContentAsync(
        PathEntry relativeContentPathHint,
        TextReader markdownReader,
        MetadataContext metadata,
        TextWriter outputHtmlWriter,
        CancellationToken ct)
    {
        var (markdownMetadata, markdownBody) = await Parser.ParseMarkdownBodyAsync(
            relativeContentPathHint, markdownReader, ct).
            ConfigureAwait(false);

        var markdownDocument = MarkdownParser.Parse(markdownBody);

        var contentBody = new StringBuilder();
        var renderer = new HtmlRenderer(new StringWriter(contentBody));
        renderer.Render(markdownDocument);

        var template = GetTemplate(metadata);

        var mc = InjectMetadata(
            metadata,
            relativeContentPathHint,
            contentBody.ToString(),
            markdownMetadata);

        await template.RenderAsync(
            (text, ct) => outputHtmlWriter.WriteAsync(text).WithCancellation(ct),
            mc,
            ct).
            ConfigureAwait(false);

        return template.Name;
    }

    /// <summary>
    /// Render markdown content.
    /// </summary>
    /// <param name="markdownEntry">Markdown entry</param>
    /// <param name="metadata">Metadata context</param>
    /// <param name="outputHtmlPath">Generated html content path</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied template name.</returns>
    public async ValueTask<string> RenderContentAsync(
        MarkdownEntry markdownEntry,
        MetadataContext metadata,
        string outputHtmlPath,
        CancellationToken ct)
    {
        using var markdownStream = new FileStream(
            Path.Combine(
                markdownEntry.contentBasePath,
                markdownEntry.RelativeContentPath.RealPath),
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
            outputHtmlPath,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            65536,
            true);
        var outputHtmlWriter = new StreamWriter(
            outputHtmlStream,
            Encoding.UTF8);

        var appliedTemplateName = await this.RenderContentAsync(
            markdownEntry.RelativeContentPath,
            markdownReader,
            metadata,
            outputHtmlWriter,
            ct).
            ConfigureAwait(false);

        await outputHtmlWriter.FlushAsync().
            ConfigureAwait(false);

        return appliedTemplateName;
    }
}
