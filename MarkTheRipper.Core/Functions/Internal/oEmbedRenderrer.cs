////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.IO;
using MarkTheRipper.Metadata;
using MarkTheRipper.TextTreeNodes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions.Internal;

internal static class oEmbedRenderrer
{
    private static readonly Uri oEmbedProviderListUrl =
        new("https://oembed.com/providers.json");

    //////////////////////////////////////////////////////////////////////////////

    public static async ValueTask<RootTextNode> Get_oEmbedLayoutAsync(
        this MetadataContext metadata,
        HtmlMetadata htmlMetadata,
        string infix,
        CancellationToken ct) =>
        // Get layout AST (ITextTreeNode).
        htmlMetadata.SiteName is { } siteName ?
            // `layout-oEmbed-{infix}-{siteName}.html` ==> `layout-oEmbed-{infix}.html`
            await metadata.GetLayoutAsync(
                $"oEmbed-{infix}-{siteName}", $"oEmbed-{infix}", ct).
                ConfigureAwait(false) :
            // `layout-oEmbed-{infix}.html`
            await metadata.GetLayoutAsync(
                $"oEmbed-{infix}", null, ct).
                ConfigureAwait(false);

    public static async ValueTask<RootTextNode> Get_oEmbedLayoutAsync(
        this MetadataContext metadata,
        string infix,
        CancellationToken ct) =>
        // Get layout AST (ITextTreeNode).
        // `layout-oEmbed-{infix}.html`
        await metadata.GetLayoutAsync(
            $"oEmbed-{infix}", null, ct).
            ConfigureAwait(false);

    //////////////////////////////////////////////////////////////////////////////

    private static async ValueTask<string> RenderResponsiveBlockAsync(
        MetadataContext metadata,
        string? siteName,
        JObject oEmbedMetadataJson,
        string iFrameHtmlString,
        CancellationToken ct)
    {
        // Convert to responsive block element.
        var contentBodyString = await oEmbedUtilities.ConvertToResponsiveBlockAsync(
            iFrameHtmlString, ct).
            ConfigureAwait(false);

        // Will transfer MarkTheRipper metadata from oEmbed metadata.
        var htmlMetadata = oEmbedUtilities.CreateHtmlMetadata(
            oEmbedMetadataJson, siteName);
        oEmbedUtilities.SetHtmlMetadata(
            metadata, htmlMetadata);

        // Set patched HTML into metadata context.
        metadata.SetValue("contentBody", contentBodyString);

        // Get layout AST (ITextTreeNode).
        // `layout-oEmbed-html-{siteName}.html` ==> `layout-oEmbed-html.html`
        var layoutNode = await metadata.Get_oEmbedLayoutAsync(
            htmlMetadata, "html", ct).
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
        var layoutNode = await metadata.Get_oEmbedLayoutAsync(
            htmlMetadata, "card", ct).
            ConfigureAwait(false);

        // Render with layout AST with overall metadata.
        var overallHtmlContent = new StringBuilder();
        await layoutNode.RenderAsync(
            text => overallHtmlContent.Append(text), metadata, ct).
            ConfigureAwait(false);

        return overallHtmlContent.ToString();
    }

    //////////////////////////////////////////////////////////////////////////////

    public static async ValueTask<IExpression?> Render_oEmbedAsync(
        IHttpAccessor httpAccessor,
        MetadataContext metadata,
        Uri permaLink,
        bool useInlineHtml,
        CancellationToken ct)
    {
        // Special case: Is it in amazon product page URL?
        if (await AmazonRenderrer.RenderAmazonHtmlContentAsync(
            metadata, permaLink, useInlineHtml, ct).
            ConfigureAwait(false) is { } amazonHtmlString)
        {
            // Accept with sanitized HTML.
            var sanitizedHtmlString = await AmazonRenderrer.RenderAmazonResponsiveBlockAsync(
                metadata,
                amazonHtmlString,
                ct).
                ConfigureAwait(false);
            return new HtmlContentExpression(sanitizedHtmlString);
        }

        // TODO: cache system
        var providersJson = await httpAccessor.FetchJsonAsync(oEmbedProviderListUrl, ct).
            ConfigureAwait(false);

        var permaLinkString = permaLink.ToString();
        var targetEntries = (await Task.WhenAll(
            providersJson.
                EnumerateArray<oEmbedProvider>().
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
                            !string.IsNullOrWhiteSpace(targetEntry?.endPoint.url)).
                        ToArray()))).
            ConfigureAwait(false)).
            SelectMany(endPointUrls => endPointUrls).
            ToArray();

        // If an `html` value is obtained from oEmbed, that data is used first.
        var secondResults = new List<(string providerName, string endPointUrl, JObject metadataJson)>();
        foreach (var targetEntry in targetEntries)
        {
            // oEmbed specification: 2.2. Consumer Request
            var requestUrlString =
                $"{targetEntry.endPoint.url.Trim()}?url={permaLink}&format=json";
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
                        var sanitizedHtmlString = await RenderResponsiveBlockAsync(
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

    public static async ValueTask<IExpression?> Render_oEmbedDiscoveryAsync(
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
                    var sanitizedHtmlString = await RenderResponsiveBlockAsync(
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
}
