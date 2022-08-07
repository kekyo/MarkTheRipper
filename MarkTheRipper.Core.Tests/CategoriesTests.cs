/////////////////////////////////////////////////////////////////////////////////////
//
// MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using MarkTheRipper.Internal;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using VerifyNUnit;

using static NUnit.Framework.Assert;

namespace MarkTheRipper;

[TestFixture]
public sealed class CategoriesTests
{
    [Test]
    public Task AggregateCategories1()
    {
        var mh1 = new MarkdownHeader(
            "content1",
            new Dictionary<string, object?>()
            {
                { "category", new[] { "cat1", } },
            },
            null!);

        var actual = EntryAggregator.AggregateCategories(new[]
        {
            mh1,
        });

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateCategories2()
    {
        var mh1 = new MarkdownHeader(
            "content1",
            new Dictionary<string, object?>()
            {
                { "category", new[] { "cat1", "cat2", } },
            },
            null!);

        var actual = EntryAggregator.AggregateCategories(new[]
        {
            mh1,
        });

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateCategories3()
    {
        var mh1 = new MarkdownHeader(
            "content1",
            new Dictionary<string, object?>()
            {
                { "category", new[] { "cat1", } },
            },
            null!);
        var mh2 = new MarkdownHeader(
            "content2",
            new Dictionary<string, object?>()
            {
                { "category", new[] { "cat1", } },
            },
            null!);

        var actual = EntryAggregator.AggregateCategories(new[]
        {
            mh1, mh2,
        });

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateCategories4()
    {
        var mh1 = new MarkdownHeader(
            "content1",
            new Dictionary<string, object?>()
            {
                { "category", new[] { "cat1", } },
            },
            null!);
        var mh2 = new MarkdownHeader(
            "content2",
            new Dictionary<string, object?>()
            {
                { "category", new[] { "cat1", "cat2" } },
            },
            null!);

        var actual = EntryAggregator.AggregateCategories(new[]
        {
            mh1, mh2,
        });

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateCategories5()
    {
        var mh1 = new MarkdownHeader(
            "content1",
            new Dictionary<string, object?>()
            {
                { "category", new[] { "cat1", } },
            },
            null!);
        var mh2 = new MarkdownHeader(
            "content2",
            new Dictionary<string, object?>()
            {
                { "category", new[] { "cat1", "cat2" } },
            },
            null!);
        var mh3 = new MarkdownHeader(
            "content3",
            new Dictionary<string, object?>()
            {
                { "category", new[] { "cat3" } },
            },
            null!);

        var actual = EntryAggregator.AggregateCategories(new[]
        {
            mh1, mh2, mh3,
        });

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateCategories6()
    {
        var mh1 = new MarkdownHeader(
            "content1",
            new Dictionary<string, object?>()
            {
                { "category", new string[0] },
            },
            null!);

        var actual = EntryAggregator.AggregateCategories(new[]
        {
            mh1,
        });

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateCategories7()
    {
        var mh1 = new MarkdownHeader(
            "content1",
            new Dictionary<string, object?>()
            {
                { "category", new[] { "cat1", } },
            },
            null!);
        var mh2 = new MarkdownHeader(
            "content2",
            new Dictionary<string, object?>()
            {
                { "category", new[] { "cat1", "cat2" } },
            },
            null!);
        var mh3 = new MarkdownHeader(
            "content2",
            new Dictionary<string, object?>()
            {
                { "category", new[] { "cat1", "cat2" } },
            },
            null!);

        var actual = EntryAggregator.AggregateCategories(new[]
        {
            mh1, mh2, mh3,
        });

        return Verifier.Verify(actual);
    }

    [Test]
    public Task AggregateCategories8()
    {
        var mh1 = new MarkdownHeader(
            "content1",
            new Dictionary<string, object?>()
            {
                { "category", new[] { "cat1", "cat2", } },
            },
            null!);
        var mh2 = new MarkdownHeader(
            "content2",
            new Dictionary<string, object?>()
            {
                { "category", new[] { "cat3", "cat2" } },
            },
            null!);

        var actual = EntryAggregator.AggregateCategories(new[]
        {
            mh1, mh2,
        });

        return Verifier.Verify(actual);
    }
}
