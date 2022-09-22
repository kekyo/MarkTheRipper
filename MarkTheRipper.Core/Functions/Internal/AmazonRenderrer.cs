////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions.Internal;

internal static class AmazonRenderrer
{
    // HELP: Appending other entries...
    private static readonly Dictionary<string, (string region, string url)> amazonEmbeddingQueries = new()
    {
        { "www.amazon.com", ("us", "https://ws-na.amazon-adsystem.com/widgets/q?ServiceVersion=20070822&OneJS=1&Operation=GetAdHtml&MarketPlace=US&source=ss&ref=as_ss_li_til&ad_type=product_link&tracking_id={0}&language=en_US&marketplace=amazon&region=US&asins={1}&show_border=false&link_opens_in_new_window=true") },
        { "www.amazon.co.jp", ("jp", "https://rcm-fe.amazon-adsystem.com/e/cm?lt1=_blank&t={0}&language=ja_JP&o=9&p=8&l=as4&m=amazon&f=ifr&ref=as_ss_li_til&asins={1}") },
    };

    //////////////////////////////////////////////////////////////////////////////

    public static async ValueTask<string?> RenderAmazonHtmlContentAsync(
        MetadataContext metadata,
        Uri permaLink,
        bool useInlineHtml,
        CancellationToken ct)
    {
        if (amazonEmbeddingQueries.TryGetValue(permaLink.Host, out var query) &&
            permaLink.PathAndQuery.Split('/') is { } pathElements &&
            pathElements.Reverse().
                // Likes ASIN (https://en.wikipedia.org/wiki/Amazon_Standard_Identification_Number)
                FirstOrDefault(e => e.Length == 10 && e.All(ch => char.IsUpper(ch) || char.IsDigit(ch))) is { } asin &&
            await metadata.LookupValueAsync(
                $"amazonTrackingId-{query.region}", default(string), ct).
                ConfigureAwait(false) is { } trackingId &&
            !string.IsNullOrWhiteSpace(trackingId))
        {
            // Embeddable product badge.
            if (useInlineHtml)
            {
                return $"<iframe sandbox='allow-popups allow-scripts allow-modals allow-forms allow-same-origin' width='120' height='240' marginwidth='0' marginheight='0' scrolling='no' frameborder='0' src='{string.Format(query.url, trackingId, asin)}'></iframe>";
            }

            // Uses PAAPI v5
            if (await metadata.LookupValueAsync(
                $"amazonAccessKey-{query.region}", default(string), ct).
                ConfigureAwait(false) is { } accessKey &&
                !string.IsNullOrWhiteSpace(accessKey) &&
                await metadata.LookupValueAsync(
                $"amazonSecretKey-{query.region}", default(string), ct).
                ConfigureAwait(false) is { } secretKey &&
                !string.IsNullOrWhiteSpace(secretKey))
            {

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
