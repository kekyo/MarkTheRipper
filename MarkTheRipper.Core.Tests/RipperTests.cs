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
public sealed class RipperTests
{
    private static async ValueTask<string> RipOffContentAsync(
        string markdownText, string templateName, string templateText,
        params (string keyName, object? value)[] baseMetadata)
    {
        var template = await Ripper.ParseTemplateAsync(
            "test.html", templateText, default);
        var templates = new Dictionary<string, RootTemplateNode>(StringComparer.OrdinalIgnoreCase)
        {
            { templateName, template },
        };

        var markdownReader = new StringReader(markdownText);
        var htmlWriter = new StringWriter();

        var appliedName = await Ripper.RipOffContentAsync(
            markdownReader,
            templates,
            baseMetadata.ToDictionary(entry => entry.keyName, entry => entry.value),
            htmlWriter,
            default);

        AreEqual(templateName, appliedName);

        return htmlWriter.ToString();
    }

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

    [Test]
    public async Task RipOffWithExplicitTemplate()
    {
        var actual = await RipOffContentAsync(
@"
---
title: hoehoe
tags: foo,bar
template: baz
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
    <p>Date: {date:yyyy/MM/dd HH:mm:ss.fff zzz}</p>
{contentBody}</body>
</html>
",
("date", date));

        await Verifier.Verify(actual);
    }
}
