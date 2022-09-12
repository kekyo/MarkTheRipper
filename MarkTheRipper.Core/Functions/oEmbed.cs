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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MarkTheRipper.Functions;

// Thanks suggest Ovis oEmbed handling:
// https://github.com/Ovis/BlogGenerator/blob/main/BlogGenerator/ShortCodes/OEmbedShortCodes.cs

internal static class oEmbed
{
    private static readonly Uri oEmbedProviderListUrl =
        new("https://oembed.com/providers.json");

    //////////////////////////////////////////////////////////////////////////////

    private sealed class oEmbedEndPoint : IMetadataEntry
    {
        public readonly string url;
        public readonly Func<string, bool>[] matchers;

        [JsonConstructor]
        public oEmbedEndPoint(
            string? url,
            string?[]? schemes)
        {
            this.url = url?.Trim() ?? string.Empty;
            this.matchers = schemes?.
                Select<string?, Func<string, bool>>(scheme =>
                {
                    if (scheme is { } s)
                    {
                        var r = new Regex(s.Replace("*", ".*"), RegexOptions.Compiled);
                        return r.IsMatch;
                    }
                    else
                    {
                        return _ => false;
                    }
                }).
                ToArray() ??
                InternalUtilities.Empty<Func<string, bool>>();
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
            $"{this.url ?? "(Unknown url)"}, Schemes={this.matchers.Length}";
    }

    private sealed class oEmbedProvider : IMetadataEntry
    {
        public readonly string provider_name;
        public readonly Uri provider_url;
        public readonly oEmbedEndPoint[] endpoints;

        [JsonConstructor]
        public oEmbedProvider(
            string? provider_name,
            string? provider_url,
            oEmbedEndPoint[]? endpoints)
        {
            this.provider_name = provider_name!;
            this.provider_url = provider_url is { } pus &&
                Uri.TryCreate(pus.Trim(), UriKind.Absolute, out var pu) ?
                    pu : null!;
            this.endpoints = endpoints ?? InternalUtilities.Empty<oEmbedEndPoint>();
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
        MetadataContext metadata, string providerName, string endPointUrl, JObject oEmbedMetadataJson)
    {
        var mc = metadata.Spawn();
        mc.SetValue("providerName", providerName);
        mc.SetValue("endPointUrl", endPointUrl);
        foreach (var entry in oEmbedMetadataJson)
        {
            mc.SetValue(entry.Key, entry.Value?.ToObject<string>() ?? string.Empty);
        }
        return mc;
    }

    //////////////////////////////////////////////////////////////////////////////

    private static async ValueTask<string> Render_oEmbedSanitizedHtmlBodySizeAsync(
        MetadataContext metadata,
        string providerName,
        string endPointUrl,
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
            metadata, providerName, endPointUrl, oEmbedMetadataJson);

        // Set patched HTML into metadata context.
        mc.SetValue("contentBody", contentBodyString);

        // Get layout AST (ITextTreeNode).
        // `layout-oEmbed-*.html` ==> `layout-oEmbed-html.html`
        var layoutNode = await MetadataUtilities.GetLayoutAsync(
            $"oEmbed-{providerName}", "oEmbed-html", metadata, ct).
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
        string providerName,
        string endPointUrl,
        JObject oEmbedMetadataJson,
        CancellationToken ct)
    {
        // Will transfer MarkTheRipper metadata from oEmbed metadata.
        var mc = SpawnWith_oEmbedMetadata(
            metadata, providerName, endPointUrl, oEmbedMetadataJson);

        // Get layout AST (ITextTreeNode).
        // `layout-oEmbed-*.html` ==> `layout-oEmbed-default.html`
        var layoutNode = await MetadataUtilities.GetLayoutAsync(
            $"oEmbed-{providerName}", "oEmbed-default", metadata, ct).
            ConfigureAwait(false);

        // Render with layout AST with overall metadata.
        var overallHtmlContent = new StringBuilder();
        await layoutNode.RenderAsync(
            text => overallHtmlContent.Append(text), mc, ct).
            ConfigureAwait(false);

        return overallHtmlContent.ToString();
    }

    //////////////////////////////////////////////////////////////////////////////

    private static async ValueTask<IExpression?> Render_oEmbedAsync(
        MetadataContext metadata,
        string oEmbedTargetUrl,
        CancellationToken ct)
    {
        // TODO: cache system
        var providersJson = await Utilities.FetchJsonAsync(oEmbedProviderListUrl, ct).
            ConfigureAwait(false);

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
                            Any(matcher => matcher(oEmbedTargetUrl)) ?
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
            var requestUrlString = $"{targetEntry.endPoint.url.Trim()}?url={oEmbedTargetUrl}";
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
                            targetEntry.provider.provider_name,
                            targetEntry.endPoint.url,
                            obj,
                            htmlString,
                            ct).
                            ConfigureAwait(false);
                        return new HtmlContentExpression(sanitizedHtmlString);
                    }
                    else
                    {
                        secondResults.Add(
                            (targetEntry.provider.provider_name, targetEntry.endPoint.url, obj));
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
        foreach (var (providerName, endPointUrl, metadataJson) in secondResults)
        {
            var requestUrlString = $"{endPointUrl}?url={oEmbedTargetUrl}";
            try
            {
                var overallHtmlContentString = await Render_oEmbedMetadataAsync(
                    metadata,
                    providerName,
                    endPointUrl,
                    metadataJson,
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
        MetadataContext metadata,
        string oEmbedEndPointUrlString,
        CancellationToken ct)
    {
        try
        {
            var requestUrl = new Uri(oEmbedEndPointUrlString, UriKind.Absolute);

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
                        "Direct",
                        oEmbedEndPointUrlString,
                        obj,
                        htmlString,
                        ct).
                        ConfigureAwait(false);
                    return new HtmlContentExpression(sanitizedHtmlString);
                }
                else
                {
                    // Render with oEmbed metadata and layout when produce oEmbed metadata.
                    var overallHtmlContentString = await Render_oEmbedMetadataAsync(
                        metadata,
                        "Direct",
                        oEmbedEndPointUrlString,
                        obj,
                        ct).
                        ConfigureAwait(false);
                    return new HtmlContentExpression(overallHtmlContentString);
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(
                $"Could not fetch from oEmbed discovery end point: Url={oEmbedEndPointUrlString}, Message={ex.Message}");
        }

        return null;
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

        var mc = metadata.Spawn();
        mc.SetValue("permaLink", permaLink);

        //////////////////////////////////////////////////////////////////
        // Step 1. Automatic resolve using global oEmbed provider list.

        // Render oEmbed from perma link.
        if (await Render_oEmbedAsync(mc, permaLinkString, ct).
            ConfigureAwait(false) is { } result1)
        {
            // Done.
            return result1;
        }

        //////////////////////////////////////////////////////////////////
        // Step 2. Fetch HTML from perma link directly.

        try
        {
            var requestUrl = new Uri(permaLinkString, UriKind.Absolute);

            // TODO: cache system
            var html = await Utilities.FetchHtmlAsync(requestUrl, ct).
                ConfigureAwait(false);

            //////////////////////////////////////////////////////////////////
            // Step 3. Retreive meta tags from HTML.

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
                        mc.SetValue(p, c);
                    }
                    else if (element.GetAttribute("name") is { } name &&
                        name.Trim() is { } n &&
                        !string.IsNullOrWhiteSpace(n))
                    {
                        mc.SetValue(n, c);
                    }
                }
            }

            if (html.Head?.QuerySelector("title") is { } title &&
                title.TextContent.Trim() is { } t &&
                !string.IsNullOrWhiteSpace(t))
            {
                mc.SetValue("title", t);
            }

            //////////////////////////////////////////////////////////////////
            // Step 4. Resolve by oEmbed discover meta tag link.

            // Contains oEmbed meta tags.
            if (html.Head?.QuerySelector("link[type='application/json+oembed']") is { } targetElement &&
                targetElement.GetAttribute("href") is { } hrefString &&
                hrefString.Trim() is { } hs &&
                Uri.TryCreate(hs, UriKind.Absolute, out var href))
            {
                // Render oEmbed from discovered perma link.
                if (await Render_oEmbedDiscoveryAsync(mc, hs, ct).
                    ConfigureAwait(false) is { } result2)
                {
                    // Done.
                    return result2;
                }
            }

            //////////////////////////////////////////////////////////////////
            // Step 5. Give up oEmbed resolving, try to get HTML meta tags.

            // Render with layout.
            // Get layout AST (ITextTreeNode).
            // `layout-oEmbed-fallback-meta.html` ==> `layout-oEmbed-fallback-simple.html`
            var layoutNode = await MetadataUtilities.GetLayoutAsync(
                $"oEmbed-fallback-meta", "oEmbed-fallback-simple", mc, ct).
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
                $"Could not fetch perma link content: Url={permaLinkString}, Message={ex.Message}");
        }

        //////////////////////////////////////////////////////////////////
        // Step 6. Could not fetch any information.

        {
            // Render with layout.
            // Get layout AST (ITextTreeNode).
            // `layout-oEmbed-fallback-meta.html`
            var layoutNode = await MetadataUtilities.GetLayoutAsync(
                $"oEmbed-fallback-simple", null, mc, ct).
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
}
