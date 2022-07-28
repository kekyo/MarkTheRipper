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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Internal;

internal readonly struct MarkdownContent
{
    public readonly string Body;
    public readonly IReadOnlyDictionary<string, object?> Metadata;

    public MarkdownContent(IReadOnlyDictionary<string, object?> metadata, string body)
    {
        Metadata = metadata;
        Body = body;
    }
}

internal static class Parser
{
    private sealed class TemplateParseContext
    {
        public readonly string TemplatePath;
        public readonly TextReader Reader;
        public readonly CancellationToken Token;
        public readonly StringBuilder OriginalText;

        public TemplateParseContext(
            string templatePath,
            TextReader reader,
            CancellationToken token)
        {
            this.TemplatePath = templatePath;
            this.Reader = reader;
            this.Token = token;
            this.OriginalText = new();
        }

        public TemplateParseContext(TemplateParseContext context)
        {
            this.TemplatePath = context.TemplatePath;
            this.Reader = context.Reader;
            this.Token = context.Token;
            this.OriginalText = context.OriginalText;
        }
    }

    private static async ValueTask<TemplateNode[]> ParseTemplateAsync(
        TemplateParseContext context)
    {
        var nestedIterations = new Stack<(string iteratorKeyName, List<TemplateNode> nodes)>();

        var nodes = new List<TemplateNode>();
        var buffer = new StringBuilder();

        while (true)
        {
            var line = await context.Reader.ReadLineAsync().
                WithCancellation(context.Token).
                ConfigureAwait(false);
            if (line == null)
            {
                break;
            }

            context.OriginalText.AppendLine(line);

            var startIndex = 0;
            while (startIndex < line.Length)
            {
                var openIndex = line.IndexOf('{', startIndex);
                if (openIndex == -1)
                {
                    buffer.AppendLine(line.Substring(startIndex));
                    break;
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
                        $"Could not find close bracket. Template={context.TemplatePath}");
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
                            $"`foreach` parameter required. Template={context.TemplatePath}");
                    }

                    nestedIterations.Push((parameter!, nodes));
                    nodes = new();
                }
                // Special case: iterator end
                else if (keyName == "/")
                {
                    if (!string.IsNullOrWhiteSpace(parameter))
                    {
                        throw new FormatException(
                            $"Invalid iterator-end parameter. Template={context.TemplatePath}");
                    }
                    else if (nestedIterations.Count <= 0)
                    {
                        throw new FormatException(
                            $"Could not find iterator-begin. Template={context.TemplatePath}");
                    }

                    var childNodes = nodes.ToArray();
                    var (iteratorKeyName, lastNodes) = nestedIterations.Pop();

                    nodes = lastNodes;
                    nodes.Add(new ForEachNode(iteratorKeyName, childNodes));
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

        return nodes.ToArray();
    }

    public static async ValueTask<RootTemplateNode> ParseTemplateAsync(
        string templatePath, TextReader templateReader, CancellationToken ct)
    {
        var context = new TemplateParseContext(
            templatePath, templateReader, ct);

        var nodes = await ParseTemplateAsync(context).
            ConfigureAwait(false);

        return new RootTemplateNode(
            context.OriginalText.ToString(), nodes);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    // TODO: rewrite with LL.
    private static readonly char[] separators = new[] { ',' };
    private static object? ParseYamlLikeString(string text)
    {
        // `title: "Hello world"`
        if (text.StartsWith("\"") || text.EndsWith("\""))
        {
            // string
            return text.Trim('"');
        }
        // `title: 'Hello world'`
        else if (text.StartsWith("'") || text.EndsWith("'"))
        {
            // string
            return text.Trim('\'');
        }
        // `title: [Hello world]`
        // * Assume yaml array-like: `tags: [aaa, bbb]` --> `tags: aaa, bbb`
        else if (text.StartsWith("[") && text.EndsWith("]"))
        {
            return text.TrimStart('[').TrimEnd(']').
                Split(separators).
                Select(value => ParseYamlLikeString(value.Trim())).
                ToArray();
        }
        else if (long.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var lv))
        {
            // long
            return lv;
        }
        else if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var dv))
        {
            // double
            return dv;
        }
        else if (bool.TryParse(text, out var bv))
        {
            // bool
            return bv;
        }
        else if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dtv))
        {
            // DateTimeOffset
            return dtv;
        }
        else if (Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out var uv))
        {
            // Uri
            return uv;
        }
        else
        {
            return text;
        }
    }

    public static async ValueTask<MarkdownContent> ParseEntireMarkdownAsync(
        TextReader markdownReader, CancellationToken ct)
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
                    $"Could not find markdown header.");
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                if (line.Trim().StartsWith("---"))
                {
                    break;
                }
            }
        }

        var metadata = new Dictionary<string, object?>();

        // `title: Hello world`
        while (true)
        {
            var line = await markdownReader.ReadLineAsync().
                WithCancellation(ct).
                ConfigureAwait(false);
            if (line == null)
            {
                throw new FormatException();
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                var keyIndex = line.IndexOf(':');
                if (keyIndex >= 1)
                {
                    var key = line.Substring(0, keyIndex).Trim();
                    var valueText = line.Substring(keyIndex + 1).Trim();
                    var value = ParseYamlLikeString(valueText);

                    metadata[key] = value;
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
                        throw new FormatException();
                    }
                }
            }
        }

        var sb = new StringBuilder();
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
                sb.AppendLine(line);
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
            sb.AppendLine(line);
        }

        return new(metadata, sb.ToString());
    }
}
