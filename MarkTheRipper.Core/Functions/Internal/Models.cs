////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using MarkTheRipper.Metadata;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Functions.Internal;

internal sealed class oEmbedEndPoint : IMetadataEntry
{
    public readonly string url;
    public readonly Func<string, bool>[] matchers;

    [JsonConstructor]
    public oEmbedEndPoint(
        string? url,
        string?[]? schemes)
    {
        this.url = url?.Trim() ?? string.Empty;
        matchers = schemes?.
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
            "url" => url,
            _ => null,
        });

    public override string ToString() =>
        $"{url ?? "(Unknown url)"}, Schemes={matchers.Length}";
}

//////////////////////////////////////////////////////////////////////////////

internal sealed class oEmbedProvider : IMetadataEntry
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
            "name" => provider_name,
            "url" => provider_url,
            _ => null,
        });

    public override string ToString() =>
        $"{provider_name}, Url={provider_url}, EndPoints={endpoints.Length}";
}

//////////////////////////////////////////////////////////////////////////////

internal sealed class HtmlMetadata
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

// https://webservices.amazon.com/paapi5/documentation/get-items.html
internal sealed class AmazonPAAPIGetItemsRequest
{
    public readonly string[] ItemIds;
    public readonly string ItemIdType;
    public readonly string[] LanguagesOfPreference;
    public readonly string Marketplace;
    public readonly string PartnerTag;
    public readonly string PartnerType;
    public readonly string[] Resources;

    [JsonConstructor]
    public AmazonPAAPIGetItemsRequest(
        string[] ItemIds, string ItemIdType, string[] LanguagesOfPreference,
        string Marketplace, string PartnerTag, string PartnerType,
        string[] Resources)
    {
        this.ItemIds = ItemIds;
        this.ItemIdType = ItemIdType;
        this.LanguagesOfPreference = LanguagesOfPreference;
        this.Marketplace = Marketplace;
        this.PartnerTag = PartnerTag;
        this.PartnerType = PartnerType;
        this.Resources = Resources;
    }
}

internal readonly struct AmazonPAAPIGetItemsError
{
    public readonly string __type;
    public readonly string Code;
    public readonly string Message;

    [JsonConstructor]
    public AmazonPAAPIGetItemsError(
        string __type, string Code, string Message)
    {
        this.__type = __type;
        this.Code = Code;
        this.Message = Message;
    }
}

internal sealed class AmazonPAAPIGetItemsItemImageDetail
{
    public readonly int Height;
    public readonly Uri URL;
    public readonly int Width;

    [JsonConstructor]
    public AmazonPAAPIGetItemsItemImageDetail(
        int Height,
        string URL,
        int Width)
    {
        this.Height = Height;
        this.URL = InternalUtilities.GetUrl(URL);
        this.Width = Width;
    }
}

internal sealed class AmazonPAAPIGetItemsItemImage
{
    public readonly AmazonPAAPIGetItemsItemImageDetail? Large;

    [JsonConstructor]
    public AmazonPAAPIGetItemsItemImage(AmazonPAAPIGetItemsItemImageDetail? Large) =>
        this.Large = Large;
}

internal sealed class AmazonPAAPIGetItemsItemImages
{
    public readonly AmazonPAAPIGetItemsItemImage? Primary;
    public readonly AmazonPAAPIGetItemsItemImage? Variants;

    [JsonConstructor]
    public AmazonPAAPIGetItemsItemImages(AmazonPAAPIGetItemsItemImage? Variants) =>
        this.Variants = Variants;
}

internal sealed class AmazonPAAPILabelMetadata
{
    public readonly string DisplayValue;
    public readonly string Label;
    public readonly CultureInfo Locale;
    public readonly string? Value;

    [JsonConstructor]
    public AmazonPAAPILabelMetadata(
        string DisplayValue, string Label, string Locale, string? Value)
    {
        this.DisplayValue = DisplayValue;
        this.Label = Label;
        this.Locale = InternalUtilities.GetLocale(Locale);
        this.Value = Value;
    }
}

internal sealed class AmazonPAAPIGetItemsItemInfo
{
    public readonly AmazonPAAPILabelMetadata? Title;
    public readonly AmazonPAAPILabelMetadata? Features;

    [JsonConstructor]
    public AmazonPAAPIGetItemsItemInfo(
        AmazonPAAPILabelMetadata? Title,
        AmazonPAAPILabelMetadata? Features)
    {
        this.Title = Title;
        this.Features = Features;
    }
}

internal sealed class AmazonPAAPIGetItemsPrice
{
    public readonly double Amount;
    public readonly string Currency;
    public readonly string DisplayAmount;

    [JsonConstructor]
    public AmazonPAAPIGetItemsPrice(
        double Amount, string Currency, string DisplayAmount)
    {
        this.Amount = Amount;
        this.Currency = Currency;
        this.DisplayAmount = DisplayAmount;
    }
}

internal readonly struct AmazonPAAPIGetItemsSummary
{
    public readonly AmazonPAAPILabelMetadata? Condition;
    public readonly AmazonPAAPIGetItemsPrice? HighestPrice;
    public readonly AmazonPAAPIGetItemsPrice? LowestPrice;

    [JsonConstructor]
    public AmazonPAAPIGetItemsSummary(
        AmazonPAAPILabelMetadata? Condition,
        AmazonPAAPIGetItemsPrice? HighestPrice,
        AmazonPAAPIGetItemsPrice? LowestPrice)
    {
        this.Condition = Condition;
        this.HighestPrice = HighestPrice;
        this.LowestPrice = LowestPrice;
    }
}

internal sealed class AmazonPAAPIGetItemsOffers
{
    public readonly AmazonPAAPIGetItemsSummary[] Summaries;

    [JsonConstructor]
    public AmazonPAAPIGetItemsOffers(
        AmazonPAAPIGetItemsSummary[] Summaries) =>
        this.Summaries = Summaries;
}

internal readonly struct AmazonPAAPIGetItemsItemResult
{
    public readonly string? ASIN;
    public readonly Uri? DetailPageURL;
    public readonly AmazonPAAPIGetItemsItemImages? Images;
    public readonly AmazonPAAPIGetItemsItemInfo? ItemInfo;
    public readonly AmazonPAAPIGetItemsOffers? Offers;
    public readonly string? ParentASIN;

    [JsonConstructor]
    public AmazonPAAPIGetItemsItemResult(
        string? ASIN,
        Uri? DetailPageURL,
        AmazonPAAPIGetItemsItemImages? Images,
        AmazonPAAPIGetItemsItemInfo? ItemInfo,
        AmazonPAAPIGetItemsOffers? Offers,
        string? ParentASIN)
    {
        this.ASIN = ASIN;
        this.DetailPageURL = DetailPageURL;
        this.Images = Images;
        this.ItemInfo = ItemInfo;
        this.Offers = Offers;
        this.ParentASIN = ParentASIN;
    }
}

internal sealed class AmazonPAAPIGetItemsItemResults
{
    public readonly AmazonPAAPIGetItemsItemResult[] Items;

    [JsonConstructor]
    public AmazonPAAPIGetItemsItemResults(
        AmazonPAAPIGetItemsItemResult[] Items) =>
        this.Items = Items;
}

internal sealed class AmazonPAAPIGetItemsResponse
{
    public readonly AmazonPAAPIGetItemsError[] Errors;
    public readonly AmazonPAAPIGetItemsItemResults? ItemResults;

    [JsonConstructor]
    public AmazonPAAPIGetItemsResponse(
        AmazonPAAPIGetItemsError[] Errors,
        AmazonPAAPIGetItemsItemResults? ItemResults)
    {
        this.Errors = Errors;
        this.ItemResults = ItemResults;
    }
}
