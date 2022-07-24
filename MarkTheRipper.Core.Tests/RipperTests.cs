/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using VerifyNUnit;

namespace MarkTheRipper;

[TestFixture]
public sealed class RipperTests
{
    private static async ValueTask<string> RipOffContentAsync(
        string markdownContent, string template,
        params (string keyName, object? value)[] baseMetadata) =>
        await Ripper.RipOffContentAsync(
            markdownContent,
            await Ripper.ParseTemplateAsync("test.html", template, default),
            baseMetadata.ToDictionary(entry => entry.keyName, entry => entry.value),
            default);

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
", @"<!DOCTYPE html>
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
}
