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
using System;
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
    private readonly Func<string, RootTemplateNode?> getTemplate;

    public Ripper(Func<string, RootTemplateNode?> getTemplate) =>
        this.getTemplate = getTemplate;

    public static ValueTask<RootTemplateNode> ParseTemplateAsync(
        string templatePath, TextReader template, CancellationToken ct) =>
        Parser.ParseTemplateAsync(templatePath, template, ct);

    public static ValueTask<RootTemplateNode> ParseTemplateAsync(
        string templatePath, string templateText, CancellationToken ct) =>
        Parser.ParseTemplateAsync(templatePath, new StringReader(templateText), ct);

    /// <summary>
    /// Parse markdown header.
    /// </summary>
    /// <param name="contentBasePathHint">Markdown content base path (Hint)</param>
    /// <param name="relativeContentPath">Markdown content path</param>
    /// <param name="markdownReader">Markdown content reader</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied template name.</returns>
    public async ValueTask<MarkdownHeader> ParseMarkdownHeaderAsync(
        string contentBasePathHint,
        string relativeContentPath,
        TextReader markdownReader,
        CancellationToken ct)
    {
        var metadata = await Parser.ParseMarkdownHeaderAsync(
            relativeContentPath, markdownReader, ct).
            ConfigureAwait(false);

        // Special: category
        if (!metadata.ContainsKey("category"))
        {
            var relativeDirectoryPath =
                Path.GetDirectoryName(relativeContentPath) ??
                Path.DirectorySeparatorChar.ToString();
            var pathElements = relativeDirectoryPath.
                Split(Utilities.PathSeparators, StringSplitOptions.RemoveEmptyEntries);

            metadata.Add("category", pathElements);
        }

        return new(relativeContentPath, metadata, contentBasePathHint);
    }

    /// <summary>
    /// Parse markdown header.
    /// </summary>
    /// <param name="contentsBasePath">Markdown content path</param>
    /// <param name="relativeContentPath">Markdown content path</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied template name.</returns>
    public async ValueTask<MarkdownHeader> ParseMarkdownHeaderAsync(
        string contentsBasePath,
        string relativeContentPath,
        CancellationToken ct)
    {
        using var markdownStream = new FileStream(
            Path.Combine(contentsBasePath, relativeContentPath),
            FileMode.Open, FileAccess.Read, FileShare.Read,
            65536, true);
        using var markdownReader = new StreamReader(
            markdownStream, Encoding.UTF8, true);

        return await this.ParseMarkdownHeaderAsync(
            contentsBasePath, relativeContentPath, markdownReader, ct).
            ConfigureAwait(false);
    }

    /// <summary>
    /// Render markdown content.
    /// </summary>
    /// <param name="markdownHeader">Parsed markdown header</param>
    /// <param name="markdownReader">Markdown content path</param>
    /// <param name="getMetadata">Metadata getter</param>
    /// <param name="outputHtmlWriter">Generated html content writer</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied template name.</returns>
    public async ValueTask<string> RenderContentAsync(
        MarkdownHeader markdownHeader,
        TextReader markdownReader,
        Func<string, object?> getMetadata,
        TextWriter outputHtmlWriter,
        CancellationToken ct)
    {
        var body = await Parser.ParseMarkdownBodyAsync(
            markdownReader, ct).
            ConfigureAwait(false);

        var markdownDocument = MarkdownParser.Parse(body);

        var contentBody = new StringBuilder();
        var renderer = new HtmlRenderer(new StringWriter(contentBody));
        renderer.Render(markdownDocument);

        var fp = (markdownHeader.GetMetadata("lang") is { } langValue ?
            langValue : getMetadata("lang")) switch
        {
            IFormatProvider v => v,
            string lang => new CultureInfo(lang),
            _ => CultureInfo.CurrentCulture,
        };

        object? GetMetadata(string keyName) =>
            keyName == "contentBody" ?
                contentBody :
                markdownHeader.GetMetadata(keyName) is { } value ?
                    value :
                    getMetadata(keyName);

        var templateName =
            GetMetadata("template")?.ToString() ?? "page";

        if (this.getTemplate(templateName) is { } template)
        {
            await template.RenderAsync(
                (text, ct) => outputHtmlWriter.WriteAsync(text).WithCancellation(ct),
                GetMetadata, fp, ct).
                ConfigureAwait(false);

            return templateName;
        }
        else
        {
            throw new FormatException(
                $"Could not find template. Name={templateName}");
        }
    }

    /// <summary>
    /// Render markdown content.
    /// </summary>
    /// <param name="markdownHeader">Parsed markdown header</param>
    /// <param name="getMetadata">Metadata getter</param>
    /// <param name="outputHtmlPath">Generated html content path</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied template name.</returns>
    public async ValueTask<string> RenderContentAsync(
        MarkdownHeader markdownHeader,
        Func<string, object?> getMetadata,
        string outputHtmlPath,
        CancellationToken ct)
    {
        using var markdownStream = new FileStream(
            Path.Combine(markdownHeader.ContentBasePath, markdownHeader.RelativeContentPath),
            FileMode.Open, FileAccess.Read, FileShare.Read,
            65536, true);
        using var markdownReader = new StreamReader(
            markdownStream, Encoding.UTF8, true);

        using var outputHtmlStream = new FileStream(
            outputHtmlPath,
            FileMode.Create, FileAccess.ReadWrite, FileShare.None,
            65536, true);
        using var outputHtmlWriter = new StreamWriter(
            outputHtmlStream, Encoding.UTF8);

        var appliedTemplateName = await this.RenderContentAsync(
            markdownHeader, markdownReader, getMetadata,
            outputHtmlWriter, ct).
            ConfigureAwait(false);

        await outputHtmlWriter.FlushAsync().
            ConfigureAwait(false);

        return appliedTemplateName;
    }
}
