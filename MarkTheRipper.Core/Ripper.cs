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
using MarkTheRipper.Functions;
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

    private static void InjectAdditionalMetadata(
        Dictionary<string, object?> markdownMetadata,
        PathEntry markdownPath,
        string? contentBody)
    {
        var storeToPathHint = new PathEntry(
            Path.Combine(
                Path.GetDirectoryName(markdownPath.PhysicalPath) ??
                Path.DirectorySeparatorChar.ToString(),
                Path.GetFileNameWithoutExtension(markdownPath.PhysicalPath) + ".html"));

        markdownMetadata["markdownPath"] = markdownPath;
        markdownMetadata["path"] = storeToPathHint;

        // Special: Automatic insertion for category when not available.
        if (!markdownMetadata.ContainsKey("category"))
        {
            var relativeDirectoryPath =
                Path.GetDirectoryName(markdownPath.PhysicalPath) ??
                Path.DirectorySeparatorChar.ToString();
            var pathElements = relativeDirectoryPath.
                Split(Utilities.PathSeparators, StringSplitOptions.RemoveEmptyEntries);
            markdownMetadata.Add("category",
                pathElements.Aggregate(
                    new PartialCategoryEntry(),
                    (agg, v) => new PartialCategoryEntry(v, agg)));
        }

        if (contentBody != null)
        {
            markdownMetadata["contentBody"] = contentBody;
        }
    }

    /// <summary>
    /// Parse markdown markdownEntry.
    /// </summary>
    /// <param name="contentBasePathHint">Markdown content base path (Hint)</param>
    /// <param name="markdownPath">Markdown content path</param>
    /// <param name="markdownReader">Markdown content reader</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied template name.</returns>
    public async ValueTask<MarkdownEntry> ParseMarkdownHeaderAsync(
        string contentBasePathHint,
        PathEntry markdownPath,
        TextReader markdownReader,
        CancellationToken ct)
    {
        var metadata = await Parser.ParseMarkdownHeaderAsync(
            markdownPath, markdownReader, ct).
            ConfigureAwait(false);

        InjectAdditionalMetadata
            (metadata, markdownPath, null);

        return new(metadata, contentBasePathHint);
    }

    /// <summary>
    /// Parse markdown markdownEntry.
    /// </summary>
    /// <param name="contentsBasePath">Markdown content path</param>
    /// <param name="markdownPath">Markdown content path</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied template name.</returns>
    public async ValueTask<MarkdownEntry> ParseMarkdownHeaderAsync(
        string contentsBasePath,
        PathEntry markdownPath,
        CancellationToken ct)
    {
        using var markdownStream = new FileStream(
            Path.Combine(contentsBasePath, markdownPath.PhysicalPath),
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
            markdownPath,
            markdownReader,
            ct).
            ConfigureAwait(false);
    }

    private static async ValueTask<RootTemplateNode> GetTemplateAsync(
        MetadataContext context, CancellationToken ct)
    {
        if (context.Lookup("templateList") is IReadOnlyDictionary<string, RootTemplateNode> tl)
        {
            if (context.Lookup("template") is { } tn &&
                await Reducer.FormatValueAsync(
                    tn, Utilities.Empty<IExpression>(), context, ct).
                    ConfigureAwait(false) is { } templateName)
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

    private static MetadataContext SpawnWithAdditionalMetadata(
        Dictionary<string, object?> markdownMetadata,
        MetadataContext parentMetadata,
        PathEntry markdownPath,
        string? contentBody)
    {
        var mc = parentMetadata.Spawn();

        mc.Set("relative", CalculateRelativePath.Function);

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
    /// <returns>Applied template name.</returns>
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

        var template = await GetTemplateAsync(metadata, ct).
            ConfigureAwait(false);

        var mc = SpawnWithAdditionalMetadata(
            markdownMetadata,
            metadata,
            markdownPath,
            contentBody.ToString());

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
    /// <param name="storeToBasePath">Generated html content base path</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied template name.</returns>
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

        var appliedTemplateName = await this.RenderContentAsync(
            markdownEntry.MarkdownPath,
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
