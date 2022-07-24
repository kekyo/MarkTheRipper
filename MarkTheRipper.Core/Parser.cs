﻿/////////////////////////////////////////////////////////////////////////////////////
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper;

internal readonly struct MarkdownContent
{
    public readonly string Body;
    public readonly IReadOnlyDictionary<string, object?> Metadata;

    public MarkdownContent(IReadOnlyDictionary<string, object?> metadata, string body)
    {
        this.Metadata = metadata;
        this.Body = body;
    }
}

internal static class Parser
{
    private sealed class TemplateParseContext
    {
        public readonly string TemplateName;
        public readonly TextReader Reader;
        public readonly CancellationToken Token;
        public readonly StringBuilder OriginalText;

        public TemplateParseContext(
            string templateName,
            TextReader reader,
            CancellationToken token)
        {
            this.TemplateName = templateName;
            this.Reader = reader;
            this.Token = token;
            this.OriginalText = new();
        }

        public TemplateParseContext(TemplateParseContext context)
        {
            this.TemplateName = context.TemplateName;
            this.Reader = context.Reader;
            this.Token = context.Token;
            this.OriginalText = context.OriginalText;
        }
    }

    private static async ValueTask<TemplateNode[]> ParseTemplateAsync(
        TemplateParseContext context)
    {
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
                else
                {
                    buffer.Append(line.Substring(0, openIndex));
                    if (buffer.Length >= 1)
                    {
                        nodes.Add(new TextNode(buffer.ToString()));
                        buffer.Clear();
                    }

                    var closeIndex = line.IndexOf('}', openIndex + 1);
                    if (closeIndex == -1)
                    {
                        throw new FormatException(
                            $"Could not find close bracket. Template={context.TemplateName}");
                    }
                    else
                    {
                        startIndex = closeIndex + 1;

                        var metadataWords = line.Substring(
                            openIndex + 1, closeIndex - openIndex - 1);
                        var metadataWordSplitterIndex = metadataWords.IndexOf(':');

                        var keyName = (metadataWordSplitterIndex >= 0) ?
                            metadataWords.Substring(0, metadataWordSplitterIndex) : metadataWords;
                        var parameter = (metadataWordSplitterIndex >= 0) ?
                            metadataWords.Substring(metadataWordSplitterIndex + 1) : null;

                        // Special case: foreach
                        if (StringComparer.OrdinalIgnoreCase.Equals("foreach", keyName))
                        {
                            if (string.IsNullOrWhiteSpace(parameter))
                            {
                                throw new FormatException(
                                    $"`foreach` parameter required. Template={context.TemplateName}");
                            }
                            else
                            {
                                var childContext = new TemplateParseContext(context);

                                var childNodes = await ParseTemplateAsync(childContext).
                                    ConfigureAwait(false);

                                nodes.Add(new ForEachNode(parameter!, childNodes));
                            }
                        }
                        else
                        {
                            nodes.Add(new ReplacerNode(keyName, parameter));
                        }
                    }
                }
            }
        }

        if (buffer.Length >= 1)
        {
            nodes.Add(new TextNode(buffer.ToString()));
        }

        return nodes.ToArray();
    }

    public static async ValueTask<Template> ParseTemplateAsync(
        string templateName, TextReader templateReader, CancellationToken ct)
    {
        var context = new TemplateParseContext(
            templateName, templateReader, ct);

        var nodes = await ParseTemplateAsync(context).
            ConfigureAwait(false);

        return new Template(
            context.OriginalText.ToString(), nodes);
    }

    public static async ValueTask<MarkdownContent> LoadMarkdownContentAsync(
        TextReader tr, CancellationToken ct)
    {
        // `---`
        while (true)
        {
            var line = await tr.ReadLineAsync().
                WithCancellation(ct).
                ConfigureAwait(false);
            if (line == null)
            {
                throw new FormatException();
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                if (line.Trim().StartsWith("---"))
                {
                    break;
                }
            }
        }

        var metadata = new Dictionary<string, object?>(
            StringComparer.OrdinalIgnoreCase);

        // `title: Hello world`
        while (true)
        {
            var line = await tr.ReadLineAsync().
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
                    var value = line.Substring(keyIndex + 1).Trim();

                    // `title: "Hello world"`
                    if (value.StartsWith("\""))
                    {
                        value = value.Trim('"');
                    }
                    // `title: 'Hello world'`
                    else if (value.StartsWith("'"))
                    {
                        value = value.Trim('\'');
                    }
                    // `title: [Hello world]`
                    // * Assume JavaScript array-like: `tags: [aaa, bbb]` --> `tags: aaa, bbb`
                    else if (value.StartsWith("["))
                    {
                        value = value.TrimStart('[').TrimEnd(']');
                    }

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
            var line = await tr.ReadLineAsync().
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
            var line = await tr.ReadLineAsync().
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

    public static ValueTask RenderAsync(
        Template template,
        IReadOnlyDictionary<string, object?> baseMetadata,
        IReadOnlyDictionary<string, object?> metadata,
        string contentBody,
        Func<string, CancellationToken, ValueTask> writer,
        CancellationToken ct)
    {
        string? GetMetadata(string keyName, string? parameter) =>
            keyName.ToLowerInvariant() switch
            {
                "contentbody" => contentBody,
                _ => metadata.TryGetValue(keyName, out var value) ?
                    value?.ToString() :
                    baseMetadata.TryGetValue(keyName, out var baseValue) ?
                        baseValue?.ToString() :
                        null,
            };

        return template.RenderAsync(writer, GetMetadata, ct);
    }
}
