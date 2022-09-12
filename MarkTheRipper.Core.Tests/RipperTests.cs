/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.Functions;
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
public sealed class RipperTests
{
    private sealed class RipOffBaseMetadata
    {
        public readonly (string key, object? value)[] BaseMetadata;

        public RipOffBaseMetadata(params (string key, object? value)[] baseMetadata) =>
            this.BaseMetadata = baseMetadata;
    }

    private sealed class RipOffLayouts
    {
        public readonly (string layoutName, string layoutText)[] Layouts;

        public RipOffLayouts(params (string layoutName, string)[] layouts) =>
            this.Layouts = layouts;
    }

    private static async ValueTask<string> RipOffContentAsync(
        string markdownText, string layoutName, string layoutText,
        RipOffBaseMetadata? baseMetadata = default,
        RipOffLayouts? layouts = default)
    {
        baseMetadata ??= new();
        layouts ??= new();

        var metadata = new MetadataContext();
        MetadataUtilities.SetDefaults(metadata);

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
tags: foo,bar
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
    <p>Date: {format date 'yyyy/MM/dd HH:mm:ss.fff zzz'}</p>
{contentBody}</body>
</html>
",
new(("date", date)));

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
    <p>Date: {date}</p>
{contentBody}</body>
</html>
",
new(("date", date)));

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
tags: foo,bar,baz
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
tags: foo,bar
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
    <h1>{lookup category}</h1>
    {contentBody}
  </body>
</html>
",
new(("main", "MAIN CATEGORY")),
new(("sub", "SUB CATEGORY")));

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
category: hoge1,hoge2,hoge3
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
{foreach category.breadcrumbs}
      <h1>Category: {item.name}</h1>
{end}
    {contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffCategoryLookup2()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
category: hoge1/hoge2/hoge3
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
tags: foo,bar,baz
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
category: foo,bar,baz
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
    {foreach tags}
    <h1>Tag: {test item.name 123}</h1>
    {end}
    {contentBody}
  </body>
</html>
",
new(("test", FunctionFactory.CastTo(
    async (parameters, metadata, ct) =>
    {
        var name = await parameters[0].ReduceExpressionAndFormatAsync(metadata, ct);
        var arg1 = await parameters[1].ReduceExpressionAndFormatAsync(metadata, ct);
        return new ValueExpression(name + arg1);
    }))));
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffFunction2()
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
    {foreach tags}
    <h1>Tag: {test item.name 123}</h1>
    {end}
    {contentBody}
  </body>
</html>
",
new(("test", FunctionFactory.CastTo(
    (parameters, metadata, fp, ct) =>
    {
        var name = parameters[0]?.ToString();
        var arg1 = parameters[1]?.ToString();
        return Task.FromResult((object?)(name + arg1));
    }))));
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffAdd1()
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
    <p>1 = {add 1}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffAdd2()
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
    <p>1 + 2 + 3 = {add 1 2 3}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffAdd3()
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
    <p>1 + 2.1 + 3 = {add 1 2.1 3}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffSub1()
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
    <p>1 = {sub 1}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffSub2()
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
    <p>1 - 2 - 3 = {sub 1 2 3}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffSub3()
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
    <p>1 - 2.1 - 3 = {sub 1 2.1 3}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffMul1()
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
    <p>1 = {mul 1}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffMul2()
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
    <p>1 * 2 * 3 = {mul 1 2 3}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffMul3()
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
    <p>1 * 2.1 * 3 = {format (mul 1 2.1 3) 'F3'}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffDiv1()
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
    <p>1 = {div 1}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffDiv2()
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
    <p>18 / 3 / 2 = {div 18 3 2}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffDiv3()
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
    <p>18.6 / 3.1 / 4 = {div 18.6 3.1 4}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffMod1()
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
    <p>1 = {mod 1}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffMod2()
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
    <p>18 % 12 % 4 = {mod 18 12 4}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffMod3()
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
    <p>18.1 % 12 % 4 = {format (mod 18.1 12 4) 'F3'}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }


    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffFormulaComplex()
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
    <p>(1 + 2) * 4 = {mul (add 1 2) 4}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffFormulaWithStringNumeric()
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
    <p>(1 + 2) * 4 = {mul (add '1' 2) '4'}</p>

{contentBody}</body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffBodyExamination1()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar,baz
---

Hello MarkTheRipper!
This is test contents.

* {title}
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
  </head>
  <body>
{contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffBodyExamination2()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar,baz
---

Hello MarkTheRipper!
This is test contents.

{foreach tags}
* {item.name}
{end}
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
  </head>
  <body>
{contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffBodyExaminationOnCodeBlock1()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar,baz
---

Hello MarkTheRipper!
This is test contents.

```
{title}
```
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
  </head>
  <body>
{contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffBodyExaminationOnCodeBlock2()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar,baz
---

Hello MarkTheRipper!
This is test contents.

```javascript
{title}
```
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
  </head>
  <body>
{contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffBodyExaminationOnCodeBlock3()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar,baz
---

Hello MarkTheRipper!
This is test contents.

   ```
   {title}
   ```
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
  </head>
  <body>
{contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffBodyExaminationOnCodeBlock4()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar,baz
---

Hello MarkTheRipper!
This is test contents.

   ```javascript
   {title}
   ```
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
  </head>
  <body>
{contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffBodyExaminationOnCodeSpan1()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar,baz
---

Hello MarkTheRipper!
This is test contents.

`{title}`
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
  </head>
  <body>
{contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffBodyExaminationOnCodeSpan2()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar,baz
---

Hello MarkTheRipper!
This is test contents.

`{title}`
",
"page",
@"<!DOCTYPE html>
<html>
  <head>
    <title>{title}</title>
  </head>
  <body>
{contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
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
    {oEmbed https://www.youtube.com/watch?v=1La4QzGeaaQ}

{contentBody}</body>
</html>
",
default,
new(("oEmbed-html", "<div class='oEmbed-outer'>{contentBody}</div>")));
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

{oEmbed https://www.youtube.com/watch?v=1La4QzGeaaQ}

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
new(("oEmbed-html", "<div class='oEmbed-outer'>{contentBody}</div>")));
        await Verifier.Verify(actual);
    }
}
