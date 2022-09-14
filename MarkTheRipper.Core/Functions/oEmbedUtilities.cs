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
using MarkTheRipper.Internal;
using MarkTheRipper.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions;

internal static class oEmbedUtilities
{
    public sealed class oEmbedEndPoint : IMetadataEntry
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
                    if (scheme != null)
                    {
                        var sb = new StringBuilder(scheme);
                        sb.Replace(".", "\\.");
                        sb.Replace("?", "\\?");
                        sb.Replace("+", "\\+");
                        sb.Replace("*", ".*");
                        var r = new Regex(sb.ToString(), RegexOptions.Compiled);
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

    //////////////////////////////////////////////////////////////////////////////

    public sealed class oEmbedProvider : IMetadataEntry
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

    public sealed class HtmlMetadata
    {
        public string? SiteName;
        public string? Title;
        public string? AltTitle;
        public string? Author;
        public string? Description;
        public string? Type;
        public Uri? ImageUrl;
    }

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
}
