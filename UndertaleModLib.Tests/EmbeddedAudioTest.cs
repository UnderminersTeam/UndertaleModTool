using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModLib.Tests
{
    public class EmbeddedAudioTest
    {
        [Theory]
        [InlineData(new byte[]
            {
                4, 0, 0, 0,
                252, 253, 254, 255,
            }
        )]
        public void TestUnserialize(byte[] data)
        {
            using var stream = new MemoryStream(data);
            var reader = new UndertaleReader(stream);
            var embeddedAudio = new UndertaleEmbeddedAudio();

            embeddedAudio.Unserialize(reader);

            Assert.True(embeddedAudio.Data.Length == BitConverter.ToInt32(data[..4]));
            Assert.Equal(embeddedAudio.Data, data[4..]);
        }

        [Theory]
        [InlineData(new byte[]
            {
                4, 0, 0, 0,
                252, 253, 254, 255
            }
        )]
        public void TestSerialize(byte[] data)
        {
            using var stream = new MemoryStream();
            UndertaleEmbeddedAudio audio = new UndertaleEmbeddedAudio()
            {
                Name = new UndertaleString("foobar"),
                Data = data[4..]
            };
            var writer = new UndertaleWriter(stream);

            audio.Serialize(writer);

            Assert.True(stream.Length == data.Length);
            Assert.Equal(stream.ToArray(), data);
        }
    }
}
