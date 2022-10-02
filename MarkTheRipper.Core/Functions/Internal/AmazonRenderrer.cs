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

internal static class AmazonRenderrer
{
    // HELP: Appending other entries...
    private static readonly Dictionary<string, AmazonEndPoint> amazonEmbeddingQueries = new()
    {
        { "www.amazon.com", new(
            "us",
            "en-US",
            "https://ws-na.amazon-adsystem.com/widgets/q?ServiceVersion=20070822&OneJS=1&Operation=GetAdHtml&MarketPlace=US&source=ss&ref=as_ss_li_til&ad_type=product_link&tracking_id={0}&language=en_US&marketplace=amazon&region=US&asins={1}&show_border=false&link_opens_in_new_window=true",
            "us-east-1",
            new Uri("https://webservices.amazon.com/paapi5/searchitems")) },
        { "www.amazon.co.jp", new(
            "jp",
            "ja-JP",
            "https://rcm-fe.amazon-adsystem.com/e/cm?lt1=_blank&t={0}&language=ja_JP&o=9&p=8&l=as4&m=amazon&f=ifr&ref=as_ss_li_til&asins={1}",
            "us-west-2",
            new Uri("https://webservices.amazon.co.jp/paapi5/searchitems")) },
    };

    //////////////////////////////////////////////////////////////////////////////

    private static async ValueTask<IExpression> RenderEmbeddablePageAsync(
        MetadataContext metadata,
        AmazonEndPoint endPoint,
        string trackingId,
        string asin,
        CancellationToken ct)
    {
        var sourceUrl = string.Format(
            endPoint.AssociateEndPointFormat, trackingId, asin);
        var contentBody = $"<iframe sandbox='allow-popups allow-scripts allow-modals allow-forms allow-same-origin' width='120' height='240' marginwidth='0' marginheight='0' scrolling='no' frameborder='0' src='{sourceUrl}'></iframe>";

        // Set patched HTML into metadata context.v
        var mc = metadata.Spawn();
        mc.Set("contentBody",
            new ValueExpression(new HtmlContentEntry(contentBody)));

        // Will transfer MarkTheRipper metadata from oEmbed metadata.
        var htmlMetadata = new HtmlMetadata
        {
            SiteName = "Amazon",
            Type = "rich",
        };

        return await oEmbedRenderrer.RenderWithHtmlMetadataAsync(
            mc, "html", htmlMetadata, ct).
            ConfigureAwait(false);
    }

    private static async ValueTask<IExpression?> RenderPAAPIAsync(
        IHttpAccessor httpAccessor,
        MetadataContext metadata,
        Uri permaLink,
        AmazonEndPoint endPoint,
        string asin,
        CancellationToken ct)
    {
        var paapiData = await Task.WhenAll(
            metadata.LookupValueAsync(
                $"amazonPartnerTag-{endPoint.Region}", default(string), ct).AsTask(),
            metadata.LookupValueAsync(
                $"amazonAccessKey-{endPoint.Region}", default(string), ct).AsTask(),
            metadata.LookupValueAsync(
                $"amazonSecretKey-{endPoint.Region}", default(string), ct).AsTask()).
            ConfigureAwait(false);

        if (paapiData[0] is { } partnerTag &&
            !string.IsNullOrWhiteSpace(partnerTag) &&
            paapiData[1] is { } accessKey &&
            !string.IsNullOrWhiteSpace(accessKey) &&
            paapiData[2] is { } secretKey &&
            !string.IsNullOrWhiteSpace(secretKey))
        {
            var request = new AmazonPAAPIGetItemsRequest(
                new[] { asin },
                "ASIN",
                new[] { endPoint.AssociateLanguage },
                permaLink.Host,
                partnerTag,
                "Associates",
                new[] {
                    "Images.Primary.Large",
                    "ItemInfo.Title",
                    "Offers.Listings.Price" });
            var requestJson = JToken.FromObject(
                request,
                InternalUtilities.DefaultJsonSerializer);

            var headers = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Host", endPoint.PAAPIEndPoint.Host },
                { "X-Amz-Target", "com.amazon.paapi5.v1.ProductAdvertisingAPIv1.GetItems" },
                { "Content-Encoding", "amz-1.0" },
            };

            var date = await metadata.LookupValueAsync(
                "generated", DateTimeOffset.Now, ct).
                ConfigureAwait(false);
            var awsv4Auth = new AWSV4Auth.Builder(accessKey, secretKey).
                Date(date).
                Path(endPoint.PAAPIEndPoint.PathAndQuery).
                Region(endPoint.PAAPIEndPointRegion).
                Service("ProductAdvertisingAPI").
                HttpMethodName("POST").
                Headers(headers).
                Payload(requestJson.ToString()).
                Build();

            var awsv4AuthHeader = awsv4Auth.GetHeaders();

            var paapiResultJson = await httpAccessor.PostJsonAsync<AmazonPAAPIGetItemsResponse>(
                endPoint.PAAPIEndPoint, requestJson, awsv4AuthHeader, ct).
                ConfigureAwait(false);
            if (paapiResultJson.Errors.Length >= 1)
            {
                Trace.WriteLine(
                    $"Could not fetch from PAAPI end point: Message={string.Join(",", paapiResultJson.Errors.Select(error => $"{error.Code}: {error.Message}"))}");
            }

            if (paapiResultJson.ItemResults is { } results &&
                results.Items.FirstOrDefault() is { } item &&
                item.ItemInfo?.Title is { } title &&
                item.Images?.Primary?.Large?.URL is { } imageUrl &&
                item.Offers?.Listings.
                    Select(summary => summary.Price).
                    FirstOrDefault()?.DisplayAmount is { } price)
            {
                var paapiResultMetadata = new HtmlMetadata
                {
                    SiteName = "Amazon",
                    Title = title.DisplayValue,
                    Type = "rich",
                    ImageUrl = imageUrl,
                    Description = price,
                };

                return await oEmbedRenderrer.RenderWithHtmlMetadataAsync(
                    metadata, "html", paapiResultMetadata, ct).
                    ConfigureAwait(false);
            }
        }

        return null;
    }

    private static readonly char[] eolSplitChars = new[] {
        '\n', '\r' };

    private static async ValueTask<IExpression?> RenderEmbeddablePageWithParsingAsync(
        IHttpAccessor httpAccessor,
        MetadataContext metadata,
        AmazonEndPoint endPoint,
        string trackingId,
        string asin,
        CancellationToken ct)
    {
        // Embeddable product badge (for parsing)
        var sourceUrl = string.Format(
            endPoint.AssociateEndPointFormat, trackingId, asin);

        var bodyHtml = await httpAccessor.FetchHtmlAsync(
            new Uri(sourceUrl), ct).
            ConfigureAwait(false);

        if (bodyHtml.GetElementsByClassName("amzn-ad-prod-detail") is { } details &&
            details.FirstOrDefault() is { } detail &&
            detail.GetElementsByTagName("script") is { } scripts &&
            scripts.SelectMany(script =>
                script.InnerHtml.Split(eolSplitChars, StringSplitOptions.RemoveEmptyEntries).
                Select(line => line.Trim()).
                Where(line =>
                    line.StartsWith("encodehtml(\"") &&
                    line.EndsWith("\");")).
                Select(line => Utilities.UnescapeJavascriptString(
                    line.Substring("encodehtml(\"".Length, line.Length - "encodehtml(\"\");".Length)))).
                FirstOrDefault() is { } title &&
            bodyHtml.GetElementById("prod-image") is { } prodImage &&
            prodImage.GetAttribute("src") is { } imageUrlString &&
            Uri.TryCreate(imageUrlString, UriKind.Absolute, out var imageUrl) &&
            bodyHtml.GetElementsByClassName("price").
                Select(price => price.InnerHtml.Replace(" ", string.Empty)).
                FirstOrDefault() is { } price)
        {
            var pbResultMetadata = new HtmlMetadata
            {
                SiteName = "Amazon",
                Title = title,
                Type = "rich",
                ImageUrl = imageUrl,
                Description = price,
            };

            return await oEmbedRenderrer.RenderWithHtmlMetadataAsync(
                metadata, "card", pbResultMetadata, ct).
                ConfigureAwait(false);
        }

        return null;
    }

    public static async ValueTask<IExpression?> RenderAmazonHtmlContentAsync(
        IHttpAccessor httpAccessor,
        MetadataContext metadata,
        Uri permaLink,
        bool embedPageIfAvailable,
        CancellationToken ct)
    {
        if (amazonEmbeddingQueries.TryGetValue(permaLink.Host, out var endPoint) &&
            permaLink.PathAndQuery.Split('/') is { } pathElements &&
            pathElements.
                // Likes ASIN (https://en.wikipedia.org/wiki/Amazon_Standard_Identification_Number)
                FirstOrDefault(e => e.Length == 10 && e.All(ch => char.IsUpper(ch) || char.IsDigit(ch))) is { } asin)
        {
            // Retreive tracking id when enables embeddable page.
            string? trackingId = null;
            if (embedPageIfAvailable)
            {
                trackingId = await metadata.LookupValueAsync(
                    $"amazonTrackingId-{endPoint.Region}", default(string), ct).
                    ConfigureAwait(false);

                // Tracking id is available.
                if (!string.IsNullOrWhiteSpace(trackingId))
                {
                    // Constructs embeddable page.
                    return await RenderEmbeddablePageAsync(
                        metadata, endPoint, trackingId!, asin, ct).
                        ConfigureAwait(false);
                }
            }

            // Try uses PAAPI v5
            if (await RenderPAAPIAsync(
                httpAccessor, metadata, permaLink, endPoint, asin, ct).
                ConfigureAwait(false) is { } paapiResult)
            {
                return paapiResult;
            }

            // Retreive tracking id when did not retreive.
            if (!embedPageIfAvailable)
            {
                trackingId = await metadata.LookupValueAsync(
                    $"amazonTrackingId-{endPoint.Region}", default(string), ct).
                    ConfigureAwait(false);
            }

            // Tracking id is available.
            if (!string.IsNullOrWhiteSpace(trackingId))
            {
                if (await RenderEmbeddablePageWithParsingAsync(
                    httpAccessor, metadata, endPoint, trackingId!, asin, ct).
                    ConfigureAwait(false) is { } embeddableResult)
                {
                    return embeddableResult;
                }
            }
        }

        return null;
    }
}
