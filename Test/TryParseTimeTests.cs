#if DEBUG
using System;
using System.Collections.Generic;
using Xunit;

namespace Project.Test;

public class TryParseTimeTests {

    [Theory]
    [InlineData("0", true, "00:00:00")]
    [InlineData("123", true, "00:02:03")]
    [InlineData("1:23", true, "00:01:23")]
    [InlineData("1:60", true, "00:02:00")]
    [InlineData("12:34:56", true, "12:34:56")]
    [InlineData("25:0:0", true, "1.01:00:00")]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("a", false)]
    [InlineData("a:b", false)]
    [InlineData("-1", false)]
    [InlineData("1:2:3:4", false)]
    public void Run(string text, bool expectedOK, string expectedResultText = "00:00:00") {
        var ok = TagFile.TryParseTime(text, out var result);
        Assert.Equal(expectedOK, ok);
        Assert.Equal(expectedResultText, result.ToString());
    }
}
#endif
