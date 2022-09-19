////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using MarkTheRipper.Expressions;
using MarkTheRipper.Internal;
using MarkTheRipper.Metadata;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions.Internal;

internal static class oEmbedUtilities
{
    // HELP: Appending other entries...
    private static readonly Dictionary<string, string> amazonEmbeddingQueries = new()
    {
        { "www.amazon.com", "https://ws-na.amazon-adsystem.com/widgets/q?ServiceVersion=20070822&OneJS=1&Operation=GetAdHtml&MarketPlace=US&source=ss&ref=as_ss_li_til&ad_type=product_link&tracking_id={0}&language=en_US&marketplace=amazon&region=US&asins={1}&show_border=false&link_opens_in_new_window=true" },
        { "www.amazon.co.jp", "https://rcm-fe.amazon-adsystem.com/e/cm?lt1=_blank&t={0}&language=ja_JP&o=9&p=8&l=as4&m=amazon&f=ifr&ref=as_ss_li_til&asins={1}" },
    };

    //////////////////////////////////////////////////////////////////////////////

    public static HtmlMetadata CreateHtmlMetadata(
        JObject oEmbedMetadataJson, string? siteName) =>
        new HtmlMetadata
        {
            SiteName = oEmbedMetadataJson.GetValue<string?>("provider_name") ?? siteName,
            Title = oEmbedMetadataJson.GetValue<string?>("title"),
            Author = oEmbedMetadataJson.GetValue<string?>("author_name"),
            Type = oEmbedMetadataJson.GetValue<string?>("type"),
            ImageUrl = oEmbedMetadataJson.GetValue<Uri?>("thumbnail_url"),
        };

    public static HtmlMetadata CreateHtmlMetadata(
        IHtmlDocument html, Uri requestUrl)
    {
        var htmlMetadata = new HtmlMetadata();

        // Retreive title.
        if (html.Head?.QuerySelector("title") is { } title &&
            title.TextContent.Trim() is { } t &&
            !string.IsNullOrWhiteSpace(t))
        {
            htmlMetadata.Title = t;
        }

        // Retreive favicon.
        if (html.Head?.QuerySelectorAll("link[rel='icon']").
            AsEnumerable().
            Select(element => Uri.TryCreate(
                element.GetAttribute("href")?.Trim() ?? string.Empty,
                UriKind.RelativeOrAbsolute,
                out var href) ?
                    href : null!).
            Where(href => href != null).
            FirstOrDefault() is { } iconUrl)
        {
            htmlMetadata.ImageUrl = new Uri(requestUrl, iconUrl);
        }

        // Retreive OGP tags.
        foreach (var element in html.Head?.
            QuerySelectorAll("meta").AsEnumerable() ??
            InternalUtilities.Empty<IElement>())
        {
            if (element.GetAttribute("content") is { } content &&
                content.Trim() is { } c &&
                !string.IsNullOrWhiteSpace(c))
            {
                if (element.GetAttribute("property") is { } property &&
                    property.Trim() is { } p &&
                    !string.IsNullOrWhiteSpace(p))
                {
                    switch (p)
                    {
                        case "og:type":
                            htmlMetadata.Type = c;
                            break;
                        case "og:title":
                            if (string.IsNullOrWhiteSpace(htmlMetadata.Title))
                            {
                                htmlMetadata.Title = c;
                            }
                            else
                            {
                                htmlMetadata.AltTitle = htmlMetadata.Title;
                                htmlMetadata.Title = c;
                            }
                            break;
                        case "og:description":
                            htmlMetadata.Description = c;
                            break;
                        case "og:site_name":
                            htmlMetadata.SiteName = c;
                            break;
                        case "og:image" when Uri.TryCreate(c, UriKind.Absolute, out var imageUrl):
                            htmlMetadata.ImageUrl = imageUrl;
                            break;
                    }
                }
            }
        }

        return htmlMetadata;
    }

    //////////////////////////////////////////////////////////////////////////////

    public static void SetHtmlMetadata(
        MetadataContext metadata, HtmlMetadata htmlMetadata)
    {
        metadata.SetValue("siteName", htmlMetadata.SiteName);
        metadata.SetValue("title", htmlMetadata.Title);
        metadata.SetValue("altTitle", htmlMetadata.AltTitle);
        metadata.SetValue("author", htmlMetadata.Author);
        metadata.SetValue("description", htmlMetadata.Description);
        metadata.SetValue("type", htmlMetadata.Type);
        metadata.SetValue("imageUrl", htmlMetadata.ImageUrl);
    }

    //////////////////////////////////////////////////////////////////////////////
    public static async ValueTask<string?> GetAmazonEmbeddedBlockAsync(
    Uri permaLink, MetadataContext metadata, CancellationToken ct)
    {
        if (permaLink.HostNameType == UriHostNameType.Dns &&
            amazonEmbeddingQueries.TryGetValue(permaLink.Host, out var queryUrlFormat) &&
            permaLink.PathAndQuery.Split('/') is { } pathElements &&
            pathElements.Reverse().
                // Likes ASIN (https://en.wikipedia.org/wiki/Amazon_Standard_Identification_Number)
                FirstOrDefault(e => e.Length >= 10 && e.All(char.IsLetterOrDigit)) is { } asin)
        {
            if (metadata.Lookup("amazonTrackingId") is { } trackingIdExpression &&
                await trackingIdExpression.ReduceExpressionAndFormatAsync(metadata, ct).
                ConfigureAwait(false) is { } trackingId &&
                !string.IsNullOrWhiteSpace(trackingId))
            {
                return $"<iframe sandbox='allow-popups allow-scripts allow-modals allow-forms allow-same-origin' width='120' height='240' marginwidth='0' marginheight='0' scrolling='no' frameborder='0' src='{string.Format(queryUrlFormat, trackingId, asin)}'></iframe>";
            }
        }

        return null;
    }

    // TODO: PAAPI

    //////////////////////////////////////////////////////////////////////////////

    public static async ValueTask<string> ConvertToResponsiveBlockAsync(
        string iFrameHtmlString,
        CancellationToken ct)
    {
        // Sanitize non HTTPS links.
        var sanitizedHtmlString = iFrameHtmlString.Replace("http:", "https");

        // Parse oEmbed metadata `html` value.
        var parser = new HtmlParser();
        var html = await parser.ParseDocumentAsync(sanitizedHtmlString, ct).
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
       return $"<div style='position:relative; width:100%;{ratioString}'>{html.Body!.InnerHtml}</div>";
    }
}
