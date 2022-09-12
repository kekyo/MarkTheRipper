/////////////////////////////////////////////////////////////////////////////////////
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
using MarkTheRipper.Metadata;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions;

internal static class oEmbed
{
    // Thanks suggest Ovis oEmbed handling:
    //   https://github.com/Ovis/BlogGenerator/blob/main/BlogGenerator/ShortCodes/OEmbedShortCodes.cs

    private readonly struct oEmbedEndPoint
    {
        public readonly string? url;
        public readonly Regex[] schemes;

        [JsonConstructor]
        public oEmbedEndPoint(
            string? url, string[]? schemes)
        {
            this.url = url;
            this.schemes = schemes?.
                Select(scheme => new Regex(scheme.Replace("*", ".*"), RegexOptions.Compiled)).
                ToArray() ??
                InternalUtilities.Empty<Regex>();
        }
    }

    private readonly struct oEmbedProvider
    {
        public readonly string provider_name;
        public readonly Uri provider_url;
        public readonly oEmbedEndPoint[] endpoints;

        [JsonConstructor]
        public oEmbedProvider(
            string provider_name,
            Uri provider_url,
            oEmbedEndPoint[] endpoints)
        {
            this.provider_name = provider_name;
            this.provider_url = provider_url;
            this.endpoints = endpoints;
        }
    }

    private static readonly Uri oEmbedProviderListUrl =
        new("https://oembed.com/providers.json");

    private static async ValueTask<string> Sanitize_oEmbedBodySizeAsync(string bodyHtml)
    {
        var parser = new HtmlParser();

        var html = await parser.ParseDocumentAsync(bodyHtml).
            ConfigureAwait(false);

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

        var ratioString = width >= 1 && height >= 1 ?
            $" padding-top:{(height * 100.0 / width).ToString("F3", CultureInfo.InvariantCulture)}%;" :
            string.Empty;

        return $"<div class='oEmbed-outer'><div style='position:relative; width:100%;{ratioString}'>{html.Body!.InnerHtml}</div></div>";
    }

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

        var permaLinkString = await parameters[0].ReduceExpressionAndFormatAsync(metadata, ct).
            ConfigureAwait(false);
        if (!Uri.TryCreate(permaLinkString, UriKind.Absolute, out var permaLink))
        {
            throw new ArgumentException(
                $"Invalid oEmbed function argument: URL={permaLinkString}");
        }

        // TODO: cache system
        var providersJson = await Utilities.FetchJsonAsync(oEmbedProviderListUrl, ct).
            ConfigureAwait(false);

        var targetEndPoints = (await Task.WhenAll(
            providersJson.
                EnumerateArray<oEmbedProvider>().
                Select(provider =>
                    Task.Run(() => provider.endpoints.
                        Select(endPoint => endPoint.schemes.
                            Any(scheme => scheme.IsMatch(permaLinkString)) ?
                                endPoint.url! : null!).
                        Where(endPointUrl => endPointUrl != null).
                        ToArray()))).
            ConfigureAwait(false)).
            SelectMany(endPointUrls => endPointUrls).
            ToArray();

        foreach (var targetEndPoint in targetEndPoints)
        {
            var requestUrlString = $"{targetEndPoint}?url={permaLinkString}";
            try
            {
                var requestUrl = new Uri(requestUrlString, UriKind.Absolute);
                var metadataJson = await Utilities.FetchJsonAsync(requestUrl, ct).
                    ConfigureAwait(false);

                // oEmbed metadata produces `html` data.
                if (metadataJson.GetValue<string>("html") is { } htmlString &&
                    !string.IsNullOrWhiteSpace(htmlString))
                {
                    // Accept with sanitized HTML.
                    var sanitizedHtmlString = await Sanitize_oEmbedBodySizeAsync(htmlString).
                        ConfigureAwait(false);
                    return new HtmlContentExpression(sanitizedHtmlString);
                }

                // TODO: Render with oEmbed metadata and layout.
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    $"Could not fetch from oEmbed end point: Url={requestUrlString}, Message={ex.Message}");
            }
        }

        // TODO: Fetch HTML and render OGP metadata.
        // TODO: Render with layout.

        return new HtmlContentExpression(
            $"<div class='oEmbed-outer'><a class='oEmbed-link' href='{permaLinkString}'>{permaLinkString}</a></div>");
    }
}
