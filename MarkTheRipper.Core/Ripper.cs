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
    private readonly IReadOnlyDictionary<string, RootTemplateNode> templates;
    private readonly IReadOnlyDictionary<string, object?> baseMetadata;

    public Ripper(
        IReadOnlyDictionary<string, RootTemplateNode> templates,
        IReadOnlyDictionary<string, object?> baseMetadata)
    {
        this.templates = templates;
        this.baseMetadata = baseMetadata;
    }

    public static ValueTask<RootTemplateNode> ParseTemplateAsync(
        string templatePath, TextReader template, CancellationToken ct) =>
        Parser.ParseTemplateAsync(templatePath, template, ct);

    public static ValueTask<RootTemplateNode> ParseTemplateAsync(
        string templatePath, string templateText, CancellationToken ct) =>
        Parser.ParseTemplateAsync(templatePath, new StringReader(templateText), ct);

    /// <summary>
    /// Rip off and generate from Markdown content.
    /// </summary>
    /// <param name="markdownReader">Markdown content</param>
    /// <param name="htmlWriter">Generated html content</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied template name</returns>
    public async ValueTask<string> RipOffContentAsync(
        TextReader markdownReader,
        TextWriter htmlWriter,
        CancellationToken ct)
    {
        var markdownContent = await Parser.ParseEntireMarkdownAsync(
            markdownReader, ct).
            ConfigureAwait(false);

        var markdownDocument = MarkdownParser.Parse(markdownContent.Body);

        var contentBody = new StringBuilder();
        var renderer = new HtmlRenderer(new StringWriter(contentBody));
        renderer.Render(markdownDocument);

        object? RawGetMetadata(string keyName) =>
            keyName == "contentBody" ?
                contentBody :
                markdownContent.Metadata.TryGetValue(keyName, out var value) ?
                    value :
                    baseMetadata.TryGetValue(keyName, out var baseValue) ?
                        baseValue :
                        null;

        var fp = RawGetMetadata("lang") switch
        {
            IFormatProvider v => v,
            string lang => new CultureInfo(lang),
            _ => CultureInfo.CurrentCulture,
        };

        string? GetMetadata(string keyName, string? parameter, IFormatProvider fp) =>
            RawGetMetadata(keyName) is { } value ?
                Utilities.FormatValue(value, parameter, fp) :
                null;

        var templateName = GetMetadata("template", null, fp) ?? "page";

        if (!templates.TryGetValue(templateName, out var template))
        {
            throw new FormatException(
                $"Could not find template. Name={templateName}");
        }

        await template.RenderAsync(
            (text, ct) => htmlWriter.WriteAsync(text).WithCancellation(ct),
            GetMetadata, fp, ct).
            ConfigureAwait(false);

        return templateName;
    }

    /// <summary>
    /// Rip off and generate from Markdown content.
    /// </summary>
    /// <param name="markdownPath">Markdown content path</param>
    /// <param name="outputHtmlPath">Generated html content path</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>Applied template name.</returns>
    public async ValueTask<string> RipOffContentAsync(
        string markdownPath,
        string outputHtmlPath,
        CancellationToken ct)
    {
        using var markdownStream = new FileStream(
            markdownPath,
            FileMode.Open, FileAccess.Read, FileShare.Read,
            65536, true);
        using var markdownReader = new StreamReader(
            markdownStream, Encoding.UTF8, true);

        using var htmlStream = new FileStream(
            outputHtmlPath,
            FileMode.Create, FileAccess.ReadWrite, FileShare.None,
            65536, true);
        using var htmlWriter = new StreamWriter(
            htmlStream, Encoding.UTF8);

        var templateName = await this.RipOffContentAsync(
            markdownReader,htmlWriter, ct).
            ConfigureAwait(false);

        await htmlWriter.FlushAsync().
            ConfigureAwait(false);

        return templateName;
    }
}
