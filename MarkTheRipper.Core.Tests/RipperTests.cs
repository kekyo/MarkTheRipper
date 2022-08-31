﻿/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Layout;
using MarkTheRipper.Metadata;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VerifyNUnit;
using static NUnit.Framework.Assert;

namespace MarkTheRipper;

[TestFixture]
public sealed class RipperTests
{
    private static async ValueTask<string> RipOffContentAsync(
        string markdownText, string layoutName, string layoutText,
        params (string keyName, object? value)[] baseMetadata)
    {
        var metadata = new MetadataContext();

        var layout = await Parser.ParseLayoutAsync(
            layoutName, new StringReader(layoutText), default);
        var layoutList = new Dictionary<string, RootLayoutNode>
        {
            { layoutName, layout }
        };
        metadata.SetValue("layout", new PartialLayoutEntry(layoutName));
        metadata.SetValue("layoutList", layoutList);

        foreach (var entry in baseMetadata)
        {
            metadata.SetValue(entry.keyName, entry.value);
        }

        var ripper = new Ripper();

        var htmlWriter = new StringWriter();
        var appliedLayoutName = await ripper.RenderContentAsync(
            new PathEntry("RipperTests.md"),
            new StringReader(markdownText),
            metadata,
            htmlWriter,
            default);

        AreEqual(layoutName, appliedLayoutName);

        return htmlWriter.ToString();
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOff()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: [foo,bar]
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
{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffWithExplicitLayout()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: [foo,bar]
layout: baz
---

Hello MarkTheRipper!
This is test contents.
",
"baz",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
    <meta name=""keywords"" content=""{tags}"" />
  </head>
  <body>
{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffDateFormatting()
    {
        var date = new DateTimeOffset(2022, 1, 2, 12, 34, 56, 789, TimeSpan.FromHours(9));
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: [foo,bar]
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
    <p>Date: {format date 'yyyy/MM/dd HH:mm:ss.fff zzz'}</p>
{contentBody}</body>
</html>
",
("date", date));

        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffDateFormatting2()
    {
        var date = new DateTimeOffset(2022, 1, 2, 12, 34, 56, 789, TimeSpan.FromHours(9));
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
lang: ja-jp
tags: [foo,bar]
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
    <p>Date: {date}</p>
{contentBody}</body>
</html>
",
("date", date));

        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffItemIterator()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: [foo,bar]
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
    <ul>
{foreach tags}
        <li>{item}</li>
{end}
    </ul>
{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffIndexIterator()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: [foo,bar]
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
    <ul>
{foreach tags}
        <li>{item.index}</li>
{end}
    </ul>
{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffIteratorCount()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: [foo,bar,baz]
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
    <ul>
{foreach tags}
        <li>{item.index}/{item.count}</li>
{end}
    </ul>
{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffNestedIterator()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: [foo,bar]
author: [hoge,hoe]
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
{foreach author item1}
    <h3>{item1}</h3>
    <ul>
{foreach tags item2}
        <li>{item1}: {item2} [{item1.index}-{item2.index}]</li>
{end}
    </ul>
{end}

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffNestedLookup()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
category: main
tags: [foo,bar]
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
    <h1>{lookup category}</h1>
    {contentBody}
  </body>
</html>
",
("main", "MAIN CATEGORY"),
("sub", "SUB CATEGORY"));

        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffCategoryLookup1()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
category: [hoge1,hoge2,hoge3]
tags: [foo,bar]
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
{foreach category.breadcrumbs}
      <h1>Category: {item.name}</h1>
{end}
    {contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffThroughBracketLeft()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: [foo,bar]
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
    <p>Bracket left ==> {{{title}</p>
{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffThroughBracketRight()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: [foo,bar]
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
    <p>{title}}}<==Bracket right</p>
{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffTagIteration()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: [foo,bar,baz]
---

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
  </head>
  <body>
{foreach tags}
    <p>tag: {item}</p>
{end}
{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffCategoryIterationWithProperties()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
category: [foo,bar,baz]
---

Hello MarkTheRipper!
This is test contents.
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
  </head>
  <body>
{foreach category.breadcrumbs}
    <h1>{item.name}</h1>
{foreach item.breadcrumbs}
    <h2>{item.name}</h2>
{end}
{end}
{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffFunction1()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: [foo,bar]
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
    {foreach tags}
    <h1>Tag: {test item.name 123}</h1>
    {end}
    {contentBody}
  </body>
</html>
",
("test", FunctionFactory.CreateAsyncFunction(
    async (parameters, metadata, ct) =>
    {
        var name = await parameters[0].ReduceExpressionAndFormatAsync(metadata, ct);
        var arg1 = await parameters[1].ReduceExpressionAndFormatAsync(metadata, ct);
        return new ValueExpression(name + arg1);
    })));
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffFunction2()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: [foo,bar]
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
    {foreach tags}
    <h1>Tag: {test item.name 123}</h1>
    {end}
    {contentBody}
  </body>
</html>
",
("test", FunctionFactory.CreateAsyncFunction(
    (parameters, metadata, fp, ct) =>
    {
        var name = parameters[0]?.ToString();
        var arg1 = parameters[1]?.ToString();
        return Task.FromResult((object?)(name + arg1));
    })));
        await Verifier.Verify(actual);
    }
}
