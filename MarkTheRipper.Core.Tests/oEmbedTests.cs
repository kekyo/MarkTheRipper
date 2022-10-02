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
using MarkTheRipper.Expressions;
using MarkTheRipper.IO;
using MarkTheRipper.Metadata;
using MarkTheRipper.TextTreeNodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VerifyNUnit;

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

internal sealed class RipOffBaseMetadata
{
    public readonly (string key, object? value)[] BaseMetadata;

    public RipOffBaseMetadata(params (string key, object? value)[] baseMetadata) =>
        this.BaseMetadata = baseMetadata;
}

internal sealed class RipOffLayouts
{
    public readonly (string layoutName, string layoutText)[] Layouts;

    public RipOffLayouts(params (string layoutName, string)[] layouts) =>
        this.Layouts = layouts;
}

///////////////////////////////////////////////////////////////////////////////////

[TestFixture]
public sealed class oEmbedTests
{
    private static async ValueTask<string> RipOffContentAsync(
        string markdownText, string layoutName, string layoutText,
        RipOffBaseMetadata? baseMetadata = default,
        RipOffLayouts? layouts = default,
        DummyHttpAccessor? httpAccessor = default)
    {
        baseMetadata ??= new();
        layouts ??= new();

        var metadata = MetadataUtilities.CreateWithDefaults();
        if (httpAccessor != null)
        {
            metadata.SetValue("httpAccessor", httpAccessor);
        }

        var tr = new StringReader(layoutText);
        var layout = await Parser.ParseTextTreeAsync(
            new PathEntry(layoutName),
            _ => new ValueTask<string?>(tr.ReadLineAsync()),
            (_, _) => false,
            default);

        var layoutList = new Dictionary<string, RootTextNode>
        {
            { layoutName, layout }
        };

        foreach (var entry in layouts.Layouts)
        {
            var tr2 = new StringReader(entry.layoutText);
            var entryLayout = await Parser.ParseTextTreeAsync(
                new PathEntry(entry.layoutName),
                _ => new ValueTask<string?>(tr2.ReadLineAsync()),
                (_, _) => false,
                default);
            layoutList.Add(entry.layoutName, entryLayout);
        }

        metadata.SetValue("layout", new PartialLayoutEntry(layoutName));
        metadata.SetValue("layoutList", layoutList);

        foreach (var entry in baseMetadata.BaseMetadata)
        {
            metadata.SetValue(entry.key, entry.value);
        }

        var ripper = new Ripper();

        var htmlWriter = new StringWriter();
        htmlWriter.NewLine = Environment.NewLine;
        var appliedLayoutName = await ripper.RenderContentAsync(
            new PathEntry("RipperTests.md"),
            new StringReader(markdownText),
            metadata,
            htmlWriter,
            default);

        AreEqual(layoutName, appliedLayoutName.Path);

        httpAccessor?.AssertEmpty();

        return htmlWriter.ToString();
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffoEmbed1()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar
---

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
    <meta name=""keywords"" content=""{tags}"" />
  </head>
  <body>
    {embed https://www.youtube.com/watch?v=1La4QzGeaaQ}

{contentBody}</body>
</html>
",
default,
new(("embed-YouTube", @"<ul>
<li>permaLink: {permaLink}</li>
<li>siteName: {siteName}</li>
<li>title: {title}</li>
<li>altTitle: {altTitle}</li>
<li>author: {author}</li>
<li>description: {description}</li>
<li>type: {type}</li>
<li>imageUrl: {imageUrl}</li>
</ul>
<div>contentBody: {contentBody}</div>")),
new(("https://oembed.com/providers.json",
    @"[
    {
        ""provider_name"": ""YouTube"",
        ""provider_url"": ""https://www.youtube.com/"",
        ""endpoints"": [
            {
                ""schemes"": [
                    ""https://*.youtube.com/watch*"",
                    ""https://*.youtube.com/v/*"",
                    ""https://youtu.be/*"",
                    ""https://*.youtube.com/playlist?list=*"",
                    ""https://youtube.com/playlist?list=*"",
                    ""https://*.youtube.com/shorts*""
                ],
                ""url"": ""https://www.youtube.com/oembed"",
                ""discovery"": true
            }
        ]
    }
]"),
    ("https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v=1La4QzGeaaQ&format=json",
    @"{
    ""title"": ""Peru 8K HDR 60FPS (FUHD)"",
    ""author_name"": ""Jacob + Katie Schwarz"",
    ""author_url"": ""https://www.youtube.com/c/JacobKatieSchwarz"",
    ""type"": ""video"",
    ""height"": 113,
    ""width"": 200,
    ""version"": ""1.0"",
    ""provider_name"": ""YouTube"",
    ""provider_url"": ""https://www.youtube.com/"",
    ""thumbnail_height"": 360,
    ""thumbnail_width"": 480,
    ""thumbnail_url"": ""https://i.ytimg.com/vi/1La4QzGeaaQ/hqdefault.jpg"",
    ""html"": ""\u003ciframe width=\u0022200\u0022 height=\u0022113\u0022 src=\u0022https://www.youtube.com/embed/1La4QzGeaaQ?feature=oembed\u0022 frameborder=\u00220\u0022 allow=\u0022accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture\u0022 allowfullscreen title=\u0022Peru 8K HDR 60FPS (FUHD)\u0022\u003e\u003c/iframe\u003e""
}")));
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffoEmbed2()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar
---

{embed https://www.youtube.com/watch?v=1La4QzGeaaQ}

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
    <meta name=""keywords"" content=""{tags}"" />
  </head>
  <body>
{contentBody}</body>
</html>
",
default,
new(("embed-YouTube", @"<ul>
<li>permaLink: {permaLink}</li>
<li>siteName: {siteName}</li>
<li>title: {title}</li>
<li>altTitle: {altTitle}</li>
<li>author: {author}</li>
<li>description: {description}</li>
<li>type: {type}</li>
<li>imageUrl: {imageUrl}</li>
</ul>
<div>contentBody: {contentBody}</div>")),
new(("https://oembed.com/providers.json",
    @"[
    {
        ""provider_name"": ""YouTube"",
        ""provider_url"": ""https://www.youtube.com/"",
        ""endpoints"": [
            {
                ""schemes"": [
                    ""https://*.youtube.com/watch*"",
                    ""https://*.youtube.com/v/*"",
                    ""https://youtu.be/*"",
                    ""https://*.youtube.com/playlist?list=*"",
                    ""https://youtube.com/playlist?list=*"",
                    ""https://*.youtube.com/shorts*""
                ],
                ""url"": ""https://www.youtube.com/oembed"",
                ""discovery"": true
            }
        ]
    }
]"),
    ("https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v=1La4QzGeaaQ&format=json",
    @"{
    ""title"": ""Peru 8K HDR 60FPS (FUHD)"",
    ""author_name"": ""Jacob + Katie Schwarz"",
    ""author_url"": ""https://www.youtube.com/c/JacobKatieSchwarz"",
    ""type"": ""video"",
    ""height"": 113,
    ""width"": 200,
    ""version"": ""1.0"",
    ""provider_name"": ""YouTube"",
    ""provider_url"": ""https://www.youtube.com/"",
    ""thumbnail_height"": 360,
    ""thumbnail_width"": 480,
    ""thumbnail_url"": ""https://i.ytimg.com/vi/1La4QzGeaaQ/hqdefault.jpg"",
    ""html"": ""\u003ciframe width=\u0022200\u0022 height=\u0022113\u0022 src=\u0022https://www.youtube.com/embed/1La4QzGeaaQ?feature=oembed\u0022 frameborder=\u00220\u0022 allow=\u0022accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture\u0022 allowfullscreen title=\u0022Peru 8K HDR 60FPS (FUHD)\u0022\u003e\u003c/iframe\u003e""
}")));
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffoEmbedNotIncludeHtml()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar
---

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
    <meta name=""keywords"" content=""{tags}"" />
  </head>
  <body>
    {embed https://www.youtube.com/watch?v=1La4QzGeaaQ}

{contentBody}</body>
</html>
",
default,
new(("card-YouTube", @"<ul>
<li>permaLink: {permaLink}</li>
<li>siteName: {siteName}</li>
<li>title: {title}</li>
<li>altTitle: {altTitle}</li>
<li>author: {author}</li>
<li>description: {description}</li>
<li>type: {type}</li>
<li>imageUrl: {imageUrl}</li>
</ul>")),
new(("https://oembed.com/providers.json",
    @"[
    {
        ""provider_name"": ""YouTube"",
        ""provider_url"": ""https://www.youtube.com/"",
        ""endpoints"": [
            {
                ""schemes"": [
                    ""https://*.youtube.com/watch*"",
                    ""https://*.youtube.com/v/*"",
                    ""https://youtu.be/*"",
                    ""https://*.youtube.com/playlist?list=*"",
                    ""https://youtube.com/playlist?list=*"",
                    ""https://*.youtube.com/shorts*""
                ],
                ""url"": ""https://www.youtube.com/oembed"",
                ""discovery"": true
            }
        ]
    }
]"),
    ("https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v=1La4QzGeaaQ&format=json",
    @"{
    ""title"": ""Peru 8K HDR 60FPS (FUHD)"",
    ""author_name"": ""Jacob + Katie Schwarz"",
    ""author_url"": ""https://www.youtube.com/c/JacobKatieSchwarz"",
    ""type"": ""video"",
    ""height"": 113,
    ""width"": 200,
    ""version"": ""1.0"",
    ""provider_name"": ""YouTube"",
    ""provider_url"": ""https://www.youtube.com/"",
    ""thumbnail_height"": 360,
    ""thumbnail_width"": 480,
    ""thumbnail_url"": ""https://i.ytimg.com/vi/1La4QzGeaaQ/hqdefault.jpg""
}")));
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffoEmbedNotIncludeHtmlToDefault()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar
---

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
    <meta name=""keywords"" content=""{tags}"" />
  </head>
  <body>
    {embed https://www.youtube.com/watch?v=1La4QzGeaaQ}

{contentBody}</body>
</html>
",
default,
new(("card-YouTube", @"<ul>
<li>permaLink: {permaLink}</li>
<li>siteName: {siteName}</li>
<li>title: {title}</li>
<li>altTitle: {altTitle}</li>
<li>author: {author}</li>
<li>description: {description}</li>
<li>type: {type}</li>
<li>imageUrl: {imageUrl}</li>
</ul>")),
new(("https://oembed.com/providers.json",
    @"[
    {
        ""provider_name"": ""YouTube"",
        ""provider_url"": ""https://www.youtube.com/"",
        ""endpoints"": [
            {
                ""schemes"": [
                    ""https://*.youtube.com/watch*"",
                    ""https://*.youtube.com/v/*"",
                    ""https://youtu.be/*"",
                    ""https://*.youtube.com/playlist?list=*"",
                    ""https://youtube.com/playlist?list=*"",
                    ""https://*.youtube.com/shorts*""
                ],
                ""url"": ""https://www.youtube.com/oembed"",
                ""discovery"": true
            }
        ]
    }
]"),
    ("https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v=1La4QzGeaaQ&format=json",
    @"{
    ""title"": ""Peru 8K HDR 60FPS (FUHD)"",
    ""author_name"": ""Jacob + Katie Schwarz"",
    ""author_url"": ""https://www.youtube.com/c/JacobKatieSchwarz"",
    ""type"": ""video"",
    ""height"": 113,
    ""width"": 200,
    ""version"": ""1.0"",
    ""provider_name"": ""YouTube"",
    ""provider_url"": ""https://www.youtube.com/"",
    ""thumbnail_height"": 360,
    ""thumbnail_width"": 480,
    ""thumbnail_url"": ""https://i.ytimg.com/vi/1La4QzGeaaQ/hqdefault.jpg""
}")));
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffoEmbedOnlyHtmlTags()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar
---

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
    <meta name=""keywords"" content=""{tags}"" />
  </head>
  <body>
    {embed https://www.youtube.com/watch?v=1La4QzGeaaQ}

{contentBody}</body>
</html>
",
default,
new(("card-YouTube", @"<ul>
<li>permaLink: {permaLink}</li>
<li>siteName: {siteName}</li>
<li>title: {title}</li>
<li>altTitle: {altTitle}</li>
<li>author: {author}</li>
<li>description: {description}</li>
<li>type: {type}</li>
<li>imageUrl: {imageUrl}</li>
</ul>")),
new(("https://oembed.com/providers.json",
    @"[]"),
    ("https://www.youtube.com/watch?v=1La4QzGeaaQ",
    @"<html><head>
    <meta property=""og:title"" content=""Peru 8K HDR 60FPS (FUHD)"" />
    <meta property=""og:description"" content=""Jacob + Katie Schwarz"" />
    <meta property=""og:type"" content=""video"" />
    <meta property=""og:site_name"" content=""YouTube"" />
    <meta property=""og:image"" content=""https://i.ytimg.com/vi/1La4QzGeaaQ/hqdefault.jpg"" />
    </head><body /></html>")));
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffoEmbedOnlyHtmlTitle()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar
---

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
    <meta name=""keywords"" content=""{tags}"" />
  </head>
  <body>
    {embed https://www.youtube.com/watch?v=1La4QzGeaaQ}

{contentBody}</body>
</html>
",
default,
new(("card-YouTube", @"<ul>
<li>permaLink: {permaLink}</li>
<li>siteName: {siteName}</li>
<li>title: {title}</li>
<li>altTitle: {altTitle}</li>
<li>author: {author}</li>
<li>description: {description}</li>
<li>type: {type}</li>
<li>imageUrl: {imageUrl}</li>
</ul>")),
new(("https://oembed.com/providers.json",
    @"[]"),
    ("https://www.youtube.com/watch?v=1La4QzGeaaQ",
    @"<html><head>
    <title>Peru 8K HDR 60FPS (FUHD)</title>
    <meta property=""og:description"" content=""Jacob + Katie Schwarz"" />
    <meta property=""og:type"" content=""video"" />
    <meta property=""og:site_name"" content=""YouTube"" />
    <meta property=""og:image"" content=""https://i.ytimg.com/vi/1La4QzGeaaQ/hqdefault.jpg"" />
    </head><body /></html>")));
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffoEmbedBothHtmlTitle()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar
---

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
    <meta name=""keywords"" content=""{tags}"" />
  </head>
  <body>
    {embed https://www.youtube.com/watch?v=1La4QzGeaaQ}

{contentBody}</body>
</html>
",
default,
new(("card-YouTube", @"<ul>
<li>permaLink: {permaLink}</li>
<li>siteName: {siteName}</li>
<li>title: {title}</li>
<li>altTitle: {altTitle}</li>
<li>author: {author}</li>
<li>description: {description}</li>
<li>type: {type}</li>
<li>imageUrl: {imageUrl}</li>
</ul>")),
new(("https://oembed.com/providers.json",
    @"[]"),
    ("https://www.youtube.com/watch?v=1La4QzGeaaQ",
    @"<html><head>
    <title>[AltTitle]</title>
    <meta property=""og:title"" content=""Peru 8K HDR 60FPS (FUHD)"" />
    <meta property=""og:description"" content=""Jacob + Katie Schwarz"" />
    <meta property=""og:type"" content=""video"" />
    <meta property=""og:site_name"" content=""YouTube"" />
    <meta property=""og:image"" content=""https://i.ytimg.com/vi/1La4QzGeaaQ/hqdefault.jpg"" />
    </head><body /></html>")));
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffoEmbedFallback()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar
---

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
    <meta name=""keywords"" content=""{tags}"" />
  </head>
  <body>
    {embed https://www.youtube.com/watch?v=1La4QzGeaaQ}

{contentBody}</body>
</html>
",
default,
new(("card", @"<ul>
<li>permaLink: {permaLink}</li>
<li>siteName: {siteName}</li>
<li>title: {title}</li>
<li>altTitle: {altTitle}</li>
<li>author: {author}</li>
<li>description: {description}</li>
<li>type: {type}</li>
<li>imageUrl: {imageUrl}</li>
</ul>")),
new(("https://oembed.com/providers.json",
    @"[]"),
    ("https://www.youtube.com/watch?v=1La4QzGeaaQ",
    @"")));
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffoEmbedDiscovery()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar
---

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
    <meta name=""keywords"" content=""{tags}"" />
  </head>
  <body>
    {embed https://www.youtube.com/watch?v=1La4QzGeaaQ}

{contentBody}</body>
</html>
",
default,
new(("card-YouTube", @"<ul>
<li>permaLink: {permaLink}</li>
<li>siteName: {siteName}</li>
<li>title: {title}</li>
<li>altTitle: {altTitle}</li>
<li>author: {author}</li>
<li>description: {description}</li>
<li>type: {type}</li>
<li>imageUrl: {imageUrl}</li>
</ul>")),
new(("https://oembed.com/providers.json",
    @"[]"),
    ("https://www.youtube.com/watch?v=1La4QzGeaaQ",
    @"<html><head>
    <link type=""application/json+oembed"" href=""https://www.example.com/"" />
    </head><body /></html>"),
    ("https://www.example.com/",
    @"{
    ""title"": ""Peru 8K HDR 60FPS (FUHD)"",
    ""author_name"": ""Jacob + Katie Schwarz"",
    ""author_url"": ""https://www.youtube.com/c/JacobKatieSchwarz"",
    ""type"": ""video"",
    ""height"": 113,
    ""width"": 200,
    ""version"": ""1.0"",
    ""provider_name"": ""YouTube"",
    ""provider_url"": ""https://www.youtube.com/"",
    ""thumbnail_height"": 360,
    ""thumbnail_width"": 480,
    ""thumbnail_url"": ""https://i.ytimg.com/vi/1La4QzGeaaQ/hqdefault.jpg""
}")));
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffoEmbedDiscoveryWithHtml()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar
---

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
    <meta name=""keywords"" content=""{tags}"" />
  </head>
  <body>
    {embed https://www.youtube.com/watch?v=1La4QzGeaaQ}

{contentBody}</body>
</html>
",
default,
new(("embed-YouTube", @"<ul>
<li>permaLink: {permaLink}</li>
<li>siteName: {siteName}</li>
<li>title: {title}</li>
<li>altTitle: {altTitle}</li>
<li>author: {author}</li>
<li>description: {description}</li>
<li>type: {type}</li>
<li>imageUrl: {imageUrl}</li>
</ul>
<div>contentBody: {contentBody}</div>")),
new(("https://oembed.com/providers.json",
    @"[]"),
    ("https://www.youtube.com/watch?v=1La4QzGeaaQ",
    @"<html><head>
    <link type=""application/json+oembed"" href=""https://www.example.com/"" />
    </head><body /></html>"),
    ("https://www.example.com/",
    @"{
    ""title"": ""Peru 8K HDR 60FPS (FUHD)"",
    ""author_name"": ""Jacob + Katie Schwarz"",
    ""author_url"": ""https://www.youtube.com/c/JacobKatieSchwarz"",
    ""type"": ""video"",
    ""height"": 113,
    ""width"": 200,
    ""version"": ""1.0"",
    ""provider_name"": ""YouTube"",
    ""provider_url"": ""https://www.youtube.com/"",
    ""thumbnail_height"": 360,
    ""thumbnail_width"": 480,
    ""thumbnail_url"": ""https://i.ytimg.com/vi/1La4QzGeaaQ/hqdefault.jpg"",
    ""html"": ""\u003ciframe width=\u0022200\u0022 height=\u0022113\u0022 src=\u0022https://www.youtube.com/embed/1La4QzGeaaQ?feature=oembed\u0022 frameborder=\u00220\u0022 allow=\u0022accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture\u0022 allowfullscreen title=\u0022Peru 8K HDR 60FPS (FUHD)\u0022\u003e\u003c/iframe\u003e""
}")));
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffoEmbedInAmazonCom()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar
---

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
    <meta name=""keywords"" content=""{tags}"" />
  </head>
  <body>
    {embed https://www.amazon.com/gp/product/B07X5FPP4P/}

{contentBody}</body>
</html>
",
new(("amazonTrackingId-us", "abcde1-1")),
new(("embed-Amazon", @"<ul>
<li>permaLink: {permaLink}</li>
<li>siteName: {siteName}</li>
<li>title: {title}</li>
<li>altTitle: {altTitle}</li>
<li>author: {author}</li>
<li>description: {description}</li>
<li>type: {type}</li>
<li>imageUrl: {imageUrl}</li>
</ul>
<div>contentBody: {contentBody}</div>")));
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffoEmbedInAmazonCoJp()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar
---

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
    <meta name=""keywords"" content=""{tags}"" />
  </head>
  <body>
    {embed https://www.amazon.co.jp/dp/B07KQ25738/}

{contentBody}</body>
</html>
",
new(("amazonTrackingId-jp", "abcde1-1")),
new(("embed-Amazon", @"<ul>
<li>permaLink: {permaLink}</li>
<li>siteName: {siteName}</li>
<li>title: {title}</li>
<li>altTitle: {altTitle}</li>
<li>author: {author}</li>
<li>description: {description}</li>
<li>type: {type}</li>
<li>imageUrl: {imageUrl}</li>
</ul>
<div>contentBody: {contentBody}</div>")));
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffoEmbedInAmazonComInCard()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar
---

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
    <meta name=""keywords"" content=""{tags}"" />
  </head>
  <body>
    {card https://www.amazon.com/gp/product/B07X5FPP4P/}

{contentBody}</body>
</html>
",
new(("amazonTrackingId-us", "abcde1-1")),
new(("card-Amazon", @"<ul>
<li>permaLink: {permaLink}</li>
<li>siteName: {siteName}</li>
<li>title: {title}</li>
<li>altTitle: {altTitle}</li>
<li>author: {author}</li>
<li>description: {description}</li>
<li>type: {type}</li>
<li>imageUrl: {imageUrl}</li>
</ul>
<div>contentBody: {contentBody}</div>")));
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffoEmbedInAmazonCoJpInCard()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar
---

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
    <meta name=""keywords"" content=""{tags}"" />
  </head>
  <body>
    {card https://www.amazon.co.jp/dp/B07KQ25738/}

{contentBody}</body>
</html>
",
new(("amazonTrackingId-jp", "abcde1-1")),
new(("card-Amazon", @"<ul>
<li>permaLink: {permaLink}</li>
<li>siteName: {siteName}</li>
<li>title: {title}</li>
<li>altTitle: {altTitle}</li>
<li>author: {author}</li>
<li>description: {description}</li>
<li>type: {type}</li>
<li>imageUrl: {imageUrl}</li>
</ul>
<div>contentBody: {contentBody}</div>")));
        await Verifier.Verify(actual);
    }
}
