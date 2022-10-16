/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper;

public static class Utilities
{
    private static readonly Dictionary<string, CultureInfo> cultures =
        new(StringComparer.InvariantCultureIgnoreCase);

    public static readonly char[] PathSeparators = new[]
    {
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar,
    };

    public static readonly Encoding UTF8 =
        new UTF8Encoding(false);   // No BOM

    ///////////////////////////////////////////////////////////////////////////////////

    public static string GetDirectoryPath(string path) =>
        Path.GetDirectoryName(path) switch
        {
            // Not accurate in Windows, but a compromise...
            null => Path.DirectorySeparatorChar.ToString(),
            "" => string.Empty,
            var dp => dp,
        };

    ///////////////////////////////////////////////////////////////////////////////////

    public static JsonSerializer GetDefaultJsonSerializer()
    {
        var defaultNamingStrategy = new CamelCaseNamingStrategy();
        var serializer = new JsonSerializer
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            DateTimeZoneHandling = DateTimeZoneHandling.Local,
            NullValueHandling = NullValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ContractResolver = new DefaultContractResolver { NamingStrategy = defaultNamingStrategy, },
        };
        serializer.Converters.Add(new StringEnumConverter(defaultNamingStrategy));
        return serializer;
    }

    public static ValueTask<JToken> DeserializeJsonAsync(
        this JsonSerializer serializer, Stream stream, CancellationToken ct)
    {
        var tr = new StreamReader(stream, UTF8);
        var jr = new JsonTextReader(tr);
        return new ValueTask<JToken>(JToken.ReadFromAsync(jr, null, ct));
    }

    public static IEnumerable<TValue> EnumerateArray<TValue>(
        this JToken token, JsonSerializer? serializer = default)
    {
        serializer ??= InternalUtilities.DefaultJsonSerializer;

        if (token is JArray array)
        {
            foreach (var item in array)
            {
                yield return item.ToObject<TValue>(serializer)!;
            }
        }
    }

    public static TValue GetValue<TValue>(
        this JToken token, string memberName, TValue defaultValue = default!,
        JsonSerializer? serializer = default)
    {
        serializer ??= InternalUtilities.DefaultJsonSerializer;

        if (token is JObject obj)
        {
            if (obj.TryGetValue(memberName, out var value))
            {
                return value.ToObject<TValue>(serializer) ??
                    defaultValue;
            }
        }

        return defaultValue;
    }

    ///////////////////////////////////////////////////////////////////////////////////

    public static string EscapeHtmlString(string htmlString, bool escapeSpaces = false)
    {
        var sb = new StringBuilder(
            htmlString, htmlString.Length + 32);
        sb.Replace("&", "&amp;");
        sb.Replace("<", "&lt;");
        sb.Replace(">", "&gt;");
        sb.Replace("\"", "&quot;");
        sb.Replace("'", "&#39;");
        if (escapeSpaces)
        {
            sb.Replace(" ", "&nbsp;");
        }
        return sb.ToString();
    }

    public static string UnescapeJavascriptString(string javascriptString)
    {
        static string? GetToken(string str, ref int index, int length)
        {
            var sb = new StringBuilder();
            while (length >= 1 && index < str.Length)
            {
                var ch = str[index++];
                sb.Append(ch);
                length--;
            }
            return length == 0 ? sb.ToString() : null;
        }

        var sb = new StringBuilder(javascriptString.Length);
        var index = 0;
        while (index < javascriptString.Length)
        {
            var ch = javascriptString[index++];
            if (ch == '\\')
            {
                if (index < javascriptString.Length)
                {
                    var ch2 = javascriptString[index++];
                    switch (ch2)
                    {
                        case 'b':
                            sb.Append((char)8);
                            break;
                        case 't':
                            sb.Append((char)9);
                            break;
                        case 'n':
                            sb.Append((char)10);
                            break;
                        case 'v':
                            sb.Append((char)11);
                            break;
                        case 'f':
                            sb.Append((char)12);
                            break;
                        case 'r':
                            sb.Append((char)13);
                            break;
                        case 'x':
                            if (GetToken(javascriptString, ref index, 2) is { } hexString &&
                                int.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                            {
                                sb.Append((char)hex);
                            }
                            break;
                        case 'u':
                            if (GetToken(javascriptString, ref index, 4) is { } ucString &&
                                int.TryParse(ucString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var uc))
                            {
                                sb.Append((char)uc);
                            }
                            break;
                        default:
                            sb.Append(ch2);
                            break;
                    };
                }
            }
            else
            {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }

    ///////////////////////////////////////////////////////////////////////////////////

    public static CultureInfo GetLocale(string? localeString)
    {
        if (!string.IsNullOrWhiteSpace(localeString))
        {
            lock (cultures)
            {
                if (!cultures.TryGetValue(localeString!, out var culture))
                {
                    try
                    {
                        culture = CultureInfo.GetCultureInfo(localeString);
                    }
                    catch
                    {
                        culture = CultureInfo.InvariantCulture;
                    }

                    cultures[localeString!] = culture;
                }
                return culture;
            }
        }
        else
        {
            return CultureInfo.InvariantCulture;
        }
    }
}
