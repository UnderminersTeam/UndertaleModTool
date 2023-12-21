using UndertaleModLib;
using UndertaleModLib.Models;
using Xunit;
using Xunit.Extensions;

namespace UndertaleModLibTests.Models;

public class EmbeddedAudioTest
{
    [Theory]
    [InlineData(new byte[] 
    { 
        4, 0, 0, 0, 
        252, 253, 254, 255,
    })]

    public void TestUnserialize(byte[] data)
    {
        using var stream = new MemoryStream(data);
        var reader = new UndertaleReader(stream);
        var embeddedAudio = new UndertaleEmbeddedAudio();
        
        embeddedAudio.Unserialize(reader);
        
        Assert.True(embeddedAudio.Data.Length == BitConverter.ToInt32(data[..4]));
        Assert.Equal(embeddedAudio.Data, data[4..]);
    }

    [Fact]
    public void TestSerialize()
    {
        using var stream = new MemoryStream();
        var fullData = new byte[] { 4, 0, 0, 0, 252, 253, 254, 255 };
        UndertaleEmbeddedAudio audio = new UndertaleEmbeddedAudio()
        {
            Name = new UndertaleString("foobar"),
            Data = fullData[4..]
        };
        var writer = new UndertaleWriter(stream);
        
        audio.Serialize(writer);

        Assert.True(stream.Length == fullData.Length);
        Assert.Equal(stream.ToArray(), fullData);
    }
}
