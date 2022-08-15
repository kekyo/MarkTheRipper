/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Metadata;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using VerifyNUnit;

using static NUnit.Framework.Assert;

namespace MarkTheRipper;

[TestFixture]
public sealed class TagsTests
{
    private static readonly MetadataContext empty = new();

    [Test]
    public Task AggregateTags1()
    {
        var mh1 = new MarkdownEntry(
            "content1",
            new Dictionary<string, object?>()
            {
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                } },
            },
            null!);

        var actual = EntryAggregator.AggregateTags(new[]
        {
            mh1,
        }, empty);

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateTags2()
    {
        var mh1 = new MarkdownEntry(
            "content1",
            new Dictionary<string, object?>()
            {
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                    new PartialTagEntry("tag2"),
                } },
            },
            null!);

        var actual = EntryAggregator.AggregateTags(new[]
        {
            mh1,
        }, empty);

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateTags3()
    {
        var mh1 = new MarkdownEntry(
            "content1",
            new Dictionary<string, object?>()
            {
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                } },
            },
            null!);
        var mh2 = new MarkdownEntry(
            "content2",
            new Dictionary<string, object?>()
            {
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                } },
            },
            null!);

        var actual = EntryAggregator.AggregateTags(new[]
        {
            mh1, mh2,
        }, empty);

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateTags4()
    {
        var mh1 = new MarkdownEntry(
            "content1",
            new Dictionary<string, object?>()
            {
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                } },
            },
            null!);
        var mh2 = new MarkdownEntry(
            "content2",
            new Dictionary<string, object?>()
            {
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                    new PartialTagEntry("tag2"),
                } },
            },
            null!);

        var actual = EntryAggregator.AggregateTags(new[]
        {
            mh1, mh2,
        }, empty);

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateTags5()
    {
        var mh1 = new MarkdownEntry(
            "content1",
            new Dictionary<string, object?>()
            {
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                } },
            },
            null!);
        var mh2 = new MarkdownEntry(
            "content2",
            new Dictionary<string, object?>()
            {
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                    new PartialTagEntry("tag2"),
                } },
            },
            null!);
        var mh3 = new MarkdownEntry(
            "content3",
            new Dictionary<string, object?>()
            {
                { "tags", new[] {
                    new PartialTagEntry("tag3"),
                } },
            },
            null!);

        var actual = EntryAggregator.AggregateTags(new[]
        {
            mh1, mh2, mh3,
        }, empty);

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateTags6()
    {
        var mh1 = new MarkdownEntry(
            "content1",
            new Dictionary<string, object?>()
            {
            },
            null!);

        var actual = EntryAggregator.AggregateTags(new[]
        {
            mh1,
        }, empty);

        return Verifier.Verify(actual);
    }
}
