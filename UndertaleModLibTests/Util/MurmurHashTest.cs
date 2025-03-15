using Xunit;
using UndertaleModLib.Util;

namespace UndertaleModLibTests.Util;

// Test data from https://en.wikipedia.org/wiki/MurmurHash.
public class MurmurHashTest
{
    [Theory]
    [InlineData("", 0, 0)]
    [InlineData("", 1, 0x514e28b7)]
    [InlineData("", 0xffffffff, 0x81f16f39)]
    [InlineData("test", 0, 0xba6bd213)]
    [InlineData("test", 0x9747b28c, 0x704b81dc)]
    [InlineData("Hello, world!", 0, 0xc0363e43)]
    [InlineData("Hello, world!", 0x9747b28c, 0x24884cba)]
    [InlineData("The quick brown fox jumps over the lazy dog", 0, 0x2e4ff723)]
    [InlineData("The quick brown fox jumps over the lazy dog", 0x9747b28c, 0x2fa826cd)]
    public void TestHashStrings(string str, uint seed, uint expectedHash)
    {
        Assert.Equal(expectedHash, MurmurHash.Hash(str, seed));
    }
}
