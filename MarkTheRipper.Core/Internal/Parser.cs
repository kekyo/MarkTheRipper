﻿/////////////////////////////////////////////////////////////////////////////////////
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

internal enum ListTypes
{
    List = 0,
    StrictArray,
    Value,
    Array,
}

internal static class Parser
{
    private static readonly char[][] idleSeparators = new[]
    {
        new[] { ' ' },       // List
        new[] { ',', ' ' },  // Array
        new[] { ' ' },       // Value
        new[] { ',', ' ' },  // ValueOrArray
    };
    private static readonly char[][] skipDetectionSeparators = new[]
    {
        new[] { '(', ')', '[', ']', '\'', '"', ' ' },  // List
        new[] { '(', ')', '[', ']', '\'', '"', ',' },  // Array
        new[] { '(', ')', '[', ']', '\'', '"', ' ' },  // Value
        new[] { '(', ')', '[', ']', '\'', '"', ',' },  // ValueOrArray
    };

    private static bool IsValidVariableName(string text)
    {
        static bool IsVariableChars0(char ch) =>
            char.IsLetter(ch) || ch == '_';

        if (text.Length >= 1)
        {
            if (!IsVariableChars0(text[0]))
            {
                return false;
            }

            for (var index = 1; index < text.Length; index++)
            {
                var ch = text[index];
                if (!(IsVariableChars0(ch) || char.IsDigit(ch) || ch == '.'))
                {
                    return false;
                }
            }
        }
        return true;
    }

    public static IExpression ParseExpression(
        string expressionString, ListTypes outerType = ListTypes.List)
    {
        var expressions = new List<IExpression>();
        var nested = new Stack<(ListTypes lastType, List<IExpression> lastExpressions)>();
        var currentType = outerType;

        var index = 0;
        while (index < expressionString.Length)
        {
            var startIndex = expressionString.IndexOfNotAll(
                idleSeparators[(int)currentType], index);
            if (startIndex == -1)
            {
                break;
            }

            if (currentType == ListTypes.Value &&
                nested.Count == 0 &&
                expressions.Count >= 1)
            {
                throw new FormatException(
                    "Expression must be only one term at this point.");
            }

            var ch = expressionString[startIndex];
            if (ch == '"')
            {
                var endIndex = expressionString.IndexOf('"', startIndex + 1);
                if (endIndex == -1)
                {
                    throw new FormatException("Could not find close quote.");
                }

                var text = expressionString.Substring(startIndex + 1, endIndex - 1 - startIndex);
                expressions.Add(new ValueExpression(text));

                index = endIndex + 1;
            }
            else if (ch == '\'')
            {
                var endIndex = expressionString.IndexOf('\'', startIndex + 1);
                if (endIndex == -1)
                {
                    throw new FormatException("Could not find close quote.");
                }

                var text = expressionString.Substring(startIndex + 1, endIndex - 1 - startIndex);
                expressions.Add(new ValueExpression(text));

                index = endIndex + 1;
            }
            else if (ch == '(')
            {
                nested.Push((currentType, expressions));
                expressions = new List<IExpression>();

                currentType = ListTypes.List;
                index = startIndex + 1;
            }
            else if (ch == ')')
            {
                if (nested.Count <= 0)
                {
                    throw new FormatException("Could not find open bracket.");
                }
                if (currentType != ListTypes.List)
                {
                    throw new FormatException("Unmatched close bracket.");
                }

                var (lastType, lastExpressions) = nested.Pop();

                var expression = expressions.Count switch
                {
                    // "()"
                    0 => throw new FormatException("Could not make empty at this point."),
                    // "(expr)"
                    1 => expressions[0],  // unwrap
                    // "(expr0, expr1, ...)"
                    _ => new ApplyExpression(expressions[0], expressions.Skip(1).ToArray()),
                };
                lastExpressions.Add(expression);

                expressions = lastExpressions;

                currentType = lastType;
                index++;
            }
            else if (ch == '[')
            {
                nested.Push((currentType, expressions));
                expressions = new List<IExpression>();

                currentType = ListTypes.StrictArray;
                index = startIndex + 1;
            }
            else if (ch == ']')
            {
                if (nested.Count <= 0)
                {
                    throw new FormatException("Could not find array beginning.");
                }
                if (currentType != ListTypes.StrictArray)
                {
                    throw new FormatException("Unmatched array finishing.");
                }

                var (lastType, lastExpressions) = nested.Pop();

                lastExpressions.Add(new ArrayExpression(expressions.ToArray()));
                expressions = lastExpressions;

                currentType = lastType;
                index++;
            }
            else
            {
                var nextIndex = expressionString.IndexOfAny(
                    skipDetectionSeparators[(int)currentType], startIndex + 1);
                if (nextIndex == -1)
                {
                    nextIndex = expressionString.Length;
                }

                var text = expressionString.
                    Substring(startIndex, nextIndex - startIndex).
                    Trim();
                if (bool.TryParse(text, out var bv))
                {
                    expressions.Add(new ValueExpression(bv));
                }
                else if (long.TryParse(text, out var lv))
                {
                    expressions.Add(new ValueExpression(lv));
                }
                else if (double.TryParse(text, out var dv))
                {
                    expressions.Add(new ValueExpression(dv));
                }
                else if (IsValidVariableName(text))
                {
                    expressions.Add(new VariableExpression(text));
                }
                else if (DateTimeOffset.TryParse(
                    text,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var dtv))
                {
                    expressions.Add(new ValueExpression(dtv));
                }
                else
                {
                    expressions.Add(new ValueExpression(text));
                }

                index = nextIndex;
            }
        }

        if (nested.Count >= 1)
        {
            switch (nested.Pop().lastType)
            {
                case ListTypes.List:
                    throw new FormatException("Unmatched close bracket.");
                case ListTypes.StrictArray:
                    throw new FormatException("Unmatched array finishing.");
            }
        }

        return (currentType, expressions.Count) switch
        {
            (ListTypes.StrictArray, 1) when expressions[0] is ArrayExpression => expressions[0],  // unwrap
            (ListTypes.StrictArray, _) => new ArrayExpression(expressions.ToArray()),
            (ListTypes.Array, 1) => expressions[0],  // unwrap
            (ListTypes.Array, _) => new ArrayExpression(expressions.ToArray()),
            (_, 0) => throw new FormatException("Could not make empty at this point."),
            (ListTypes.List, 1) => expressions[0],  // unwrap
            (ListTypes.List, _) => new ApplyExpression(expressions[0], expressions.Skip(1).ToArray()),
            (ListTypes.Value, 1) => expressions[0],
            _ => throw new FormatException("Could not make list/array at this point."),
        };
    }

    ///////////////////////////////////////////////////////////////////////////////////

    public static async ValueTask<RootTemplateNode> ParseTemplateAsync(
        string templateName,
        TextReader templateReader,
        CancellationToken ct)
    {
        var nestedIterations =
            new Stack<(IExpression[] iteratorParameters, List<ITemplateNode> nodes)>();

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

                var expressionString = line.Substring(
                    openIndex + 1, closeIndex - openIndex - 1);

                var expression = ParseExpression(expressionString, ListTypes.List);

                // Special case: iterator begin
                if (expression is ApplyExpression(VariableExpression("foreach"), var iteratorParameters))
                {
                    if (iteratorParameters.Length <= 0)
                    {
                        throw new FormatException(
                            $"`foreach` parameter required. Template={templateName}");
                    }

                    nestedIterations.Push((iteratorParameters, nodes));
                    nodes = new();
                }
                // Special case: iterator end
                else if (expression is VariableExpression("end"))
                {
                    if (nestedIterations.Count <= 0)
                    {
                        throw new FormatException(
                            $"Could not find iterator-begin. Template={templateName}");
                    }

                    var childNodes = nodes.ToArray();
                    var (parameters, lastNodes) = nestedIterations.Pop();

                    nodes = lastNodes;
                    nodes.Add(new ForEachNode(parameters, childNodes));
                }
                else
                {
                    nodes.Add(new ExpressionNode(expression));
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

#if true
    // TODO: rewrite with LL.
    private enum ParseTypes
    {
        AutoDetect,
        StringOnly,
        DateOnly,
    }
    private enum __ListTypes
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
        __ListTypes listType,
        TResult? previousValue)
    {
        // `title: [Hello world]`
        // * Assume yaml array-like: `tags: [aaa, bbb]` --> `tags: aaa, bbb`
        if (
            listType != __ListTypes.Ignore &&
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
                            listType == __ListTypes.AcceptOnce ?
                                __ListTypes.Ignore : __ListTypes.Accept,
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
#endif

    public static async ValueTask<Dictionary<string, IExpression>> ParseMarkdownHeaderAsync(
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

        var markdownMetadata = new Dictionary<string, IExpression>();

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
#if false
                        "title" => ParseExpression(valueText, ListTypes.Value),
                        "author" => ParseExpression(valueText, ListTypes.ValueOrArray),
                        "template" => ParseExpression(valueText, ListTypes.Value),
                        "category" => new ValueExpression(
                            ParseExpression(valueText, ListTypes.Array).
                            Aggregate(
                                new PartialCategoryEntry(),
                                (agg, v) => new PartialCategoryEntry(v.ImplicitValue, agg))),
                        "tags" => ParseExpression(valueText, ListTypes.ValueOrArray),
                        "date" => ParseExpression(valueText, ListTypes.Value),
                        _ => ParseExpression(valueText, ListTypes.ValueOrArray),
#else
                        "title" => ParseYamlLikeString(
                            valueText,
                            null,
                            ParseTypes.StringOnly,
                            __ListTypes.Ignore,
                            default(string)),
                        "author" => ParseYamlLikeString(
                            valueText,
                            null,
                            ParseTypes.StringOnly,
                            __ListTypes.AcceptOnce,
                            default(object)),
                        "template" => ParseYamlLikeString(
                            valueText,
                            (text, _) => new PartialTemplateEntry(text),
                            ParseTypes.StringOnly,
                            __ListTypes.Ignore,
                            default(PartialTemplateEntry)),
                        "category" => ParseYamlLikeString(
                            valueText,
                            (text, previous) => new PartialCategoryEntry(text, previous),
                            ParseTypes.StringOnly,
                            __ListTypes.AcceptOnce,
                            new PartialCategoryEntry()) switch
                            {
                                PartialCategoryEntry[] entries => entries.LastOrDefault(),
                                var r => r,
                            },
                        "tags" => ParseYamlLikeString(
                            valueText,
                            (text, _) => new PartialTagEntry(text),
                            ParseTypes.StringOnly,
                            __ListTypes.AcceptOnce,
                            default(PartialTagEntry)),
                        "date" => ParseYamlLikeString(
                            valueText,
                            null,
                            ParseTypes.DateOnly,
                            __ListTypes.Ignore,
                            default(object)),
                        _ => ParseYamlLikeString(
                            valueText,
                            null,
                            ParseTypes.AutoDetect,
                            __ListTypes.Accept,
                            default(object)),
#endif
                    };

                    if (value != null)
                    {
                        markdownMetadata[key] = new ValueExpression(value);
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

    public static async ValueTask<(Dictionary<string, IExpression> markdownMetadata, string markdownBody)> ParseMarkdownBodyAsync(
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
