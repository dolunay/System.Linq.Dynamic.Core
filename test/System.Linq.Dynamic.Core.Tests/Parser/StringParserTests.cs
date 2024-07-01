﻿using System.Linq.Dynamic.Core.Exceptions;
using System.Linq.Dynamic.Core.Parser;
using FluentAssertions;
using Xunit;

namespace System.Linq.Dynamic.Core.Tests.Parser;

public class StringParserTests
{
    [Theory]
    [InlineData("'s")]
    [InlineData("\"s")]
    public void StringParser_ParseStringAndUnescape_With_UnexpectedUnclosedString_ThrowsException(string input)
    {
        // Act
        var exception = Assert.Throws<ParseException>(() => StringParser.ParseStringAndUnescape(input));

        // Assert
        Assert.Equal($"Unexpected end of string with unclosed string at position 2 near '{input}'.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("x")]
    public void StringParser_ParseStringAndUnescape_With_InvalidStringLength_ThrowsException(string input)
    {
        // Act
        Action action = () => StringParser.ParseStringAndUnescape(input);

        // Assert
        action.Should().Throw<ParseException>().WithMessage($"String '{input}' should have at least 2 characters.");
    }

    [Theory]
    [InlineData("xx")]
    [InlineData("  ")]
    public void StringParser_ParseStringAndUnescape_With_InvalidStringQuoteCharacter_ThrowsException(string input)
    {
        // Act
        Action action = () => StringParser.ParseStringAndUnescape(input);

        // Assert
        action.Should().Throw<ParseException>().WithMessage("An escaped string should start with a double (\") or a single (') quote.");
    }

    [Fact]
    public void StringParser_ParseStringAndUnescape_With_UnexpectedUnrecognizedEscapeSequence_ThrowsException()
    {
        // Arrange
        var input = new string(new[] { '"', '\\', 'u', '?', '"' });

        // Act
        Action action = () => StringParser.ParseStringAndUnescape(input);

        // Assert
        var parseException = action.Should().Throw<ParseException>();

        parseException.Which.InnerException!.Message.Should().Contain("hexadecimal digits");

        parseException.Which.StackTrace.Should().Contain("at System.Linq.Dynamic.Core.Parser.StringParser.ParseStringAndUnescape(String s, Int32 pos) in ").And.Contain("StringParser.cs:line ");
    }

    [Theory]
    [InlineData("''", "")]
    [InlineData("'s'", "s")]
    [InlineData("'\\\\'", "\\")]
    [InlineData("'\\n'", "\n")]
    public void StringParser_ParseStringAndUnescape_SingleQuotedString(string input, string expectedResult)
    {
        // Act
        var result = StringParser.ParseStringAndUnescape(input);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("\"\"", "")]
    [InlineData("\"\\\\\"", "\\")]
    [InlineData("\"\\n\"", "\n")]
    [InlineData("\"\\\\n\"", "\\n")]
    [InlineData("\"\\\\new\"", "\\new")]
    [InlineData("\"[]\"", "[]")]
    [InlineData("\"()\"", "()")]
    [InlineData("\"(\\\"\\\")\"", "(\"\")")]
    [InlineData("\"/\"", "/")]
    [InlineData("\"a\"", "a")]
    [InlineData("\"This \\\"is\\\" a test.\"", "This \"is\" a test.")]
    [InlineData(@"""This \""is\"" b test.""", @"This ""is"" b test.")]
    [InlineData("\"ab\\\"cd\"", "ab\"cd")]
    [InlineData("\"\\\"\"", "\"")]
    [InlineData("\"\\\"\\\"\"", "\"\"")]
    [InlineData("\"AB YZ 19 \uD800\udc05 \u00e4\"", "AB YZ 19 \uD800\udc05 \u00e4")]
    [InlineData("\"\\\\\\\\192.168.1.1\\\\audio\\\\new\"", "\\\\192.168.1.1\\audio\\new")]
    [InlineData("\"{\\\"PropertyA\\\":\\\"\\\"}\"", @"{""PropertyA"":""""}")] // #786
    public void StringParser_ParseStringAndUnescape_DoubleQuotedString(string input, string expectedResult)
    {
        // Act
        var result = StringParser.ParseStringAndUnescape(input);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void StringParser_ParseStringAndUnescape()
    {
        // Arrange
        var test = "\"x\\\"X\"";

        // Act
        var result = StringParser.ParseStringAndUnescape(test);

        // Assert
        result.Should().Be("x\"X");
    }

    [Fact]
    public void StringParser_ParseStringAndUnescapeTwoDoubleQuotesByASingleDoubleQuote()
    {
        // Arrange
        var test = "\"x\"\"X\"";

        // Act
        var result = StringParser.ParseStringAndUnescapeTwoDoubleQuotesByASingleDoubleQuote(test);

        // Assert
        result.Should().Be("x\"X");
    }
}