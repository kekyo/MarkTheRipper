////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Internal;
using MarkTheRipper.IO;
using MarkTheRipper.Metadata;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions.Internal;

internal static class oEmbedRenderrer
{
    private static readonly Uri oEmbedProviderListUrl =
        new("https://oembed.com/providers.json");

    //////////////////////////////////////////////////////////////////////////////

    public static async ValueTask<IExpression> RenderWithHtmlMetadataAsync(
        IMetadataContext metadata,
        string layoutInfix,
        HtmlMetadata htmlMetadata,
        CancellationToken ct)
    {
        oEmbedUtilities.SetHtmlMetadata(metadata, htmlMetadata);

        // Get layout AST (ITextTreeNode).
        // `{layoutInfix}-{siteName}.html` ==> `{layoutInfix}.html`
        var layoutNode = await metadata.Get_oEmbedLayoutAsync(
            htmlMetadata, layoutInfix, ct).
            ConfigureAwait(false);

        // Render with layout AST with overall metadata.
        var overallHtmlContent = await layoutNode.RenderOverallAsync(metadata, ct).
            ConfigureAwait(false);

        // Done.
        return new ValueExpression(
            new HtmlContentEntry(overallHtmlContent));
    }

    //////////////////////////////////////////////////////////////////////////////

    private static async ValueTask<IExpression> RenderResponsiveBlockAsync(
        IMetadataContext metadata,
        string? siteName,
        JObject oEmbedMetadataJson,
        string iFrameHtmlString,
        CancellationToken ct)
    {
        // Convert to responsive block element.
        var contentBodyString = await oEmbedUtilities.ConvertToResponsiveBlockAsync(
            iFrameHtmlString, ct).
            ConfigureAwait(false);

        // Set patched HTML into metadata context.
        var mc = metadata.Spawn();
        mc.Set("contentBody",
            new ValueExpression(new HtmlContentEntry(contentBodyString)));

        // Will transfer MarkTheRipper metadata from oEmbed metadata.
        var htmlMetadata = oEmbedUtilities.CreateHtmlMetadata(
            oEmbedMetadataJson, siteName);

        return await RenderWithHtmlMetadataAsync(
            mc, "embed", htmlMetadata, ct).
            ConfigureAwait(false);
    }

    //////////////////////////////////////////////////////////////////////////////

    private static ValueTask<IExpression> Render_oEmbedCardAsync(
        IMetadataContext metadata,
        string? siteName,
        JObject oEmbedMetadataJson,
        CancellationToken ct)
    {
        // Removed parent content body.
        var mc = metadata.Spawn();
        mc.SetValue("contentBody", string.Empty);

        // Will transfer MarkTheRipper metadata from oEmbed metadata.
        var htmlMetadata = oEmbedUtilities.CreateHtmlMetadata(
            oEmbedMetadataJson, siteName);

        return RenderWithHtmlMetadataAsync(
            mc, "card", htmlMetadata, ct);
    }

    //////////////////////////////////////////////////////////////////////////////

    public static async ValueTask<IExpression?> Render_oEmbedAsync(
        IHttpAccessor httpAccessor,
        IMetadataContext metadata,
        Uri permaLink,
        Uri examinedLink,
        bool embedPageIfAvailable,
        CancellationToken ct)
    {
        // TODO: cache system
        var providersJson = await httpAccessor.FetchJsonAsync(oEmbedProviderListUrl, ct).
            ConfigureAwait(false);

        static bool IsMatched(oEmbedEndPoint endPoint, string urlString) =>
            endPoint.matchers.Any(matcher => matcher(urlString));

        var permaLinkString = permaLink.ToString();
        var examinedLinkString = examinedLink.ToString();
        var targetEntries = (await Task.WhenAll(
            providersJson.
                EnumerateArray<oEmbedProvider>().
                Where(provider =>
                    !string.IsNullOrWhiteSpace(provider.provider_name) &&
                    provider.provider_url != null &&
                    provider.endpoints.Length >= 1).
                Select(provider =>
                    Task.Run(() => provider.endpoints.
                        Collect(endPoint =>
                        {
                            if (IsMatched(endPoint, permaLinkString))
                            {
                                return new { provider, endPoint, url = permaLink };
                            }
                            else if (IsMatched(endPoint, examinedLinkString))
                            {
                                return new { provider, endPoint, url = examinedLink };
                            }
                            else
                            {
                                return null!;
                            }
                        }).
                        Where(targetEntry =>
                            !string.IsNullOrWhiteSpace(targetEntry.endPoint.url)).
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
                $"{targetEntry.endPoint.url.Trim()}?url={targetEntry.url}&format=json";
            try
            {
                var requestUrl = new Uri(requestUrlString, UriKind.Absolute);

                // TODO: cache system
                var metadataJson = await httpAccessor.FetchJsonAsync(requestUrl, ct).
                    ConfigureAwait(false);

                if (metadataJson is JObject metadataJsonObj)
                {
                    // oEmbed metadata produces `html` embeddable data.
                    if (embedPageIfAvailable &&
                        metadataJsonObj.GetValue<string>("html") is { } htmlString &&
                        !string.IsNullOrWhiteSpace(htmlString))
                    {
                        // Accept with sanitized HTML.
                        return await RenderResponsiveBlockAsync(
                            metadata,
                            targetEntry.provider.provider_name,
                            metadataJsonObj,
                            htmlString,
                            ct).
                            ConfigureAwait(false);
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
            try
            {
                return await Render_oEmbedCardAsync(
                    metadata,
                    providerName,
                    metadataJsonObj,
                    ct).
                    ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var requestUrlString = $"{endPointUrl}?url={permaLink}";
                Trace.WriteLine(
                    $"Could not fetch from oEmbed end point: Url={requestUrlString}, Message={ex.Message}");
            }
        }

        return null;
    }

    public static async ValueTask<IExpression?> Render_oEmbedDiscoveryAsync(
        IHttpAccessor httpAccessor,
        IMetadataContext metadata,
        Uri permaLink,
        bool embedPageIfAvailable,
        CancellationToken ct)
    {
        try
        {
            // TODO: cache system
            var metadataJson = await httpAccessor.FetchJsonAsync(permaLink, ct).
                ConfigureAwait(false);

            if (metadataJson is JObject metadataJsonObj)
            {
                // oEmbed metadata produces `html` embeddable data.
                if (embedPageIfAvailable &&
                    metadataJsonObj.GetValue<string>("html") is { } htmlString &&
                    !string.IsNullOrWhiteSpace(htmlString))
                {
                    // Accept with sanitized HTML.
                    return await RenderResponsiveBlockAsync(
                        metadata,
                        null,
                        metadataJsonObj,
                        htmlString,
                        ct).
                        ConfigureAwait(false);
                }
                else
                {
                    // Render with oEmbed metadata and layout when produce oEmbed metadata.
                    return await Render_oEmbedCardAsync(
                        metadata,
                        null,
                        metadataJsonObj,
                        ct).
                        ConfigureAwait(false);
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
