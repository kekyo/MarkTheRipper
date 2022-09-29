////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using MarkTheRipper.IO;
using MarkTheRipper.Metadata;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions.Internal;

internal static class AmazonRenderrer
{
    private sealed class AmazonEndPoint
    {
        public readonly string Region;
        public readonly string AssociateLanguage;
        public readonly string AssociateEndPointFormat;

        public readonly string PAAPIEndPointRegion;
        public readonly Uri PAAPIEndPoint;

        public AmazonEndPoint(
            string region,
            string associateLanguage,
            string associateEndPointFormat,
            string paapiEndPointRegion,
            Uri paapiEndPoint)
        {
            this.Region = region;
            this.AssociateLanguage = associateLanguage;
            this.AssociateEndPointFormat = associateEndPointFormat;
            this.PAAPIEndPointRegion = paapiEndPointRegion;
            this.PAAPIEndPoint = paapiEndPoint;
        }
    }

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

    public static async ValueTask<string?> RenderAmazonHtmlContentAsync(
        IHttpAccessor httpAccessor,
        MetadataContext metadata,
        Uri permaLink,
        bool useInlineHtml,
        CancellationToken ct)
    {
        if (amazonEmbeddingQueries.TryGetValue(permaLink.Host, out var endPoint) &&
            permaLink.PathAndQuery.Split('/') is { } pathElements &&
            pathElements.Reverse().
                // Likes ASIN (https://en.wikipedia.org/wiki/Amazon_Standard_Identification_Number)
                FirstOrDefault(e => e.Length == 10 && e.All(ch => char.IsUpper(ch) || char.IsDigit(ch))) is { } asin)
        {
            // Embeddable product badge.
            if (useInlineHtml &&
                await metadata.LookupValueAsync(
                    $"amazonTrackingId-{endPoint.Region}", default(string), ct).
                    ConfigureAwait(false) is { } trackingId &&
                    !string.IsNullOrWhiteSpace(trackingId))
            {
                return $"<iframe sandbox='allow-popups allow-scripts allow-modals allow-forms allow-same-origin' width='120' height='240' marginwidth='0' marginheight='0' scrolling='no' frameborder='0' src='{string.Format(endPoint.AssociateEndPointFormat, trackingId, asin)}'></iframe>";
            }

            // Uses PAAPI v5
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
                    new[] { "Images.Primary.Large", "Images.Variants.Large",
                            "ItemInfo.Title", "ItemInfo.Features",
                            "Offers.Summaries.Condition", "Offers.Summaries.HighestPrice", "Offers.Summaries.LowestPrice" });
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

                // TODO:
            }
        }

        return null;
    }

    //////////////////////////////////////////////////////////////////////////////

    public static async ValueTask<string> RenderAmazonResponsiveBlockAsync(
        MetadataContext metadata,
        string iFrameHtmlString,
        CancellationToken ct)
    {
        // Will transfer MarkTheRipper metadata from oEmbed metadata.
        var htmlMetadata = new HtmlMetadata
        {
            SiteName = "Amazon",
            Type = "rich",
        };
        oEmbedUtilities.SetHtmlMetadata(
            metadata, htmlMetadata);

        // Set patched HTML into metadata context.
        metadata.SetValue("contentBody", iFrameHtmlString);

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
}
