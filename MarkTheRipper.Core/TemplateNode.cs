/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTheRipper;

public abstract class TemplateNode
{
    private protected TemplateNode()
    {
    }

    public abstract ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        Func<string, string?, IFormatProvider, string?> getMetadata,
        IFormatProvider fp,
        CancellationToken ct);
}

///////////////////////////////////////////////////////////////////////////////////

internal sealed class TextNode : TemplateNode
{
    private readonly string text;

    public TextNode(string text) =>
        this.text = text;

    public override ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        Func<string, string?, IFormatProvider, string?> getMetadata,
        IFormatProvider fp,
        CancellationToken ct) =>
        writer(this.text, ct);

    public override string ToString() =>
        $"Text: \"{this.text}\"";
}

///////////////////////////////////////////////////////////////////////////////////

internal sealed class ReplacerNode : TemplateNode
{
    private readonly string keyName;
    private readonly string? parameter;

    public ReplacerNode(string keyName, string? parameter)
    {
        this.keyName = keyName;
        this.parameter = parameter;
    }

    public override async ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        Func<string, string?, IFormatProvider, string?> getMetadata,
        IFormatProvider fp,
        CancellationToken ct)
    {
        if (getMetadata(keyName, this.parameter, fp) is { } value)
        {
            await writer(value, ct).
                ConfigureAwait(false);
        }
    }

    public override string ToString() =>
        this.parameter is { } ?
            $"Replacer: {{{keyName}:{this.parameter}}}" :
            $"Replacer: {{{keyName}}}";
}

///////////////////////////////////////////////////////////////////////////////////

internal sealed class ForEachNode : TemplateNode
{
    private static readonly char[] separators = new[] { ',', ';', ':' };

    private readonly string keyName;
    private readonly TemplateNode[] childNodes;

    public ForEachNode(string keyName, TemplateNode[] childNodes)
    {
        this.keyName = keyName;
        this.childNodes = childNodes;
    }

    public override async ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        Func<string, string?, IFormatProvider, string?> getMetadata,
        IFormatProvider fp,
        CancellationToken ct)
    {
        if (getMetadata(this.keyName, null, fp) is { } valueString)
        {
            var iterationValues = valueString.Split(
                separators, StringSplitOptions.RemoveEmptyEntries);

            var index = 0;
            foreach (var iterationValue in iterationValues)
            {
                string? GetMetadata(string keyName, string? parameter, IFormatProvider fp) =>
                    keyName == (this.keyName + "-item") ?
                        Utilities.FormatValue(iterationValue, parameter, fp) :
                        keyName == (this.keyName + "-index") ?
                            index.ToString(parameter) :
                            getMetadata(keyName, parameter, fp);

                foreach (var childNode in this.childNodes)
                {
                    await childNode.RenderAsync(writer, GetMetadata, fp, ct).
                        ConfigureAwait(false);
                }

                index++;
            }
        }
    }

    public override string ToString() =>
        $"ForEach: {{{this.keyName}}}";
}

///////////////////////////////////////////////////////////////////////////////////

public sealed class RootTemplateNode : TemplateNode
{
    public readonly string OriginalText;

    private readonly TemplateNode[] nodes;

    public RootTemplateNode(string originalText, TemplateNode[] nodes)
    {
        this.OriginalText = originalText;
        this.nodes = nodes;
    }

    public override async ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        Func<string, string?, IFormatProvider, string?> getMetadata,
        IFormatProvider fp,
        CancellationToken ct)
    {
        foreach (var node in this.nodes)
        {
            await node.RenderAsync(writer, getMetadata, fp, ct).
                ConfigureAwait(false);
        }
    }
}
