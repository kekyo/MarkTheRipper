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
public sealed class CategoriesTests
{
    private static readonly MetadataContext empty = new();

    [Test]
    public async Task AggregateCategories1()
    {
        var mh1 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content1" },
                { "category",
                    new PartialCategoryEntry("cat1",
                    new PartialCategoryEntry("(root)", null))
                },
            },
            null!);

        var actual = await EntryAggregator.AggregateCategoriesAsync(new[]
        {
            mh1,
        }, empty, default);

        await Verifier.Verify(actual);
    }

    [Test]
    public async Task AggregateCategories2()
    {
        var mh1 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content1" },
                { "category",
                    new PartialCategoryEntry("cat2",
                    new PartialCategoryEntry("cat1",
                    new PartialCategoryEntry("(root)", null)))
                },
            },
            null!);

        var actual = await EntryAggregator.AggregateCategoriesAsync(new[]
        {
            mh1,
        }, empty, default);

        await Verifier.Verify(actual);
    }

    [Test]
    public async Task AggregateCategories3()
    {
        var mh1 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content1" },
                { "category",
                    new PartialCategoryEntry("cat1",
                    new PartialCategoryEntry("(root)", null))
                },
            },
            null!);
        var mh2 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content2" },
                { "category",
                    new PartialCategoryEntry("cat1",
                    new PartialCategoryEntry("(root)", null))
                },
            },
            null!);

        var actual = await EntryAggregator.AggregateCategoriesAsync(new[]
        {
            mh1, mh2,
        }, empty, default);

        await Verifier.Verify(actual);
    }

    [Test]
    public async Task AggregateCategories4()
    {
        var mh1 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content1" },
                { "category",
                    new PartialCategoryEntry("cat1",
                    new PartialCategoryEntry("(root)", null))
                },
            },
            null!);
        var mh2 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content2" },
                { "category",
                    new PartialCategoryEntry("cat2",
                    new PartialCategoryEntry("cat1",
                    new PartialCategoryEntry("(root)", null)))
                },
            },
            null!);

        var actual = await EntryAggregator.AggregateCategoriesAsync(new[]
        {
            mh1, mh2,
        }, empty, default);

        await Verifier.Verify(actual);
    }

    [Test]
    public async Task AggregateCategories5()
    {
        var mh1 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content1" },
                { "category",
                    new PartialCategoryEntry("cat1",
                    new PartialCategoryEntry("(root)", null))
                },
            },
            null!);
        var mh2 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content2" },
                { "category",
                    new PartialCategoryEntry("cat2",
                    new PartialCategoryEntry("cat1",
                    new PartialCategoryEntry("(root)", null)))
                },
            },
            null!);
        var mh3 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content3" },
                { "category",
                    new PartialCategoryEntry("cat3",
                    new PartialCategoryEntry("(root)", null))
                },
            },
            null!);

        var actual = await EntryAggregator.AggregateCategoriesAsync(new[]
        {
            mh1, mh2, mh3,
        }, empty, default);

        await Verifier.Verify(actual);
    }

    [Test]
    public async Task AggregateCategories6()
    {
        var mh1 = new MarkdownEntry(
            new Dictionary<string, object?>
            {
                { "path", "content1" },
            },
            null!);

        var actual = await EntryAggregator.AggregateCategoriesAsync(new[]
        {
            mh1,
        }, empty, default);

        await Verifier.Verify(actual);
    }

    [Test]
    public async Task AggregateCategories7()
    {
        var mh1 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content1" },
                { "category",
                    new PartialCategoryEntry("cat1",
                    new PartialCategoryEntry("(root)", null))
                },
            },
            null!);
        var mh2 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content2" },
                { "category",
                    new PartialCategoryEntry("cat2",
                    new PartialCategoryEntry("cat1",
                    new PartialCategoryEntry("(root)", null)))
                },
            },
            null!);
        var mh3 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content2" },
                { "category",
                    new PartialCategoryEntry("cat2",
                    new PartialCategoryEntry("cat1",
                    new PartialCategoryEntry("(root)", null)))
                },
            },
            null!);

        var actual = await EntryAggregator.AggregateCategoriesAsync(new[]
        {
            mh1, mh2, mh3,
        }, empty, default);

        await Verifier.Verify(actual);
    }

    [Test]
    public async Task AggregateCategories8()
    {
        var mh1 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content1" },
                { "category",
                    new PartialCategoryEntry("cat2",
                    new PartialCategoryEntry("cat1",
                    new PartialCategoryEntry("(root)", null)))
                },
            },
            null!);
        var mh2 = new MarkdownEntry(
            new Dictionary<string, object?>()
            {
                { "path", "content2" },
                { "category",
                    new PartialCategoryEntry("cat2",
                    new PartialCategoryEntry("cat3",
                    new PartialCategoryEntry("(root)", null)))
                },
            },
            null!);

        var actual = await EntryAggregator.AggregateCategoriesAsync(new[]
        {
            mh1, mh2,
        }, empty, default);

        await Verifier.Verify(actual);
    }
}
