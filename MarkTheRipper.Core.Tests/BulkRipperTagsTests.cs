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

using static NUnit.Framework.Assert;

namespace MarkTheRipper;

[TestFixture]
public sealed class BulkRipperTagsTests
{
    [Test]
    public Task AggregateTags1()
    {
        var mh1 = new MarkdownHeader(
            "content1",
            new Dictionary<string, object?>()
            {
                { "tags", new[] { "tag1", } },
            });

        var actual = BulkRipper.AggregateTags(new[]
        {
            ("base", mh1),
        });

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateTags2()
    {
        var mh1 = new MarkdownHeader(
            "content1",
            new Dictionary<string, object?>()
            {
                { "tags", new[] { "tag1", "tag2", } },
            });

        var actual = BulkRipper.AggregateTags(new[]
        {
            ("base", mh1),
        });

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateTags3()
    {
        var mh1 = new MarkdownHeader(
            "content1",
            new Dictionary<string, object?>()
            {
                { "tags", new[] { "tag1", } },
            });
        var mh2 = new MarkdownHeader(
            "content2",
            new Dictionary<string, object?>()
            {
                { "tags", new[] { "tag1", } },
            });

        var actual = BulkRipper.AggregateTags(new[]
        {
            ("base", mh1),
            ("base", mh2),
        });

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateTags4()
    {
        var mh1 = new MarkdownHeader(
            "content1",
            new Dictionary<string, object?>()
            {
                { "tags", new[] { "tag1", } },
            });
        var mh2 = new MarkdownHeader(
            "content2",
            new Dictionary<string, object?>()
            {
                { "tags", new[] { "tag1", "tag2", } },
            });

        var actual = BulkRipper.AggregateTags(new[]
        {
            ("base", mh1),
            ("base", mh2),
        });

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateTags5()
    {
        var mh1 = new MarkdownHeader(
            "content1",
            new Dictionary<string, object?>()
            {
                { "tags", new[] { "tag1", } },
            });
        var mh2 = new MarkdownHeader(
            "content2",
            new Dictionary<string, object?>()
            {
                { "tags", new[] { "tag1", "tag2", } },
            });
        var mh3 = new MarkdownHeader(
            "content3",
            new Dictionary<string, object?>()
            {
                { "tags", new[] { "tag3", } },
            });

        var actual = BulkRipper.AggregateTags(new[]
        {
            ("base", mh1),
            ("base", mh2),
            ("base", mh3),
        });

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateTags6()
    {
        var mh1 = new MarkdownHeader(
            "content1",
            new Dictionary<string, object?>()
            {
            });

        var actual = BulkRipper.AggregateTags(new[]
        {
            ("base", mh1),
        });

        return Verifier.Verify(actual);
    }

}
