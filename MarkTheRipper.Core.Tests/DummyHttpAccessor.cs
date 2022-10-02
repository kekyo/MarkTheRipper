/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using MarkTheRipper.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using static NUnit.Framework.Assert;

namespace MarkTheRipper;

internal sealed class DummyHttpAccessor : IHttpAccessor
{
    private readonly Queue<(Uri url, string content)> queue = new();

    public DummyHttpAccessor(params (string url, string content)[] entries)
    {
        foreach (var entry in entries)
        {
            this.queue.Enqueue(
                (new Uri(entry.url, UriKind.RelativeOrAbsolute), entry.content));
        }
    }

    private (Uri url, string content) Dequeue()
    {
        lock (this.queue)
        {
            AreNotEqual(0, this.queue.Count);
            return this.queue.Dequeue();
        }
    }

    public void AssertEmpty()
    {
        lock (this.queue)
        {
            AreEqual(0, this.queue.Count);
        }
    }

    public ValueTask<IHtmlDocument> FetchHtmlAsync(Uri url, CancellationToken ct)
    {
        var (u, c) = this.Dequeue();
        AreEqual(u, url);

        var parser = new HtmlParser();
        return new ValueTask<IHtmlDocument>(parser.ParseDocumentAsync(c));
    }

    public ValueTask<JToken> FetchJsonAsync(Uri url, CancellationToken ct)
    {
        var (u, c) = this.Dequeue();
        AreEqual(u, url);

        var tr = new StringReader(c);
        var jr = new JsonTextReader(tr);
        return new ValueTask<JToken>(JToken.ReadFrom(jr));
    }

    public ValueTask<JToken> PostJsonAsync(
        Uri url, JToken requestJson,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken ct)
    {
        var (u, c) = this.Dequeue();
        AreEqual(u, url);

        var tr = new StringReader(c);
        var jr = new JsonTextReader(tr);
        return new ValueTask<JToken>(JToken.ReadFrom(jr));
    }

    public ValueTask<Uri?> ExamineShortUrlAsync(
        Uri url, CancellationToken ct)
    {
        var (u, c) = this.Dequeue();
        AreEqual(u, url);

        return new ValueTask<Uri?>(
            Uri.TryCreate(c, UriKind.RelativeOrAbsolute, out var examined) ?
                examined : null);
    }
}
