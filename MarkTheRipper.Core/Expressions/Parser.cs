﻿/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using MarkTheRipper.Metadata;
using MarkTheRipper.TextTreeNodes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper.Expressions;

public enum ListTypes
{
    List = 0,
    StrictArray,
    Array,
    Value,
    SingleValue,
}

public static class Parser
{
    private static readonly char[][] idleSeparators = new[]
    {
        new[] { ' ' },       // List
        new[] { ',', ' ' },  // StrictArray
        new[] { ',', ' ' },  // Array
        Utilities.Empty<char>(),  // Value
        Utilities.Empty<char>(),  // SingleValue
    };
    private static readonly char[][] skipDetectionSeparators = new[]
    {
        new[] { '(', ')', '[', ']', '\'', '"', ' ' },  // List
        new[] { '(', ')', '[', ']', '\'', '"', ',' },  // StrictArray
        new[] { '(', ')', '[', ']', '\'', '"', ',' },  // Array
        new[] { '(', ')', '[', ']', '\'', '"' },  // Value
        new[] { '\'', '"' },  // SingleValue
    };

    private static readonly char[] escapeChars = new[] { '`', '~' };

    ///////////////////////////////////////////////////////////////////////////////////

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

            if ((currentType == ListTypes.Value || currentType == ListTypes.SingleValue) &&
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
            else if (outerType != ListTypes.SingleValue && ch == '(')
            {
                nested.Push((currentType, expressions));
                expressions = new List<IExpression>();

                currentType = ListTypes.List;
                index = startIndex + 1;
            }
            else if (outerType != ListTypes.SingleValue && ch == ')')
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
            else if (outerType != ListTypes.SingleValue && ch == '[')
            {
                nested.Push((currentType, expressions));
                expressions = new List<IExpression>();

                currentType = ListTypes.StrictArray;
                index = startIndex + 1;
            }
            else if (outerType != ListTypes.SingleValue && ch == ']')
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
                    out var dto))
                {
                    expressions.Add(new ValueExpression(new PartialDateEntry(dto)));
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
            (ListTypes.SingleValue, _) => expressions[0],
            (ListTypes.Value, 1) => expressions[0],
            _ => throw new FormatException("Could not make list/array at this point."),
        };
    }

    public static async ValueTask<IExpression> ParseKeywordExpressionAsync(
        string keyNameHint, string expressionString, CancellationToken ct) =>
        keyNameHint switch
        {
            "title" => ParseExpression(expressionString, ListTypes.SingleValue),
            "author" => ParseExpression(expressionString, ListTypes.Array),
            "layout" => new ValueExpression(new PartialLayoutEntry(
                await ParseExpression(expressionString, ListTypes.SingleValue).
                    ReduceExpressionAndFormatAsync(MetadataContext.Empty, ct).
                    ConfigureAwait(false))),
            "category" => new ValueExpression(
                ParseExpression(expressionString, ListTypes.StrictArray) is ArrayExpression(var elements) ?
                    (await Task.WhenAll(elements.Select(element =>
                        element.ReduceExpressionAndFormatAsync(MetadataContext.Empty, ct).AsTask())).
                        ConfigureAwait(false)).
                    Aggregate(
                        new PartialCategoryEntry(),
                        (agg, categoryName) => new PartialCategoryEntry(categoryName, agg)) :
                    new PartialCategoryEntry()),
            "tags" => new ValueExpression(
                ParseExpression(expressionString, ListTypes.StrictArray) is ArrayExpression(var elements) ?
                    await Task.WhenAll(elements.Select(async element =>
                        new PartialTagEntry(
                            await element.ReduceExpressionAndFormatAsync(MetadataContext.Empty, ct).
                                ConfigureAwait(false)))).
                        ConfigureAwait(false) :
                    Utilities.Empty<PartialTagEntry>()),
            "date" => ParseExpression(expressionString, ListTypes.SingleValue),
            _ => ParseExpression(expressionString, ListTypes.Array),
        };

    ///////////////////////////////////////////////////////////////////////////////////

    internal static async ValueTask<RootTextNode> ParseTextTreeAsync(
        PathEntry textPathHint,
        TextReader textReader,
        CancellationToken ct)
    {
        var nestedIterations =
            new Stack<(IExpression[] iteratorParameters, List<ITextTreeNode> nodes)>();

        var nodes = new List<ITextTreeNode>();
        var buffer = new StringBuilder();

        while (true)
        {
            var line = await textReader.ReadLineAsync().
                WithCancellation(ct).
                ConfigureAwait(false);
            if (line == null)
            {
                break;
            }

            if (line.Length == 0)
            {
                buffer.AppendLine();
                continue;
            }

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

                    // "}}"
                    if (closeIndex2 + 1 < line.Length &&
                        line[closeIndex2 + 1] == '}')
                    {
                        if ((closeIndex2 + 2) < line.Length)
                        {
                            buffer.Append(line.Substring(startIndex, closeIndex2 - startIndex + 1));
                        }
                        startIndex = closeIndex2 + 2;
                        if (startIndex >= line.Length)
                        {
                            buffer.AppendLine();
                        }
                        continue;
                    }

                    throw new FormatException(
                        $"Could not find open bracket. Path={textPathHint}");
                }

                // "{{"
                if (openIndex + 1 < line.Length &&
                    line[openIndex + 1] == '{')
                {
                    buffer.Append(line.Substring(startIndex, openIndex - startIndex + 1));
                    startIndex = openIndex + 2;
                    if (startIndex >= line.Length)
                    {
                        buffer.AppendLine();
                    }
                    continue;
                }

                if (startIndex >= line.Length)
                {
                    buffer.AppendLine(line.Substring(startIndex, openIndex - startIndex));
                }
                else
                {
                    buffer.Append(line.Substring(startIndex, openIndex - startIndex));
                }

                if (buffer.Length >= 1)
                {
                    nodes.Add(new LiteralTextNode(buffer.ToString()));
                    buffer.Clear();
                }

                var closeIndex = line.IndexOf('}', openIndex + 1);
                if (closeIndex == -1)
                {
                    throw new FormatException(
                        $"Could not find close bracket. Path={textPathHint}");
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
                            $"`foreach` parameter required. Path={textPathHint}");
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
                            $"Could not find iterator-begin. Path={textPathHint}");
                    }

                    var childNodes = nodes.ToArray();
                    var (parameters, lastNodes) = nestedIterations.Pop();

                    nodes = lastNodes;
                    nodes.Add(new ForEachNode(parameters, childNodes));
                }
                else
                {
                    nodes.Add(new ExpressionNode(expression));

                    if (startIndex >= line.Length)
                    {
                        buffer.AppendLine();
                    }
                }
            }
        }

        if (buffer.Length >= 1)
        {
            nodes.Add(new LiteralTextNode(buffer.ToString()));
        }

        return new RootTextNode(
           textPathHint,
           nodes.ToArray());
    }

    ///////////////////////////////////////////////////////////////////////////////////

    internal static async ValueTask<Dictionary<string, IExpression>> ParseMarkdownHeaderAsync(
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
                    $"Could not find any markdown header. Path={relativeContentPathHint}");
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
                    $"Could not find any markdown header. Path={relativeContentPathHint}");
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                var keyIndex = line.IndexOf(':');
                if (keyIndex >= 1)
                {
                    var keyName = line.Substring(0, keyIndex).Trim();
                    var valueText = line.Substring(keyIndex + 1).Trim();
                    var valueExpression = await ParseKeywordExpressionAsync(
                        keyName, valueText, ct).
                        ConfigureAwait(false);
                    if (valueExpression != null)
                    {
                        markdownMetadata[keyName] = valueExpression;
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
                            $"Could not find any markdown header. Path={relativeContentPathHint}");
                    }
                }
            }
        }

        return markdownMetadata;
    }

    internal static async ValueTask ParseAndAppendMarkdownHeaderAsync(
        TextReader markdownReader,
        IReadOnlyDictionary<string, string> values,
        TextWriter markdownWriter,
        CancellationToken ct)
    {
        // Writer is overlapped.
        ValueTask writingTask = new();

        try
        {
            // `---`
            while (true)
            {
                var line = await markdownReader.ReadLineAsync().
                    WithCancellation(ct).
                    ConfigureAwait(false);
                if (line == null)
                {
                    throw new InvalidOperationException();
                }

                await writingTask.
                    ConfigureAwait(false);

                writingTask = markdownWriter.WriteLineAsync(line).
                    WithCancellation(ct);

                if (!string.IsNullOrWhiteSpace(line))
                {
                    if (line.Trim().StartsWith("---"))
                    {
                        break;
                    }
                }
            }

            // `title: Hello world`
            while (true)
            {
                var line = await markdownReader.ReadLineAsync().
                    WithCancellation(ct).
                    ConfigureAwait(false);
                if (line == null)
                {
                    throw new InvalidOperationException();
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    var keyIndex = line.IndexOf(':');
                    if (keyIndex == -1)
                    {
                        // `---`
                        if (line.Trim().StartsWith("---"))
                        {
                            await writingTask.
                                ConfigureAwait(false);

                            foreach (var entry in values)
                            {
                                await markdownWriter.WriteLineAsync($"{entry.Key}: {entry.Value}").
                                    WithCancellation(ct).
                                    ConfigureAwait(false);
                            }

                            writingTask = markdownWriter.WriteLineAsync(line).
                                WithCancellation(ct);
                            break;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }

                await writingTask.
                    ConfigureAwait(false);

                await markdownWriter.WriteLineAsync(line).
                    WithCancellation(ct).
                    ConfigureAwait(false);
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

                await writingTask.
                    ConfigureAwait(false);

                writingTask = markdownWriter.WriteLineAsync(line).
                    WithCancellation(ct);
            }
        }
        finally
        {
            try
            {
                await writingTask.
                    ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }

    private sealed class CodeBlockContext
    {
        public readonly int StartLineIndex;
        public readonly int IndentLength;
        public readonly int CodeEscapeLength;

        public CodeBlockContext(int startLineIndex, int indentLength, int codeEscapeLength)
        {
            this.StartLineIndex = startLineIndex;
            this.IndentLength = indentLength;
            this.CodeEscapeLength = codeEscapeLength;
        }
    }

    internal static async ValueTask<(Dictionary<string, IExpression> markdownMetadata, string markdownBody, Func<int, int, bool>[] inCodeFragments)> ParseMarkdownBodyAsync(
        PathEntry relativeContentPathHint,
        TextReader markdownReader,
        CancellationToken ct)
    {
        var markdownMetadata = await ParseMarkdownHeaderAsync(
            relativeContentPathHint, markdownReader, ct);

        var markdownBody = new StringBuilder();
        var inCodeFragments = new List<Func<int, int, bool>>();
        var lineIndex = 0;

        while (true)
        {
            var line = await markdownReader.ReadLineAsync().
                WithCancellation(ct).
                ConfigureAwait(false);
            if (line == null)
            {
                break;
            }

            lineIndex++;

            // Skip empty lines, will detect start of body
            if (!string.IsNullOrWhiteSpace(line))
            {
                var codeBlockContext = default(CodeBlockContext);
                while (true)
                {
                    // Code block/span detection.
                    var codeEscapeStartIndex = line.IndexOfAny(
                        escapeChars);
                    if (codeEscapeStartIndex >= 0)
                    {
                        var codeEscapeChar = line[codeEscapeStartIndex];

                        var codeStartIndex = line.IndexOfNot(
                            codeEscapeChar, codeEscapeStartIndex + 1);
                        if (codeStartIndex >= 0)
                        {
                            var codeEscapeLength = codeStartIndex - codeEscapeStartIndex;

                            // First spanning?   "aaa`bbb`ccc"
                            var codeEndIndex = line.IndexOf(
                                codeEscapeChar, codeStartIndex);
                            if (codeEndIndex >= 0)
                            {
                            loop:
                                var codeEscapeEndIndex = line.IndexOfNot(
                                    codeEscapeChar, codeEndIndex + 1) is { } i && i >= 0 ?
                                        i : line.Length;

                                // Should really be the same length, but judged loose.
                                if ((codeEscapeEndIndex - codeEndIndex) >= codeEscapeLength)
                                {
                                    var closure = (lineIndex, codeStartIndex, codeEndIndex);
                                    inCodeFragments.Add((l, c) =>
                                        l == closure.lineIndex &&
                                        c >= closure.codeStartIndex &&
                                        c < closure.codeEndIndex);
                                }

                                // Search next code span.
                                codeEscapeStartIndex = line.IndexOfAny(
                                    escapeChars, codeEscapeEndIndex);
                                if (codeEscapeStartIndex >= 0)
                                {
                                    codeEscapeLength = codeStartIndex - codeEscapeStartIndex;
                                    codeEndIndex = line.IndexOf(
                                        codeEscapeChar, codeStartIndex);
                                    if (codeEndIndex >= 0)
                                    {
                                        goto loop;
                                    }
                                }
                            }
                            // Code block.
                            else
                            {
                                var indentEndIndex = line.IndexOfNot(' ', 0);

                                // Start code block
                                if (codeBlockContext == null)
                                {
                                    codeBlockContext = new(lineIndex, 0, codeEscapeLength);
                                }
                            }
                        }
                        else
                        {
                            // error/ignore?
                        }
                    }

                    // (Sanitizing EOL)
                    markdownBody.AppendLine(line);

                    line = await markdownReader.ReadLineAsync().
                        WithCancellation(ct).
                        ConfigureAwait(false);

                    // EOF
                    if (line == null)
                    {
                        break;
                    }

                    lineIndex++;
                }

                break;
            }
        }

        return (markdownMetadata, markdownBody.ToString(), inCodeFragments.ToArray());
    }
}
