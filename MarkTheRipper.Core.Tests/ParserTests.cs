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
using System.Linq;
using System.Threading.Tasks;
using VerifyNUnit;

using static NUnit.Framework.Assert;

namespace MarkTheRipper;

[TestFixture]
public sealed class ParserTests
{
    [Test]
    public Task ParseDoubleQuoted()
    {
        var actual = Parser.ParseExpression("\"abc\"");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseDoubleQuoted2()
    {
        var actual = Parser.ParseExpression("\"abc,def\"", ListTypes.Array);

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseDoubleQuoted3()
    {
        var actual = Parser.ParseExpression("\"abc'def\"");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseDoubleQuoted4()
    {
        var actual = Parser.ParseExpression(" \"abc\"");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseDoubleQuoted5()
    {
        var actual = Parser.ParseExpression("\"abc\" ");

        return Verifier.Verify(actual);
    }

    //////////////////////////////////////////////////////

    [Test]
    public Task ParseSingleQuoted()
    {
        var actual = Parser.ParseExpression("'abc'");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseSingleQuoted2()
    {
        var actual = Parser.ParseExpression("'abc,def'", ListTypes.Array);

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseSingleQuoted3()
    {
        var actual = Parser.ParseExpression("'abc\"def'");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseSingleQuoted4()
    {
        var actual = Parser.ParseExpression(" 'abc'");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseSingleQuoted5()
    {
        var actual = Parser.ParseExpression("'abc' ");

        return Verifier.Verify(actual);
    }

    //////////////////////////////////////////////////////

    [Test]
    public Task ParseVariavble1()
    {
        var actual = Parser.ParseExpression("abc");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseVariavble2()
    {
        var actual = Parser.ParseExpression("abc.def");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseVariavble3()
    {
        var actual = Parser.ParseExpression("_abc");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseVariavble4()
    {
        var actual = Parser.ParseExpression("_abc_def");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseVariavble5()
    {
        var actual = Parser.ParseExpression("abc123");

        return Verifier.Verify(actual);
    }

    //////////////////////////////////////////////////////

    [Test]
    public Task ParseNumeric1()
    {
        var actual = Parser.ParseExpression("123");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseNumeric2()
    {
        var actual = Parser.ParseExpression("123.456");

        return Verifier.Verify(actual);
    }

    //////////////////////////////////////////////////////

    [Test]
    public Task ParseDateTimeOffset1()
    {
        var actual = Parser.ParseExpression("2022/08/20 12:34:56", ListTypes.Array);

        return Verifier.Verify(actual.PrettyPrint);
    }

    //////////////////////////////////////////////////////

    [Test]
    public Task ParseBoolean1()
    {
        var actual = Parser.ParseExpression("true");

        return Verifier.Verify(actual);
    }

    //////////////////////////////////////////////////////

    [Test]
    public Task ParseOtherValue1()
    {
        var actual = Parser.ParseExpression("123abc");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseOtherValue2()
    {
        var actual = Parser.ParseExpression("https://example.com/");

        return Verifier.Verify(actual);
    }

    //////////////////////////////////////////////////////

    [Test]
    public Task ParseSeparated1()
    {
        var actual = Parser.ParseExpression("123,456", ListTypes.Array);

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseSeparated2()
    {
        var actual = Parser.ParseExpression("abc,123", ListTypes.Array);

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseSeparated3()
    {
        var actual = Parser.ParseExpression("abc.def,123", ListTypes.Array);

        return Verifier.Verify(actual);
    }

    //////////////////////////////////////////////////////

    [Test]
    public Task ParseList0()
    {
        var actual = Parser.ParseExpression("123 456");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseList1()
    {
        var actual = Parser.ParseExpression("(123 456)");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseList2()
    {
        var actual = Parser.ParseExpression("((123 456) 789)");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseList3()
    {
        var actual = Parser.ParseExpression("(123 (456 789))");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseList4()
    {
        var actual = Parser.ParseExpression("((123 456)789)");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseList5()
    {
        var actual = Parser.ParseExpression("(123(456 789))");

        return Verifier.Verify(actual);
    }

    //////////////////////////////////////////////////////

    [Test]
    public Task ParseArray0()
    {
        var actual = Parser.ParseExpression("123,456", ListTypes.Array);

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseArray1()
    {
        var actual = Parser.ParseExpression("[123,456]", ListTypes.Array);

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseArray2()
    {
        var actual = Parser.ParseExpression("[[123,456],789]", ListTypes.Array);

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseArray3()
    {
        var actual = Parser.ParseExpression("[123,[456,789]]", ListTypes.Array);

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseArray4()
    {
        var actual = Parser.ParseExpression("[[123,456]789]", ListTypes.Array);

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseArray5()
    {
        var actual = Parser.ParseExpression("[123[456,789]]", ListTypes.Array);

        return Verifier.Verify(actual);
    }

    //////////////////////////////////////////////////////

    [Test]
    public Task ParseComplex1()
    {
        var actual = Parser.ParseExpression("abc (def 123) ghi");

        return Verifier.Verify(actual);
    }

    [Test]
    public Task ParseComplex2()
    {
        var actual = Parser.ParseExpression("abc ('def' [123, 456]) \"ghi\"");

        return Verifier.Verify(actual);
    }
}
