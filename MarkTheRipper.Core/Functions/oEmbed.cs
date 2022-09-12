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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions;

// Thanks suggest Ovis oEmbed handling:
//   https://github.com/Ovis/BlogGenerator/blob/main/BlogGenerator/ShortCodes/OEmbedShortCodes.cs

internal static class oEmbed
{
    private static readonly Uri oEmbedProviderListUrl =
        new("https://oembed.com/providers.json");

    //////////////////////////////////////////////////////////////////////////////

    private sealed class oEmbedEndPoint : IMetadataEntry
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

        public ValueTask<object?> GetImplicitValueAsync(
            MetadataContext metadata, CancellationToken ct) =>
            new(url);

        public ValueTask<object?> GetPropertyValueAsync(
            string keyName, MetadataContext context, CancellationToken ct) =>
            new(keyName switch
            {
                "url" => this.url,
                _ => null,
            });

        public override string ToString() =>
            $"{this.url ?? "(Unknown url)"}, Schemes={this.schemes.Length}";
    }

    private sealed class oEmbedProvider : IMetadataEntry
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

        public ValueTask<object?> GetImplicitValueAsync(
            MetadataContext metadata, CancellationToken ct) =>
            new(provider_name);

        public ValueTask<object?> GetPropertyValueAsync(
            string keyName, MetadataContext context, CancellationToken ct) =>
            new(keyName switch
            {
                "name" => this.provider_name,
                "url" => this.provider_url,
                _ => null,
            });

        public override string ToString() =>
            $"{this.provider_name}, Url={this.provider_url}, EndPoints={this.endpoints.Length}";
    }

    //////////////////////////////////////////////////////////////////////////////

    private static MetadataContext SpawnWith_oEmbedMetadata(
        MetadataContext metadata, oEmbedProvider provider, oEmbedEndPoint endPoint, JObject oEmbedMetadataJson)
    {
        var mc = metadata.Spawn();
        mc.SetValue("provider", provider);
        mc.SetValue("endPoint", endPoint);
        foreach (var entry in oEmbedMetadataJson)
        {
            mc.SetValue(entry.Key, entry.Value?.ToObject<string>() ?? string.Empty);
        }
        return mc;
    }

    //////////////////////////////////////////////////////////////////////////////

    private static async ValueTask<string> Render_oEmbedSanitizedHtmlBodySizeAsync(
        MetadataContext metadata,
        oEmbedProvider provider, oEmbedEndPoint endPoint,
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
        var mc = SpawnWith_oEmbedMetadata(
            metadata, provider, endPoint, oEmbedMetadataJson);

        // Set patched HTML into metadata context.
        mc.SetValue("contentBody", contentBodyString);

        // Get layout AST (ITextTreeNode).
        // `layout-oEmbed-*.html` ==> `layout-oEmbed-html.html`
        var layoutNode = await MetadataUtilities.GetLayoutAsync(
            $"oEmbed-{provider.provider_name}", "oEmbed-html", metadata, ct).
            ConfigureAwait(false);

        // Render with layout AST with overall metadata.
        var overallHtmlContent = new StringBuilder();
        await layoutNode.RenderAsync(
            text => overallHtmlContent.Append(text), mc, ct).
            ConfigureAwait(false);

        return overallHtmlContent.ToString();
    }

    //////////////////////////////////////////////////////////////////////////////

    private static async ValueTask<string> Render_oEmbedMetadataAsync(
        MetadataContext metadata,
        oEmbedProvider provider, oEmbedEndPoint endPoint,
        JObject oEmbedMetadataJson,
        CancellationToken ct)
    {
        // Will transfer MarkTheRipper metadata from oEmbed metadata.
        var mc = SpawnWith_oEmbedMetadata(
            metadata, provider, endPoint, oEmbedMetadataJson);

        // Get layout AST (ITextTreeNode).
        // `layout-oEmbed-*.html` ==> `layout-oEmbed-default.html`
        var layoutNode = await MetadataUtilities.GetLayoutAsync(
            $"oEmbed-{provider.provider_name}", "oEmbed-default", metadata, ct).
            ConfigureAwait(false);

        // Render with layout AST with overall metadata.
        var overallHtmlContent = new StringBuilder();
        await layoutNode.RenderAsync(
            text => overallHtmlContent.Append(text), mc, ct).
            ConfigureAwait(false);

        return overallHtmlContent.ToString();
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

        var permaLinkString = (await parameters[0].ReduceExpressionAndFormatAsync(metadata, ct).
            ConfigureAwait(false)).
            Trim();
        if (!Uri.TryCreate(permaLinkString, UriKind.Absolute, out var permaLink))
        {
            throw new ArgumentException(
                $"Invalid oEmbed function argument: URL={permaLinkString}");
        }

        // TODO: cache system
        var providersJson = await Utilities.FetchJsonAsync(oEmbedProviderListUrl, ct).
            ConfigureAwait(false);

        var targetEntries = (await Task.WhenAll(
            providersJson.
                EnumerateArray<oEmbedProvider>().
                Select(provider =>
                    Task.Run(() => provider.endpoints.
                        Select(endPoint => endPoint.schemes.
                            Any(scheme => scheme.IsMatch(permaLinkString)) ?
                                new { provider, endPoint } : null!).
                        Where(targetEntry =>
                            targetEntry != null &&
                            !string.IsNullOrWhiteSpace(targetEntry.endPoint.url)).
                        ToArray()))).
            ConfigureAwait(false)).
            SelectMany(endPointUrls => endPointUrls).
            ToArray();

        // If an `html` value is obtained from oEmbed, that data is used first.
        var secondResults = new List<(oEmbedProvider, oEmbedEndPoint, JObject)>();
        foreach (var targetEntry in targetEntries)
        {
            var requestUrlString = $"{targetEntry.endPoint.url!.Trim()}?url={permaLinkString}";
            try
            {
                var requestUrl = new Uri(requestUrlString, UriKind.Absolute);

                // TODO: cache system
                var metadataJson = await Utilities.FetchJsonAsync(requestUrl, ct).
                    ConfigureAwait(false);

                if (metadataJson is JObject obj)
                {
                    // oEmbed metadata produces `html` data.
                    if (obj.GetValue<string>("html") is { } htmlString &&
                        !string.IsNullOrWhiteSpace(htmlString))
                    {
                        // Accept with sanitized HTML.
                        var sanitizedHtmlString = await Render_oEmbedSanitizedHtmlBodySizeAsync(
                            metadata,
                            targetEntry.provider,
                            targetEntry.endPoint,
                            obj,
                            htmlString,
                            ct).
                            ConfigureAwait(false);
                        return new HtmlContentExpression(sanitizedHtmlString);
                    }
                    else
                    {
                        secondResults.Add((targetEntry.provider, targetEntry.endPoint, obj));
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
        foreach (var (provider, endPoint, metadataJson) in secondResults)
        {
            var overallHtmlContentString = await Render_oEmbedMetadataAsync(
                metadata, provider, endPoint, metadataJson, ct).
                ConfigureAwait(false);
            return new HtmlContentExpression(overallHtmlContentString);
        }

        // TODO: Fetch HTML and render OGP metadata.
        // TODO: Render with layout.

        return new HtmlContentExpression(
            $"<div class='oEmbed-outer'><a class='oEmbed-link' href='{permaLinkString}'>{permaLinkString}</a></div>");
    }
}
