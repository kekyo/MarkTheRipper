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
        Func<string, object?> getMetadata,
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
        Func<string, object?> getMetadata,
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
        Func<string, object?> getMetadata,
        IFormatProvider fp,
        CancellationToken ct)
    {
        if (this.keyName[0] == '*')
        {
            var keyName = this.keyName.Substring(1);
            if (getMetadata(keyName) is { } rawNestedKeyName &&
                Utilities.FormatValue(rawNestedKeyName, null, fp) is { } nestedKeyName)
            {
                if (getMetadata(nestedKeyName) is { } rawValue &&
                    Utilities.FormatValue(rawValue, this.parameter, fp) is { } value)
                {
                    await writer(value, ct).
                        ConfigureAwait(false);
                }
                // Not found from all metadata.
                else
                {
                    await writer(nestedKeyName, ct).
                        ConfigureAwait(false);
                }
            }
            // Not found from all metadata.
            else
            {
                await writer($"<!-- Reference key: {keyName} -->", ct).
                    ConfigureAwait(false);
            }
        }
        else
        {
            if (getMetadata(this.keyName) is { } rawValue &&
                Utilities.FormatValue(rawValue, this.parameter, fp) is { } value)
            {
                await writer(value, ct).
                    ConfigureAwait(false);
            }
            // Not found from all metadata.
            else
            {
                await writer($"<!-- Key: {this.keyName} -->", ct).
                    ConfigureAwait(false);
            }
        }
    }

    public override string ToString() =>
        this.parameter is { } ?
            $"Replacer: {{{this.keyName}:{this.parameter}}}" :
            $"Replacer: {{{this.keyName}}}";
}

///////////////////////////////////////////////////////////////////////////////////

internal sealed class ForEachNode : TemplateNode
{
    private readonly string keyName;
    private readonly TemplateNode[] childNodes;

    public ForEachNode(string keyName, TemplateNode[] childNodes)
    {
        this.keyName = keyName;
        this.childNodes = childNodes;
    }

    public override async ValueTask RenderAsync(
        Func<string, CancellationToken, ValueTask> writer,
        Func<string, object?> getMetadata,
        IFormatProvider fp,
        CancellationToken ct)
    {
        if (getMetadata(this.keyName) is { } rawValue &&
            Utilities.EnumerateValue(rawValue) is { } enumerable)
        {
            var index = 0;
            foreach (var iterationValue in enumerable)
            {
                object? GetMetadata(string keyName) =>
                    keyName == (this.keyName + "-item") ?
                        iterationValue :
                        keyName == (this.keyName + "-index") ?
                            index :
                            getMetadata(keyName);

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
        Func<string, object?> getMetadata,
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
