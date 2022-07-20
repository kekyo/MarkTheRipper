/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using VerifyNUnit;

namespace MarkTheRipper;

[TestFixture]
public sealed class RipperTests
{
    [Test]
    public async Task RipOff()
    {
        var actual = await Ripper.RipOffContentAsync(
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
{body}</body>
</html>
",
            new Dictionary<string, string>(),
            default);
        await Verifier.Verify(actual);
    }
}
