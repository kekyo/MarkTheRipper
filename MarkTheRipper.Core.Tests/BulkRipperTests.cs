/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Metadata;
using MarkTheRipper.Template;
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
    private static async ValueTask<string> RipOffContentAsync(
        string[] categoryNames, string markdownText, string templateName, string templateText,
        params (string keyName, object? value)[] baseMetadata)
    {
        var metadata = new MetadataContext();

        var template = await Ripper.ParseTemplateAsync(
            templateName, templateText, default);
        var templateList = new Dictionary<string, RootTemplateNode>
        {
            { templateName, template }
        };
        metadata.Set("template", new PartialTemplateEntry(templateName));
        metadata.Set("templateList", templateList);

        foreach (var entry in baseMetadata)
        {
            metadata.Set(entry.keyName, entry.value);
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

            var bulkRipper = new BulkRipper(storeToBasePath);

            await bulkRipper.RipOffAsync(metadata, contentBasePath);

            var html = File.ReadAllText(
                Path.Combine(storeToBasePath, categoryUnderContentBasePath, "temp.html"));
            return html;
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
      <h1>Category: {category}</h1>
{foreach:category.path}
      <h2>Category: {item.name}</h1>
{/}
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
      <h1>Category: {category}</h1>
{foreach:category.path}
      <h2>Category: {item.name}</h1>
{/}
    {contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }
}
