/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VerifyNUnit;

using static NUnit.Framework.Assert;

namespace MarkTheRipper;

[TestFixture]
public sealed class BulkRipperTests
{
    private static async ValueTask<string> RipOffContentAsync(
        string[] categoryNames, string markdownText, string templateName, string templateText,
        params (string keyName, object? value)[] baseMetadata)
    {
        var template = await Ripper.ParseTemplateAsync(
            "test.html", templateText, default);
        var templates = new Dictionary<string, RootTemplateNode>()
        {
            { templateName, template },
        };

        var metadata = baseMetadata.ToDictionary(
            entry => entry.keyName, entry => entry.value);

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

            var ripper = new BulkRipper(
                storeToBasePath,
                templateName => templates.TryGetValue(templateName, out var template) ? template : null,
                keyName => metadata.TryGetValue(keyName, out var value) ? value : null);

            await ripper.RipOffAsync(contentBasePath);

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
    {foreach:category}
      <h1>Category: {category-item}</h1>
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
    {foreach:category}
      <h1>Category: {category-item}</h1>
    {/}
    {contentBody}
  </body>
</html>
");
        await Verifier.Verify(actual);
    }
}
