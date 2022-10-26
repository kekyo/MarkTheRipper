/////////////////////////////////////////////////////////////////////////////////////
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
        InternalUtilities.Empty<char>(),  // Value
        InternalUtilities.Empty<char>(),  // SingleValue
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
                if (!(IsVariableChars0(ch) || char.IsDigit(ch) ||
                    ch == '.' || ch == ':' || ch == '-'))
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
                await Reducer.Instance.ReduceExpressionAndFormatAsync(
                    ParseExpression(expressionString, ListTypes.SingleValue),
                    MetadataContext.Empty, ct))),
            "category" => new ValueExpression(
                ParseExpression(expressionString, ListTypes.StrictArray) switch
                {
                    ArrayExpression(var elements) =>
                        (await Task.WhenAll(elements.Select(element =>
                            Reducer.Instance.ReduceExpressionAndFormatAsync(element, MetadataContext.Empty, ct).AsTask()))).
                        SelectMany(categoryName => categoryName.
                            Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).
                            Where(categoryName => categoryName.Trim().Length >= 1)).
                        Aggregate(
                            new PartialCategoryEntry(),
                            (agg, categoryName) => new PartialCategoryEntry(categoryName, agg)),
                    _ =>
                        new PartialCategoryEntry(),
                }),
            "tags" => new ValueExpression(
                ParseExpression(expressionString, ListTypes.StrictArray) is ArrayExpression(var elements) ?
                    await Task.WhenAll(elements.Select(async element =>
                        new PartialTagEntry(
                            await Reducer.Instance.ReduceExpressionAndFormatAsync(element, MetadataContext.Empty, ct)))) :
                    InternalUtilities.Empty<PartialTagEntry>()),
            "date" => ParseExpression(expressionString, ListTypes.SingleValue),
            _ => ParseExpression(expressionString, ListTypes.Array),
        };

    ///////////////////////////////////////////////////////////////////////////////////

    internal static async ValueTask<RootTextNode> ParseTextTreeAsync(
        PathEntry textPathHint,
        Func<CancellationToken, ValueTask<string?>> textReader,
        Func<int, int, bool> inCodeFragment,
        CancellationToken ct)
    {
        var nestedIterations =
            new Stack<(IExpression[] iteratorParameters, List<ITextTreeNode> nodes)>();

        var nodes = new List<ITextTreeNode>();
        var buffer = new StringBuilder();
        var lineIndex = 0;

        while (true)
        {
            var line = await textReader(ct);
            if (line == null)
            {
                break;
            }

            if (line.Length == 0)
            {
                buffer.AppendLine();
                lineIndex++;
                continue;
            }

            var startIndex = 0;
            while (startIndex < line.Length)
            {
                // "{"
                var openIndex = line.IndexOf('{', startIndex);
                if (openIndex >= 0)
                {
                    // Inside for code span/block.
                    if (inCodeFragment(lineIndex, openIndex))
                    {
                        buffer.Append(line.Substring(startIndex, openIndex - startIndex + 1));
                        startIndex = openIndex + 1;
                        if (startIndex >= line.Length)
                        {
                            buffer.AppendLine();
                        }
                        continue;
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

                    // "{ ... }"  (Spanning)
                    var closeIndex = line.IndexOf('}', openIndex + 1);
                    if (closeIndex == -1)
                    {
                        throw new FormatException(
                            $"Could not find close bracket. Path={textPathHint}");
                    }

                    // Exhausts literal text in the buffer.
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

                    startIndex = closeIndex + 1;

                    // Extract an expression string.
                    var expressionString = line.Substring(
                        openIndex + 1, closeIndex - openIndex - 1);

                    // Parse an expression.
                    var expression = ParseExpression(expressionString, ListTypes.List);

                    // Special case: iterator begin
                    if (expression is ApplyExpression(VariableExpression("foreach"), var iteratorParameters))
                    {
                        if (iteratorParameters.Length <= 0)
                        {
                            throw new FormatException(
                                $"`foreach` parameter required. Path={textPathHint}");
                        }

                        // Push current environment.
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

                        // Pop last environment.
                        var childNodes = nodes.ToArray();
                        var (parameters, lastNodes) = nestedIterations.Pop();

                        nodes = lastNodes;
                        nodes.Add(new ForEachNode(parameters, childNodes));
                    }
                    // Other expression.
                    else
                    {
                        nodes.Add(new ExpressionNode(expression));

                        if (startIndex >= line.Length)
                        {
                            buffer.AppendLine();
                        }
                    }
                }
                // Not detected expression bracket.
                else
                {
                    // "}}"
                    var closeIndex = line.IndexOf('}', startIndex);
                    if (closeIndex >= 0 &&
                        closeIndex + 1 < line.Length &&
                        line[closeIndex + 1] == '}')
                    {
                        if ((closeIndex + 2) < line.Length)
                        {
                            buffer.Append(line.Substring(startIndex, closeIndex - startIndex + 1));
                        }
                        startIndex = closeIndex + 2;
                        if (startIndex >= line.Length)
                        {
                            buffer.AppendLine();
                        }
                        continue;
                    }

                    buffer.AppendLine(line.Substring(startIndex));
                    break;
                }
            }

            lineIndex++;
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
        Func<CancellationToken, ValueTask<string?>> markdownReader,
        CancellationToken ct)
    {
        // `---`
        while (true)
        {
            var line = await markdownReader(ct);
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

        var headerMetadata = new Dictionary<string, IExpression>();

        // `title: Hello world`
        while (true)
        {
            var line = await markdownReader(ct);
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
                        keyName, valueText, ct);
                    if (valueExpression != null)
                    {
                        headerMetadata[keyName] = valueExpression;
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

        return headerMetadata;
    }

    internal static async ValueTask ParseAndAppendMarkdownHeaderAsync(
        Func<CancellationToken, ValueTask<string?>> markdownReader,
        IReadOnlyDictionary<string, string> values,
        Func<string, CancellationToken, ValueTask> markdownWriter,
        CancellationToken ct)
    {
        // Writer is overlapped.
        ValueTask writingTask = new();

        try
        {
            // `---`
            while (true)
            {
                var line = await markdownReader(ct);
                if (line == null)
                {
                    throw new InvalidOperationException();
                }

                await writingTask;

                writingTask = markdownWriter(line, ct);

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
                var line = await markdownReader(ct);
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
                            await writingTask;

                            foreach (var entry in values)
                            {
                                await markdownWriter($"{entry.Key}: {entry.Value}", ct);
                            }

                            writingTask = markdownWriter(line, ct);
                            break;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }

                await writingTask;
                await markdownWriter(line, ct);
            }

            while (true)
            {
                var line = await markdownReader(ct);

                // EOF
                if (line == null)
                {
                    break;
                }

                await writingTask;
                writingTask = markdownWriter(line, ct);
            }
        }
        finally
        {
            try
            {
                await writingTask;
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
        public readonly char CodeEscapeChar;
        public readonly int CodeEscapeLength;

        public CodeBlockContext(
            int startLineIndex, int indentLength, char codeEscapeChar, int codeEscapeLength)
        {
            this.StartLineIndex = startLineIndex;
            this.IndentLength = indentLength;
            this.CodeEscapeChar = codeEscapeChar;
            this.CodeEscapeLength = codeEscapeLength;
        }
    }

    internal static async ValueTask<(Dictionary<string, IExpression> headerMetadata, string markdownBody, Func<int, int, bool>[] inCodeFragments)> ParseMarkdownBodyAsync(
        PathEntry relativeContentPathHint,
        Func<CancellationToken, ValueTask<string?>> markdownReader,
        CancellationToken ct)
    {
        var headerMetadata = await ParseMarkdownHeaderAsync(
            relativeContentPathHint, markdownReader, ct);

        var markdownBody = new StringBuilder();
        var inCodeFragments = new List<Func<int, int, bool>>();
        var lineIndex = 0;

        while (true)
        {
            var line = await markdownReader(ct);
            if (line == null)
            {
                break;
            }

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
                            codeEscapeChar, codeEscapeStartIndex + 1) is { } i && i >= 0 ?
                            i : line.Length;

                        var codeEscapeLength = codeStartIndex - codeEscapeStartIndex;

                        // First spanning?   "aaa`bbb`ccc"
                        var codeEndIndex = line.IndexOf(
                            codeEscapeChar, codeStartIndex);
                        if (codeEndIndex >= 0)
                        {
                        loop:
                            var codeEscapeEndIndex = line.IndexOfNot(
                                codeEscapeChar, codeEndIndex + 1) is { } i2 && i2 >= 0 ?
                                    i2 : line.Length;

                            // Should really be the same length, but judged loose.
                            if ((codeEscapeEndIndex - codeEndIndex) >= codeEscapeLength)
                            {
                                var closure = (lineIndex, codeEscapeStartIndex, codeEscapeEndIndex);
                                inCodeFragments.Add((l, c) =>
                                    l == closure.lineIndex &&
                                    c >= closure.codeEscapeStartIndex &&
                                    c < closure.codeEscapeEndIndex);
                            }

                            // Search next code span.
                            codeEscapeStartIndex = line.IndexOfAny(
                                escapeChars, codeEscapeEndIndex);
                            if (codeEscapeStartIndex >= 0)
                            {
                                codeStartIndex = line.IndexOfNot(
                                    codeEscapeChar, codeEscapeStartIndex + 1) is { } i3 && i3 >= 0 ?
                                    i3 : line.Length;

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
                                codeBlockContext = new(
                                    lineIndex, indentEndIndex, codeEscapeChar, codeEscapeLength);
                            }
                            // End code block
                            else if (
                                codeBlockContext.IndentLength == indentEndIndex &&
                                codeBlockContext.CodeEscapeChar == codeEscapeChar &&
                                codeBlockContext.CodeEscapeLength == codeEscapeLength)
                            {
                                var closure = (startLineIndex: codeBlockContext.StartLineIndex, endLineIndex: lineIndex + 1);
                                inCodeFragments.Add((l, _) =>
                                    l >= closure.startLineIndex &&
                                    l < closure.endLineIndex);
                            }
                        }
                    }

                    // (Sanitizing EOL)
                    markdownBody.AppendLine(line);
                    lineIndex++;

                    line = await markdownReader(ct);

                    // EOF
                    if (line == null)
                    {
                        break;
                    }
                }

                break;
            }
        }

        return (headerMetadata, markdownBody.ToString(), inCodeFragments.ToArray());
    }
}
