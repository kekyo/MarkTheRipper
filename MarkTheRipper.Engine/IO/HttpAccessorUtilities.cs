/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace MarkTheRipper.IO;

internal static class HttpAccessorUtilities
{
    public static IReadOnlyDictionary<string, string> GetCacheKeyValues(Uri url)
    {
        var kv = url.Query.StartsWith("?") ?
            url.Query.TrimStart('?').
            Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries).
            Select(kv =>
            {
                var splitted = kv.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                return (key: splitted[0], value: splitted.ElementAtOrDefault(1));
            }).
            ToDictionary(kv => kv.key, kv => kv.value)! :
            new Dictionary<string, string>();

        var path = url.LocalPath;
        if (path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries) is { } splitted &&
            splitted.Length >= 2)
        {
            kv.Add("-=@path@=-", string.Join("/", splitted.Take(splitted.Length - 1)));
        }

        return kv;
    }

    public static string CalculateHashPostfix(
        IReadOnlyDictionary<string, string> cacheKeyValues)
    {
        if (cacheKeyValues.Count >= 1)
        {
            using var ms = new MemoryStream();
            var tw = new StreamWriter(ms, Utilities.UTF8);
            tw.NewLine = "\n";
            foreach (var kv in cacheKeyValues.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
            {
                tw.Write(kv.Key);
                tw.Write('=');
                tw.Write(kv.Value);
                tw.Write(',');
            }
            tw.Flush();
            ms.Position = 0;

            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(ms);

            return "_" +
                BitConverter.ToString(hash).
                Replace("-", string.Empty).
                ToLowerInvariant();
        }
        else
        {
            return string.Empty;
        }
    }

    public static (string pathBase, string ext) GetPhysicalPath(
        string cacheBasePath,
        Uri url,
        string defaultExtHint,
        IReadOnlyDictionary<string, string> cacheKeyValues)
    {
        var hashPostfix = CalculateHashPostfix(cacheKeyValues);
        var host = url.IsDefaultPort ? url.Host : $"{url.Host}_{url.Port}";

        var fileNameHint = url.LocalPath.Split('/').LastOrDefault() ??
            "index";
        var ext = Path.GetExtension(fileNameHint);
        if (ext.Length == 0 || ext == ".")
        {
            ext = defaultExtHint;
        }

        var fileName =
            Path.GetFileNameWithoutExtension(fileNameHint) +
            hashPostfix;
        return
            (Path.Combine(cacheBasePath, host, fileName), ext);
    }
}
