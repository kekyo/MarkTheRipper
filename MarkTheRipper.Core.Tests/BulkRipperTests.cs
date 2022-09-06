/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Expressions;
using MarkTheRipper.TextTreeNodes;
using MarkTheRipper.Metadata;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VerifyNUnit;

namespace MarkTheRipper;

[TestFixture]
public sealed class BulkRipperTests
{
    private static async ValueTask<string?> RipOffContentAsync(
        string[] categoryNames, string markdownText, string layoutName, string layoutText,
        params (string keyName, object? value)[] baseMetadata)
    {
        var metadata = new MetadataContext();

        var layout = await Parser.ParseTextTreeAsync(
            new PathEntry(layoutName),
            new StringReader(layoutText),
            (_, _) => false,
            default);
        var layoutList = new Dictionary<string, RootTextNode>
        {
            { layoutName, layout }
        };
        metadata.SetValue("layout", new PartialLayoutEntry(layoutName));
        metadata.SetValue("layoutList", layoutList);

        foreach (var entry in baseMetadata)
        {
            metadata.SetValue(entry.keyName, entry.value);
        }

        var basePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(basePath);
        try
        {
            var contentBasePath = Path.Combine(basePath, "contents");
            var storeToBasePath = Path.Combine(basePath, "docs");

            var categoryUnderContentBasePath =
                categoryNames.Aggregate((v0, v1) => Path.Combine(v0, v1));
            Directory.CreateDirectory(
                Path.Combine(contentBasePath, categoryUnderContentBasePath));

            var contentPath = Path.Combine(
                contentBasePath, categoryUnderContentBasePath, "temp.md");
            File.WriteAllText(contentPath, markdownText);

            var refContentPath = Path.Combine(
                contentBasePath, "ref.md");
            File.WriteAllText(refContentPath, 
@"---
title: ref
tags: reftag
---

ref doc.
");

            var bulkRipper = new BulkRipper(new Ripper(), storeToBasePath);

            await bulkRipper.RipOffAsync(metadata, contentBasePath);

            var resultPath = Path.Combine(
                storeToBasePath, categoryUnderContentBasePath, "temp.html");
            if (File.Exists(resultPath))
            {
                var html = File.ReadAllText(resultPath);
                return html;
            }
            else
            {
                return null;
            }
        }
        finally
        {
            Directory.Delete(basePath, true);
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [TestCase("")]
    [TestCase("aaa")]
    [TestCase("aaa/bbb")]
    public async Task RipOffCategoryLookup1(string subNames)
    {
        var actual = await RipOffContentAsync(
            subNames.Split('/'),
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
      <h1>Category: {category}</h1>
{foreach category.breadcrumbs}
      <h2>Category: {item.name}</h1>
{end}
    {contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [TestCase("")]
    [TestCase("aaa")]
    [TestCase("aaa/bbb")]
    public async Task RipOffCategoryLookup2(string subNames)
    {
        var actual = await RipOffContentAsync(
            subNames.Split('/'),
@"
---
title: hoehoe
category: [hoge1,hoge2,hoge3]
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
      <h1>Category: {category}</h1>
{foreach category.breadcrumbs}
      <h2>Category: {item.name}</h1>
{end}
    {contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [TestCase("")]
    [TestCase("aaa")]
    [TestCase("aaa/bbb")]
    public async Task RipOffRelativePathCalculation(string subNames)
    {
        var actual = await RipOffContentAsync(
            subNames.Split('/'),
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
{foreach tagList tag}
     <h1>Tags: {tag}</h1>
{foreach tag.entries entry}
     <h2>Title: <a href='{relative entry.path}' alt='{entry.path}'>{entry.title}</a>
{end}
{end}
    {contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [TestCase("")]
    [TestCase("aaa")]
    [TestCase("aaa/bbb")]
    public async Task RipOffBreadcrumb(string subNames)
    {
        var actual = await RipOffContentAsync(
            subNames.Split('/'),
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
      {foreach category.breadcrumb}
      <li>{item.name}</li>
      {end}
    </ul>
    {contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    [Test]
    public async Task RipOffUnpublished()
    {
        var actual = await RipOffContentAsync(
            new[] { "" },
@"
---
title: hoehoe
published: false
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
      <h1>Category: {category}</h1>
{foreach category.breadcrumbs}
      <h2>Category: {item.name}</h1>
{end}
    {contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffUnpublished2()
    {
        var actual = await RipOffContentAsync(
            new[] { "" },
@"
---
title: hoehoe
published: 123
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
      <h1>Category: {category}</h1>
{foreach category.breadcrumbs}
      <h2>Category: {item.name}</h1>
{end}
    {contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }

    [Test]
    public async Task RipOffExactPublished()
    {
        var actual = await RipOffContentAsync(
            new[] { "" },
@"
---
title: hoehoe
published: true
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
      <h1>Category: {category}</h1>
{foreach category.breadcrumbs}
      <h2>Category: {item.name}</h1>
{end}
    {contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }
}
