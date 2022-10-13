/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Metadata;
using MarkTheRipper.TextTreeNodes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VerifyNUnit;

using static NUnit.Framework.Assert;

namespace MarkTheRipper;

[TestFixture]
public sealed class CardTest
{
    private static async ValueTask<string> RipOffContentAsync(
        string markdownText, string layoutName, string layoutText,
        RipOffBaseMetadata? baseMetadata = default,
        RipOffLayouts? layouts = default,
        DummyHttpAccessor? httpAccessor = default)
    {
        baseMetadata ??= new();
        layouts ??= new();

        var metadata = MetadataUtilities.CreateWithDefaults(
            httpAccessor ?? new());

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
    {card https://www.youtube.com/watch?v=1La4QzGeaaQ}

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

{card https://www.youtube.com/watch?v=1La4QzGeaaQ}

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
    {card https://www.youtube.com/watch?v=1La4QzGeaaQ}

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
    {card https://www.youtube.com/watch?v=1La4QzGeaaQ}

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
    {card https://www.youtube.com/watch?v=1La4QzGeaaQ}

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
    {card https://www.youtube.com/watch?v=1La4QzGeaaQ}

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
    {card https://www.youtube.com/watch?v=1La4QzGeaaQ}

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
    {card https://www.youtube.com/watch?v=1La4QzGeaaQ}

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
    {card https://www.youtube.com/watch?v=1La4QzGeaaQ}

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
    {card https://www.youtube.com/watch?v=1La4QzGeaaQ}

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
    ""thumbnail_url"": ""https://i.ytimg.com/vi/1La4QzGeaaQ/hqdefault.jpg"",
    ""html"": ""\u003ciframe width=\u0022200\u0022 height=\u0022113\u0022 src=\u0022https://www.youtube.com/embed/1La4QzGeaaQ?feature=oembed\u0022 frameborder=\u00220\u0022 allow=\u0022accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture\u0022 allowfullscreen title=\u0022Peru 8K HDR 60FPS (FUHD)\u0022\u003e\u003c/iframe\u003e""
}")));
        await Verifier.Verify(actual);
    }
}
