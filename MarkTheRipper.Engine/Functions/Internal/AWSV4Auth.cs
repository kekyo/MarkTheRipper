////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MarkTheRipper.Functions.Internal;

// https://webservices.amazon.com/paapi5/documentation/without-sdk.html

internal sealed class AWSV4Auth
{
    public sealed class Builder
    {
        internal DateTimeOffset date = DateTimeOffset.Now;
        internal string awsAccessKey;
        internal string awsSecretKey;
        internal string path = null!;
        internal string region = null!;
        internal string service = null!;
        internal string httpMethodName = null!;
        internal SortedDictionary<string, string> headers =
            new(StringComparer.OrdinalIgnoreCase);
        internal string payload = null!;

        public Builder(string awsAccessKey, string awsSecretKey)
        {
            this.awsAccessKey = awsAccessKey;
            this.awsSecretKey = awsSecretKey;
        }

        public Builder Date(DateTimeOffset date)
        {
            this.date = date;
            return this;
        }

        public Builder Path(string path)
        {
            this.path = path;
            return this;
        }

        public Builder Region(string region)
        {
            this.region = region;
            return this;
        }

        public Builder Service(string service)
        {
            this.service = service;
            return this;
        }

        public Builder HttpMethodName(string httpMethodName)
        {
            this.httpMethodName = httpMethodName;
            return this;
        }

        public Builder Headers(SortedDictionary<string, string> headers)
        {
            this.headers = headers;
            return this;
        }

        public Builder Payload(string payload)
        {
            this.payload = payload;
            return this;
        }

        public AWSV4Auth Build() =>
            new AWSV4Auth(this);
    }

    private static readonly string hmacAlgorithm = "AWS4-HMAC-SHA256";
    private static readonly string aws4Request = "aws4_request";

    private readonly DateTimeOffset date;
    private readonly string awsAccessKey;
    private readonly string awsSecretKey;
    private readonly string path;
    private readonly string region;
    private readonly string service;
    private readonly string httpMethodName;
    private readonly SortedDictionary<string, string> headers;
    private readonly string xAmzDate;
    private readonly string currentDate;

    private string payload;
    private string? signedHeaders;

    private AWSV4Auth(Builder builder)
    {
        this.date = builder.date;
        this.awsAccessKey = builder.awsAccessKey;
        this.awsSecretKey = builder.awsSecretKey;
        this.path = builder.path;
        this.region = builder.region;
        this.service = builder.service;
        this.httpMethodName = builder.httpMethodName;
        this.headers = builder.headers;
        this.payload = builder.payload;
        this.xAmzDate = this.GetTimeStamp();
        this.currentDate = this.GetDate();
    }

    public Dictionary<string, string> GetHeaders()
    {
        this.headers["X-Amz-Date"] = this.xAmzDate;
        var headers = new Dictionary<string, string>(this.headers);

        var canonicalURL = this.PrepareCanonicalRequest();
        var stringToSign = this.PrepareStringToSign(canonicalURL);
        var signature = this.CalculateSignature(stringToSign);

        headers["Authorization"] = this.BuildAuthorizationString(signature);
        return headers;
    }

    private string PrepareCanonicalRequest()
    {
        var canonicalUrl = new StringBuilder();
        canonicalUrl.Append(httpMethodName).Append('\n');
        canonicalUrl.Append(path).Append('\n').Append('\n');

        var signedHeaderBuilder = new StringBuilder();
        foreach (var entry in this.headers)
        {
            var key = entry.Key;
            var value = entry.Value;
            signedHeaderBuilder.Append(key.ToLowerInvariant()).Append(';');
            canonicalUrl.Append(key.ToLowerInvariant()).Append(':').Append(value).Append('\n');
        }
        canonicalUrl.Append('\n');

        this.signedHeaders = signedHeaderBuilder.ToString().
            Substring(0, signedHeaderBuilder.Length - 1);
        canonicalUrl.Append(this.signedHeaders).Append('\n');

        this.payload = this.payload == null ? "" : this.payload;
        canonicalUrl.Append(this.ToHex(this.payload));

        return canonicalUrl.ToString();
    }

    private string PrepareStringToSign(string canonicalUrl)
    {
        var sb = new StringBuilder(hmacAlgorithm).Append('\n');
        sb.Append(this.xAmzDate).Append('\n');
        sb.Append(this.currentDate).Append('/').
            Append(this.region).Append('/').
            Append(this.service).Append('/').
            Append(aws4Request).Append('\n');
        sb.Append(this.ToHex(canonicalUrl));
        return sb.ToString();
    }

    private string CalculateSignature(string stringToSign)
    {
        var signatureKey = this.GetSignatureKey(awsSecretKey, currentDate, region, service);
        var signature = this.ApplyHMACSha256(signatureKey, stringToSign);
        return this.BytesToHex(signature);
    }

    private string BuildAuthorizationString(string signature)
    {
        var sb = new StringBuilder(hmacAlgorithm).Append(' ');
        sb.Append("Credential=").Append(awsAccessKey).Append('/').
            Append(this.GetDate()).Append('/').
            Append(region).Append('/').
            Append(service).Append('/').
            Append(aws4Request).Append(' ');
        sb.Append("Signature=").Append(signature);
        return sb.ToString();
    }

    private string BytesToHex(byte[] bytes)
    {
        var sb = new StringBuilder(BitConverter.ToString(bytes));
        sb.Replace("-", string.Empty);
        return sb.ToString().ToLowerInvariant();
    }

    private string ToHex(string data)
    {
        var dataBytes = Utilities.UTF8.GetBytes(data);

        using var messageDigest = SHA256.Create();
        messageDigest.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
        return this.BytesToHex(messageDigest.Hash!);
    }

    private byte[] ApplyHMACSha256(byte[] key, string data)
    {
        var dataBytes = Utilities.UTF8.GetBytes(data);

        using var mac = new HMACSHA256();
        mac.Key = key;
        mac.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
        return mac.Hash!;
    }

    private byte[] GetSignatureKey(
        string key, string date, string regionName, string serviceName)
    {
        var kSecret = Utilities.UTF8.GetBytes("AWS4" + key);
        var kDate = this.ApplyHMACSha256(kSecret, date);
        var kRegion = this.ApplyHMACSha256(kDate, regionName);
        var kService = this.ApplyHMACSha256(kRegion, serviceName);
        var kSigning = this.ApplyHMACSha256(kService, aws4Request);
        return kSigning;
    }

    private string GetTimeStamp()
    {
        var now = this.date.UtcDateTime;
        return $"{now:yyyyMMdd}T{now:HHmmss}Z";
    }

    private string GetDate() =>
        this.date.UtcDateTime.ToString("yyyyMMdd");
}
