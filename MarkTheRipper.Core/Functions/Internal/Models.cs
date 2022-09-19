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
