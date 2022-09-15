////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using MarkTheRipper.Expressions;
using MarkTheRipper.Internal;
using MarkTheRipper.IO;
using MarkTheRipper.Metadata;
using MarkTheRipper.TextTreeNodes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions;

// Thanks suggest Ovis oEmbed handling:
// https://github.com/Ovis/BlogGenerator/blob/main/BlogGenerator/ShortCodes/OEmbedShortCodes.cs

internal static class oEmbed
{
    public static readonly Uri oEmbedProviderListUrl =
        new("https://oembed.com/providers.json");

    private static async ValueTask<RootTextNode> GetLayoutAsync(
        MetadataContext metadata,
        oEmbedUtilities.HtmlMetadata htmlMetadata,
        string infix,
        CancellationToken ct) =>
        // Get layout AST (ITextTreeNode).
        htmlMetadata.SiteName is { } siteName ?
            // `layout-oEmbed-{infix}-{siteName}.html` ==> `layout-oEmbed-{infix}.html`
            await MetadataUtilities.GetLayoutAsync(
                $"oEmbed-{infix}-{siteName}", $"oEmbed-{infix}", metadata, ct).
                ConfigureAwait(false) :
            // `layout-oEmbed-{infix}.html`
            await MetadataUtilities.GetLayoutAsync(
                $"oEmbed-{infix}", null, metadata, ct).
                ConfigureAwait(false);

    //////////////////////////////////////////////////////////////////////////////

    private static async ValueTask<string> Render_oEmbedSanitizedHtmlBodySizeAsync(
        MetadataContext metadata,
        string? siteName,
        JObject oEmbedMetadataJson,
        string oEmbedHtmlString,
        CancellationToken ct)
    {
        // Parse oEmbed metadata `html` value.
        var parser = new HtmlParser();
        var html = await parser.ParseDocumentAsync(oEmbedHtmlString, ct).
            ConfigureAwait(false);

        // Will patch HTML attributes because makes helping responsive design.
        var width = -1;
        var height = -1;
        foreach (var element in
            html.Body!.Children.AsEnumerable() ?? InternalUtilities.Empty<IElement>())
        {
            var styleString = element.GetAttribute("style") ?? string.Empty;
            styleString += "position:absolute !important; top:0 !important; left:0 !important; width:100% !important; height:100% !important;";
            element.SetAttribute("style", styleString);

            if (element.GetAttribute("width") is { } ws &&
                int.TryParse(ws, out var w))
            {
                element.RemoveAttribute("width");
                width = w;
            }
            if (element.GetAttribute("height") is { } hs &&
                int.TryParse(hs, out var h))
            {
                element.RemoveAttribute("height");
                height = h;
            }
        }

        // Makes original aspect ratio.
        var ratioString = width >= 1 && height >= 1 ?
            $" padding-top:{(height * 100.0 / width).ToString("F3", CultureInfo.InvariantCulture)}%;" :
            string.Empty;

        // Render final patched HTML.
        var contentBodyString =
            $"<div style='position:relative; width:100%;{ratioString}'>{html.Body!.InnerHtml}</div>";

        // Will transfer MarkTheRipper metadata from oEmbed metadata.
        var htmlMetadata = oEmbedUtilities.CreateHtmlMetadata(
            oEmbedMetadataJson, siteName);
        oEmbedUtilities.SetHtmlMetadata(
            metadata, htmlMetadata);

        // Set patched HTML into metadata context.
        metadata.SetValue("contentBody", contentBodyString);

        // Get layout AST (ITextTreeNode).
        // `layout-oEmbed-html-{siteName}.html` ==> `layout-oEmbed-html.html`
        var layoutNode = await GetLayoutAsync(
            metadata, htmlMetadata, "html", ct).
            ConfigureAwait(false);

        // Render with layout AST with overall metadata.
        var overallHtmlContent = new StringBuilder();
        await layoutNode.RenderAsync(
            text => overallHtmlContent.Append(text), metadata, ct).
            ConfigureAwait(false);

        return overallHtmlContent.ToString();
    }

    //////////////////////////////////////////////////////////////////////////////

    private static async ValueTask<string> Render_oEmbedCardAsync(
        MetadataContext metadata,
        string? siteName,
        JObject oEmbedMetadataJson,
        CancellationToken ct)
    {
        // Will transfer MarkTheRipper metadata from oEmbed metadata.
        var htmlMetadata = oEmbedUtilities.CreateHtmlMetadata(
            oEmbedMetadataJson, siteName);
        oEmbedUtilities.SetHtmlMetadata(
            metadata, htmlMetadata);

        // Get layout AST (ITextTreeNode).
        // `layout-oEmbed-card-{siteName}.html` ==> `layout-oEmbed-card.html`
        var layoutNode = await GetLayoutAsync(
            metadata, htmlMetadata, "card", ct).
            ConfigureAwait(false);

        // Render with layout AST with overall metadata.
        var overallHtmlContent = new StringBuilder();
        await layoutNode.RenderAsync(
            text => overallHtmlContent.Append(text), metadata, ct).
            ConfigureAwait(false);

        return overallHtmlContent.ToString();
    }

    //////////////////////////////////////////////////////////////////////////////

    private static async ValueTask<IExpression?> Render_oEmbedAsync(
        IHttpAccessor httpAccessor,
        MetadataContext metadata,
        Uri permaLink,
        bool useInlineHtml,
        CancellationToken ct)
    {
        // TODO: cache system
        var providersJson = await httpAccessor.FetchJsonAsync(oEmbedProviderListUrl, ct).
            ConfigureAwait(false);

        var permaLinkString = permaLink.ToString();
        var targetEntries = (await Task.WhenAll(
            providersJson.
                EnumerateArray<oEmbedUtilities.oEmbedProvider>().
                Where(provider =>
                    !string.IsNullOrWhiteSpace(provider.provider_name) &&
                    provider.provider_url != null &&
                    provider.endpoints.Length >= 1).
                Select(provider =>
                    Task.Run(() => provider.endpoints.
                        Select(endPoint => endPoint.matchers.
                            Any(matcher => matcher(permaLinkString)) ?
                                new { provider, endPoint } : null!).
                        Where(targetEntry =>
                            targetEntry != null &&
                            !string.IsNullOrWhiteSpace(targetEntry.endPoint.url)).
                        ToArray()))).
            ConfigureAwait(false)).
            SelectMany(endPointUrls => endPointUrls).
            ToArray();

        // If an `html` value is obtained from oEmbed, that data is used first.
        var secondResults = new List<(string providerName, string endPointUrl, JObject metadataJson)>();
        foreach (var targetEntry in targetEntries)
        {
            var requestUrlString = $"{targetEntry.endPoint.url.Trim()}?url={permaLink}";
            try
            {
                var requestUrl = new Uri(requestUrlString, UriKind.Absolute);

                // TODO: cache system
                var metadataJson = await httpAccessor.FetchJsonAsync(requestUrl, ct).
                    ConfigureAwait(false);

                if (metadataJson is JObject metadataJsonObj)
                {
                    // oEmbed metadata produces `html` data.
                    if (useInlineHtml &&
                        metadataJsonObj.GetValue<string>("html") is { } htmlString &&
                        !string.IsNullOrWhiteSpace(htmlString))
                    {
                        // Accept with sanitized HTML.
                        var sanitizedHtmlString = await Render_oEmbedSanitizedHtmlBodySizeAsync(
                            metadata,
                            targetEntry.provider.provider_name,
                            metadataJsonObj,
                            htmlString,
                            ct).
                            ConfigureAwait(false);
                        return new HtmlContentExpression(sanitizedHtmlString);
                    }
                    else
                    {
                        secondResults.Add(
                            (targetEntry.provider.provider_name, targetEntry.endPoint.url, metadataJsonObj));
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    $"Could not fetch from oEmbed end point: Url={requestUrlString}, Message={ex.Message}");
            }
        }

        // Render with oEmbed metadata and layout when produce one or more oEmbed metadata.
        foreach (var (providerName, endPointUrl, metadataJsonObj) in secondResults)
        {
            var requestUrlString = $"{endPointUrl}?url={permaLink}";
            try
            {
                var overallHtmlContentString = await Render_oEmbedCardAsync(
                    metadata,
                    providerName,
                    metadataJsonObj,
                    ct).
                    ConfigureAwait(false);
                return new HtmlContentExpression(overallHtmlContentString);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    $"Could not fetch from oEmbed end point: Url={requestUrlString}, Message={ex.Message}");
            }
        }

        return null;
    }

    private static async ValueTask<IExpression?> Render_oEmbedDiscoveryAsync(
        IHttpAccessor httpAccessor,
        MetadataContext metadata,
        Uri permaLink,
        bool useInlineHtml,
        CancellationToken ct)
    {
        try
        {
            // TODO: cache system
            var metadataJson = await httpAccessor.FetchJsonAsync(permaLink, ct).
                ConfigureAwait(false);

            if (metadataJson is JObject metadataJsonObj)
            {
                // oEmbed metadata produces `html` data.
                if (useInlineHtml &&
                    metadataJsonObj.GetValue<string>("html") is { } htmlString &&
                    !string.IsNullOrWhiteSpace(htmlString))
                {
                    // Accept with sanitized HTML.
                    var sanitizedHtmlString = await Render_oEmbedSanitizedHtmlBodySizeAsync(
                        metadata,
                        null,
                        metadataJsonObj,
                        htmlString,
                        ct).
                        ConfigureAwait(false);
                    return new HtmlContentExpression(sanitizedHtmlString);
                }
                else
                {
                    // Render with oEmbed metadata and layout when produce oEmbed metadata.
                    var overallHtmlContentString = await Render_oEmbedCardAsync(
                        metadata,
                        null,
                        metadataJsonObj,
                        ct).
                        ConfigureAwait(false);
                    return new HtmlContentExpression(overallHtmlContentString);
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(
                $"Could not fetch from oEmbed discovery end point: Url={permaLink}, Message={ex.Message}");
        }

        return null;
    }

    //////////////////////////////////////////////////////////////////////////////

    private static async ValueTask<IExpression> Internal_oEmbedAsync(
        Uri permaLink,
        MetadataContext metadata,
        bool useInlineHtml,
        CancellationToken ct)
    {
        var httpAccessor = (await metadata.GetValueAsync(
            "httpAccessor", HttpAccessor.Instance, ct).
            ConfigureAwait(false))!;

        var mc = metadata.Spawn();
        mc.SetValue("permaLink", permaLink);

        //////////////////////////////////////////////////////////////////
        // Step 1. Automatic resolve using global oEmbed provider list.

        // Render oEmbed from perma link.
        if (await Render_oEmbedAsync(
            httpAccessor, mc, permaLink, useInlineHtml, ct).
            ConfigureAwait(false) is { } result1)
        {
            // Done.
            return result1;
        }

        //////////////////////////////////////////////////////////////////
        // Step 2. Fetch HTML from perma link directly.

        try
        {
            // TODO: cache system
            var html = await httpAccessor.FetchHtmlAsync(
                permaLink, ct).
                ConfigureAwait(false);

            //////////////////////////////////////////////////////////////////
            // Step 3. Resolve by oEmbed discover meta tag link.

            // Contains oEmbed meta tags.
            if (html.Head?.QuerySelector("link[type='application/json+oembed']") is { } oEmbedLinkElement &&
                oEmbedLinkElement.GetAttribute("href") is { } hrefString &&
                hrefString.Trim() is { } hs &&
                Uri.TryCreate(hs, UriKind.Absolute, out var href))
            {
                // Render oEmbed from discovered perma link.
                if (await Render_oEmbedDiscoveryAsync(
                    httpAccessor, mc, href, useInlineHtml, ct).
                    ConfigureAwait(false) is { } result2)
                {
                    // Done.
                    return result2;
                }
            }

            //////////////////////////////////////////////////////////////////
            // Step 4. Give up oEmbed resolving, retreive meta tags from HTML.

            var htmlMetadata = oEmbedUtilities.CreateHtmlMetadata(
                html, permaLink);
            oEmbedUtilities.SetHtmlMetadata(mc, htmlMetadata);

            // Get layout AST (ITextTreeNode).
            // `layout-oEmbed-card-{siteName}.html` ==> `layout-oEmbed-card.html`
            var layoutNode = await GetLayoutAsync(
                metadata, htmlMetadata, "card", ct).
                ConfigureAwait(false);

            // Render with layout AST with overall metadata.
            var overallHtmlContent = new StringBuilder();
            await layoutNode.RenderAsync(
                text => overallHtmlContent.Append(text), mc, ct).
                ConfigureAwait(false);

            // Done.
            return new HtmlContentExpression(overallHtmlContent.ToString());
        }
        catch (Exception ex)
        {
            Trace.WriteLine(
                $"Could not fetch perma link content: Url={permaLink}, Message={ex.Message}");
        }

        //////////////////////////////////////////////////////////////////
        // Step 6. Could not fetch any information.

        {
            // Render with layout.
            // Get layout AST (ITextTreeNode).
            // `layout-oEmbed-card.html`
            var layoutNode = await MetadataUtilities.GetLayoutAsync(
                $"oEmbed-card", null, mc, ct).
                ConfigureAwait(false);

            // Render with layout AST with overall metadata.
            var overallHtmlContent = new StringBuilder();
            await layoutNode.RenderAsync(
                text => overallHtmlContent.Append(text), mc, ct).
                ConfigureAwait(false);

            // Done.
            return new HtmlContentExpression(overallHtmlContent.ToString());
        }
    }

    //////////////////////////////////////////////////////////////////////////////

    public static async ValueTask<IExpression> oEmbedAsync(
        IExpression[] parameters,
        MetadataContext metadata,
        CancellationToken ct)
    {
        if (parameters.Length != 1)
        {
            throw new ArgumentException(
                $"Invalid oEmbed function arguments: Count={parameters.Length}");
        }

        var permaLinkString = (await parameters[0].
            ReduceExpressionAndFormatAsync(metadata, ct).
            ConfigureAwait(false)).
            Trim();
        if (!Uri.TryCreate(permaLinkString, UriKind.Absolute, out var permaLink))
        {
            throw new ArgumentException(
                $"Invalid oEmbed function argument: URL={permaLinkString}");
        }

        return await Internal_oEmbedAsync(
            permaLink, metadata, true, ct).
            ConfigureAwait(false);
    }


    public static async ValueTask<IExpression> CardAsync(
        IExpression[] parameters,
        MetadataContext metadata,
        CancellationToken ct)
    {
        if (parameters.Length != 1)
        {
            throw new ArgumentException(
                $"Invalid card function arguments: Count={parameters.Length}");
        }

        var permaLinkString = (await parameters[0].
            ReduceExpressionAndFormatAsync(metadata, ct).
            ConfigureAwait(false)).
            Trim();
        if (!Uri.TryCreate(permaLinkString, UriKind.Absolute, out var permaLink))
        {
            throw new ArgumentException(
                $"Invalid card function argument: URL={permaLinkString}");
        }

        return await Internal_oEmbedAsync(
            permaLink, metadata, false, ct).
            ConfigureAwait(false);
    }
}
