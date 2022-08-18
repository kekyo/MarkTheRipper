/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Metadata;
using MarkTheRipper.Template;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Internal;

internal static class Parser
{
    public static async ValueTask<RootTemplateNode> ParseTemplateAsync(
        string templateName,
        TextReader templateReader,
        CancellationToken ct)
    {
        var nestedIterations =
            new Stack<(string[] iteratorArguments, List<ITemplateNode> nodes)>();

        var originalText = new StringBuilder();
        var nodes = new List<ITemplateNode>();
        var buffer = new StringBuilder();

        while (true)
        {
            var line = await templateReader.ReadLineAsync().
                WithCancellation(ct).
                ConfigureAwait(false);
            if (line == null)
            {
                break;
            }

            originalText.AppendLine(line);

            var startIndex = 0;
            while (startIndex < line.Length)
            {
                var openIndex = line.IndexOf('{', startIndex);
                if (openIndex == -1)
                {
                    var closeIndex2 = line.IndexOf('}', startIndex);
                    if (closeIndex2 == -1)
                    {
                        buffer.AppendLine(line.Substring(startIndex));
                        break;
                    }

                    if ((closeIndex2 + 1) < line.Length &&
                        line[closeIndex2 + 1] == '}')
                    {
                        buffer.Append(line.Substring(startIndex, closeIndex2 - startIndex + 1));
                        startIndex = closeIndex2 + 2;
                        continue;
                    }

                    throw new FormatException(
                        $"Could not find open bracket. Template={templateName}");
                }

                if ((openIndex + 1) < line.Length &&
                    line[openIndex + 1] == '{')
                {
                    buffer.Append(line.Substring(startIndex, openIndex - startIndex + 1));
                    startIndex = openIndex + 2;
                    continue;
                }

                buffer.Append(line.Substring(startIndex, openIndex - startIndex));
                if (buffer.Length >= 1)
                {
                    nodes.Add(new TextNode(buffer.ToString()));
                    buffer.Clear();
                }

                var closeIndex = line.IndexOf('}', openIndex + 1);
                if (closeIndex == -1)
                {
                    throw new FormatException(
                        $"Could not find close bracket. Template={templateName}");
                }

                startIndex = closeIndex + 1;

                var metadataWords = line.Substring(
                    openIndex + 1, closeIndex - openIndex - 1);
                var metadataWordSplitterIndex = metadataWords.IndexOf(':');

                var keyName = metadataWordSplitterIndex >= 0 ?
                    metadataWords.Substring(0, metadataWordSplitterIndex) : metadataWords;
                var parameter = metadataWordSplitterIndex >= 0 ?
                    metadataWords.Substring(metadataWordSplitterIndex + 1) : null;

                // Special case: iterator begin
                if (keyName == "foreach")
                {
                    if (string.IsNullOrWhiteSpace(parameter))
                    {
                        throw new FormatException(
                            $"`foreach` parameter required. Template={templateName}");
                    }

                    var iteratorArguments =
                        parameter!.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    nestedIterations.Push((iteratorArguments, nodes));
                    nodes = new();
                }
                // Special case: iterator end
                else if (keyName == "/")
                {
                    if (!string.IsNullOrWhiteSpace(parameter))
                    {
                        throw new FormatException(
                            $"Invalid iterator-end parameter. Template={templateName}");
                    }
                    else if (nestedIterations.Count <= 0)
                    {
                        throw new FormatException(
                            $"Could not find iterator-begin. Template={templateName}");
                    }

                    var childNodes = nodes.ToArray();
                    var (iteratorArguments, lastNodes) = nestedIterations.Pop();

                    var iteratorKeyName =
                        iteratorArguments[0];
                    var iteratorBoundName =
                        iteratorArguments.ElementAtOrDefault(1) ?? "item";

                    nodes = lastNodes;
                    nodes.Add(new ForEachNode(iteratorKeyName, iteratorBoundName, childNodes));
                }
                else
                {
                    nodes.Add(new ReplacerNode(keyName, parameter));
                }
            }
        }

        if (buffer.Length >= 1)
        {
            nodes.Add(new TextNode(buffer.ToString()));
        }

        return new RootTemplateNode(
           templateName,
           originalText.ToString(),
           nodes.ToArray());
    }

    ///////////////////////////////////////////////////////////////////////////////////

    // TODO: rewrite with LL.
    private enum ParseTypes
    {
        AutoDetect,
        StringOnly,
        DateOnly,
    }
    private enum ListTypes
    {
        Ignore,
        AcceptOnce,
        Accept,
    }
    private static readonly char[] separators = new[] { ',' };
    private static object? ParseYamlLikeString<TResult>(
        string text,
        Func<string, TResult?, TResult?>? parserOverride,
        ParseTypes parseType,
        ListTypes listType,
        TResult? previousValue)
    {
        // `title: [Hello world]`
        // * Assume yaml array-like: `tags: [aaa, bbb]` --> `tags: aaa, bbb`
        if (
            listType != ListTypes.Ignore &&
            text.StartsWith("[") &&
            text.EndsWith("]"))
        {
            return text.Substring(1, text.Length - 2).
                Split(separators).
                Aggregate(
                    (results: new List<TResult?>(), last: previousValue),
                    (agg, v) =>
                    {
                        var result = (TResult?)ParseYamlLikeString(
                            v.Trim(),
                            parserOverride,
                            parseType,
                            listType == ListTypes.AcceptOnce ?
                                ListTypes.Ignore : ListTypes.Accept,
                            agg.last);
                        agg.results.Add(result);
                        return (agg.results, result);
                    }).
                    results.ToArray();
        }

        // `title: "Hello world"`
        string? unquoted = null;
        if (text.StartsWith("\"") || text.EndsWith("\""))
        {
            // string
            unquoted = text.Substring(1, text.Length - 2);
        }
        // `title: 'Hello world'`
        else if (text.StartsWith("'") || text.EndsWith("'"))
        {
            // string
            unquoted = text.Substring(1, text.Length - 2);
        }

        // Invoke parser override function.
        if (parserOverride != null &&
            parserOverride.Invoke(unquoted ?? text, previousValue) is { } pov)
        {
            return pov;
        }

        // Quoted: Result is string.
        if (unquoted != null)
        {
            return unquoted;
        }

        // Automatic detection: long, double, bool, DateTimeOffset and Uri.
        if (parseType != ParseTypes.StringOnly)
        {
            if (parseType == ParseTypes.AutoDetect)
            {
                if (long.TryParse(
                    text,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var lv))
                {
                    // long
                    return lv;
                }
                else if (double.TryParse(
                    text,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var dv))
                {
                    // double
                    return dv;
                }
                else if (bool.TryParse(text, out var bv))
                {
                    // bool
                    return bv;
                }
            }
            
            if (DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var dtv))
            {
                // DateTimeOffset
                return dtv;
            }
            
            if (parseType == ParseTypes.AutoDetect)
            {
                if (Uri.TryCreate(
                    text,
                    UriKind.RelativeOrAbsolute,
                    out var uv))
                {
                    // Uri
                    return uv;
                }
            }
        }

        return unquoted ?? text;
    }

    public static async ValueTask<Dictionary<string, object?>> ParseMarkdownHeaderAsync(
        PathEntry relativeContentPathHint,
        TextReader markdownReader,
        CancellationToken ct)
    {
        // `---`
        while (true)
        {
            var line = await markdownReader.ReadLineAsync().
                WithCancellation(ct).
                ConfigureAwait(false);
            if (line == null)
            {
                throw new FormatException(
                    $"Could not find any markdown header: {relativeContentPathHint}");
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                if (line.Trim().StartsWith("---"))
                {
                    break;
                }
            }
        }

        var markdownMetadata = new Dictionary<string, object?>();

        // `title: Hello world`
        while (true)
        {
            var line = await markdownReader.ReadLineAsync().
                WithCancellation(ct).
                ConfigureAwait(false);
            if (line == null)
            {
                throw new FormatException(
                    $"Could not find any markdown header: Path={relativeContentPathHint}");
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                var keyIndex = line.IndexOf(':');
                if (keyIndex >= 1)
                {
                    var key = line.Substring(0, keyIndex).Trim();
                    var valueText = line.Substring(keyIndex + 1).Trim();
                    var value = key switch
                    {
                        "title" => ParseYamlLikeString(
                            valueText,
                            null,
                            ParseTypes.StringOnly,
                            ListTypes.Ignore,
                            default(string)),
                        "author" => ParseYamlLikeString(
                            valueText,
                            null,
                            ParseTypes.StringOnly,
                            ListTypes.AcceptOnce,
                            default(object)),
                        "template" => ParseYamlLikeString(
                            valueText,
                            (text, _) => new PartialTemplateEntry(text),
                            ParseTypes.StringOnly,
                            ListTypes.Ignore,
                            default(PartialTemplateEntry)),
                        "category" => ParseYamlLikeString(
                            valueText,
                            (text, previous) => new PartialCategoryEntry(text, previous),
                            ParseTypes.StringOnly,
                            ListTypes.AcceptOnce,
                            new PartialCategoryEntry()) switch
                            {
                                PartialCategoryEntry[] entries => entries.LastOrDefault(),
                                var r => r,
                            },
                        "tags" => ParseYamlLikeString(
                            valueText,
                            (text, _) => new PartialTagEntry(text),
                            ParseTypes.StringOnly,
                            ListTypes.AcceptOnce,
                            default(PartialTagEntry)),
                        "date" => ParseYamlLikeString(
                            valueText,
                            null,
                            ParseTypes.DateOnly,
                            ListTypes.Ignore,
                            default(object)),
                        _ => ParseYamlLikeString(
                            valueText,
                            null,
                            ParseTypes.AutoDetect,
                            ListTypes.Accept,
                            default(object)),
                    };

                    if (value != null)
                    {
                        markdownMetadata[key] = value;
                    }
                }
                else
                {
                    // `---`
                    if (line.Trim().StartsWith("---"))
                    {
                        break;
                    }
                    else
                    {
                        throw new FormatException(
                            $"Could not find any markdown header: Path={relativeContentPathHint}");
                    }
                }
            }
        }

        return markdownMetadata;
    }

    public static async ValueTask<(Dictionary<string, object?> markdownMetadata, string markdownBody)> ParseMarkdownBodyAsync(
        PathEntry relativeContentPathHint,
        TextReader markdownReader,
        CancellationToken ct)
    {
        var markdownMetadata = await ParseMarkdownHeaderAsync(
            relativeContentPathHint, markdownReader, ct);

        var markdownBody = new StringBuilder();
        while (true)
        {
            var line = await markdownReader.ReadLineAsync().
                WithCancellation(ct).
                ConfigureAwait(false);
            if (line == null)
            {
                break;
            }

            // Skip empty lines, will detect start of body
            if (!string.IsNullOrWhiteSpace(line))
            {
                markdownBody.AppendLine(line);
                break;
            }
        }

        while (true)
        {
            var line = await markdownReader.ReadLineAsync().
                WithCancellation(ct).
                ConfigureAwait(false);

            // EOF
            if (line == null)
            {
                break;
            }

            // (Sanitizing EOL)
            markdownBody.AppendLine(line);
        }

        return (markdownMetadata, markdownBody.ToString());
    }
}
