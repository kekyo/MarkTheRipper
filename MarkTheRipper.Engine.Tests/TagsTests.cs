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
    public async Task AggregateTags1()
    {
        var mh1 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content1" },
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                } },
            },
            null!);

        var actual = await EntryAggregator.AggregateTagsAsync(new[]
        {
            mh1,
        }, empty, default);

        await Verifier.Verify(actual);
    }

    [Test]
    public async Task AggregateTags2()
    {
        var mh1 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content1" },
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                    new PartialTagEntry("tag2"),
                } },
            },
            null!);

        var actual = await EntryAggregator.AggregateTagsAsync(new[]
        {
            mh1,
        }, empty, default);

        await Verifier.Verify(actual);
    }

    [Test]
    public async Task AggregateTags3()
    {
        var mh1 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content1" },
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                } },
            },
            null!);
        var mh2 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content2" },
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                } },
            },
            null!);

        var actual = await EntryAggregator.AggregateTagsAsync(new[]
        {
            mh1, mh2,
        }, empty, default);

        await Verifier.Verify(actual);
    }

    [Test]
    public async Task AggregateTags4()
    {
        var mh1 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content1" },
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                } },
            },
            null!);
        var mh2 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content2" },
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                    new PartialTagEntry("tag2"),
                } },
            },
            null!);

        var actual = await EntryAggregator.AggregateTagsAsync(new[]
        {
            mh1, mh2,
        }, empty, default);

        await Verifier.Verify(actual);
    }

    [Test]
    public async Task AggregateTags5()
    {
        var mh1 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content1" },
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                } },
            },
            null!);
        var mh2 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content2" },
                { "tags", new[] {
                    new PartialTagEntry("tag1"),
                    new PartialTagEntry("tag2"),
                } },
            },
            null!);
        var mh3 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content3" },
                { "tags", new[] {
                    new PartialTagEntry("tag3"),
                } },
            },
            null!);

        var actual = await EntryAggregator.AggregateTagsAsync(new[]
        {
            mh1, mh2, mh3,
        }, empty, default);

        await Verifier.Verify(actual);
    }

    [Test]
    public async Task AggregateTags6()
    {
        var mh1 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content1" },
            },
            null!);

        var actual = await EntryAggregator.AggregateTagsAsync(new[]
        {
            mh1,
        }, empty, default);

        await Verifier.Verify(actual);
    }
}
