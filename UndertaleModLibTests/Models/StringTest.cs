using UndertaleModLib;
using UndertaleModLib.Models;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace UndertaleModLibTests.Models;

public class StringTest
{
    [Theory]
    [InlineData("", "")]
    [InlineData("\n", "\n")]
    [InlineData("\n\r\n\r\n", "\n\r\n\r\n")]
    [InlineData("T\nh\ri\"s\\", "T\nh\ri\"s\\")]
    [InlineData(@"\n", "\n")]
    [InlineData(@"\n\r\n\r\n", "\n\r\n\r\n")]
    [InlineData(@"T\nh\ri\""s\\", "T\nh\ri\"s\\")]
    public void TestUnescapeText(string text, string expected)
    {
        var result = UndertaleString.UnescapeText(text);
        
        Assert.Equal(result, expected);
    }

    [Theory]
    [InlineData("", "", true)]
    [InlineData(" ", "", true)]
    [InlineData("HELLO", "hello", true)]
    [InlineData("hello", "HELLO", true)]
    [InlineData("HEllO", "heLLo", true)]
    [InlineData("hello", "world", false)]
    [InlineData("world", "hello", false)]
    [InlineData(null, null, false)]
    [InlineData("hi", null, false)]
    [InlineData(null, "null", false)]
    public void TestSearchMatches(string text, string substring, bool expected)
    {
        UndertaleString utString = new UndertaleString(text);

        var result = utString.SearchMatches(substring);
        
        Assert.Equal(result, expected);
    }

    [Theory]
    [InlineData("I rule!", new byte[] {7, 0, 0, 0, 73, 32, 114, 117, 108, 101, 33, 0})]
    [InlineData("{]öÄü¢€ΩФЙw", new byte[] {20, 0, 0, 0, 123, 93, 195, 182, 195, 132, 195, 188, 194, 162, 226, 130, 172, 206, 169, 208, 164, 208, 153, 119, 0})]
    [InlineData("\ud83d\udc38\ud83d\ude03\ud83e\uddd1\ud83c\udffc\u200d\ud83d\ude80\ud83c\udff4\u200d\u2620\ufe0f", new byte[] {36, 0, 0, 0, 240, 159, 144, 184, 240, 159, 152, 131, 240, 159, 167, 145, 240, 159, 143, 188, 226, 128, 141, 240, 159, 154, 128, 240, 159, 143, 180, 226, 128, 141, 226, 152, 160, 239, 184, 143, 0})]
    public void TestSerialize(string input, byte[] expected)
    {
        using var stream = new MemoryStream();
        UndertaleString utString = new UndertaleString(input);
        var writer = new UndertaleWriter(stream);
        
        utString.Serialize(writer);
        
        Assert.Equal(stream.ToArray(), expected);
    }
    
    [Theory]
    [InlineData(new byte[] {7, 0, 0, 0, 73, 32, 114, 117, 108, 101, 33, 0}, "I rule!")]
    [InlineData(new byte[] {20, 0, 0, 0, 123, 93, 195, 182, 195, 132, 195, 188, 194, 162, 226, 130, 172, 206, 169, 208, 164, 208, 153, 119, 0}, "{]öÄü¢€ΩФЙw")]
    [InlineData(new byte[] {36, 0, 0, 0, 240, 159, 144, 184, 240, 159, 152, 131, 240, 159, 167, 145, 240, 159, 143, 188, 226, 128, 141, 240, 159, 154, 128, 240, 159, 143, 180, 226, 128, 141, 226, 152, 160, 239, 184, 143, 0}, "\ud83d\udc38\ud83d\ude03\ud83e\uddd1\ud83c\udffc\u200d\ud83d\ude80\ud83c\udff4\u200d\u2620\ufe0f")]

    public void TestUnserialize(byte[] input, string expected)
    {
        using var stream = new MemoryStream(input);
        var reader = new UndertaleReader(stream);
        var utString = new UndertaleString();
        
        utString.Unserialize(reader);
        
        Assert.Equal(utString.Content, expected);
    }
}