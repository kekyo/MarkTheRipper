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

internal sealed class AssertionPair
{
    public readonly Uri Url;
    public readonly object Content;

    public AssertionPair(string url, string content)
    {
        this.Url = new Uri(url, UriKind.Absolute);
        this.Content = content;
    }

    public AssertionPair(string url, Uri redirectUrl)
    {
        this.Url = new Uri(url, UriKind.Absolute);
        this.Content = redirectUrl;
    }

    public void Deconstruct(out Uri url, out object content)
    {
        url = this.Url;
        content = this.Content;
    }
}

internal sealed class DummyHttpAccessor : IHttpAccessor
{
    private readonly Queue<AssertionPair> queue = new();

    public DummyHttpAccessor(params AssertionPair[] entries)
    {
        foreach (var entry in entries)
        {
            this.queue.Enqueue(entry);
        }
    }

    private AssertionPair Dequeue()
    {
        lock (this.queue)
        {
            AreNotEqual(0, this.queue.Count);
            return this.queue.Dequeue();
        }
    }

    private AssertionPair? Peek()
    {
        lock (this.queue)
        {
            return this.queue.Count >= 1 ?
                this.queue.Peek() : null;
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
        IsTrue(c is string);

        var parser = new HtmlParser();
        return new ValueTask<IHtmlDocument>(parser.ParseDocumentAsync((string)c));
    }

    public ValueTask<JToken> FetchJsonAsync(Uri url, CancellationToken ct)
    {
        var (u, c) = this.Dequeue();
        AreEqual(u, url);
        IsTrue(c is string);

        var tr = new StringReader((string)c);
        var jr = new JsonTextReader(tr);
        return new ValueTask<JToken>(JToken.ReadFrom(jr));
    }

    public ValueTask<JToken> PostJsonAsync(
        Uri url, JToken requestJson,
        IReadOnlyDictionary<string, string> headers,
        IReadOnlyDictionary<string, string> cacheKeyValues,
        CancellationToken ct)
    {
        var (u, c) = this.Dequeue();
        AreEqual(u, url);
        IsTrue(c is string);

        var tr = new StringReader((string)c);
        var jr = new JsonTextReader(tr);
        return new ValueTask<JToken>(JToken.ReadFrom(jr));
    }

    public ValueTask<Uri> ExamineShortUrlAsync(
        Uri url, CancellationToken ct)
    {
        if (this.Peek() is (var u, Uri expected))
        {
            this.Dequeue();
            AreEqual(u, url);

            return new ValueTask<Uri>(expected);
        }
        else
        {
            return new ValueTask<Uri>(url);
        }
    }
}
