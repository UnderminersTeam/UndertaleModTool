using ICSharpCode.SharpZipLib.BZip2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Util;

namespace UndertaleModLib.Models
{
    /// <summary>
    /// An embedded texture entry in the data file.
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleEmbeddedTexture : UndertaleNamedResource
    {
        /// <summary>
        /// The name of the embedded texture entry.
        /// </summary>
        public UndertaleString Name { get; set; }

        /// <summary>
        /// Whether or not this embedded texture is scaled.
        /// </summary>
        public uint Scaled { get; set; } = 0;

        /// <summary>
        /// The amount of generated mipmap levels.
        /// </summary>
        public uint GeneratedMips { get; set; }


        public uint TextureBlockSize { get; set; }

        /// <summary>
        /// The texture data in the embedded image.
        /// </summary>
        public TexData TextureData { get; set; } = new TexData();

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(Scaled);
            if (writer.undertaleData.GeneralInfo.Major >= 2)
                writer.Write(GeneratedMips);
            if (writer.undertaleData.GM2022_3)
                writer.Write(TextureBlockSize);
            writer.WriteUndertaleObjectPointer(TextureData);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Scaled = reader.ReadUInt32();
            if (reader.undertaleData.GeneralInfo.Major >= 2)
                GeneratedMips = reader.ReadUInt32();
            if (reader.undertaleData.GM2022_3)
                TextureBlockSize = reader.ReadUInt32();
            TextureData = reader.ReadUndertaleObjectPointer<TexData>();
        }

        /// <summary>
        /// TODO!
        /// </summary>
        /// <param name="writer">Where to serialize to.</param>
        public void SerializeBlob(UndertaleWriter writer)
        {
            // padding
            while (writer.Position % 0x80 != 0)
                writer.Write((byte)0);

            writer.WriteUndertaleObject(TextureData);
        }

        /// <summary>
        /// TODO!
        /// </summary>
        /// <param name="reader">Where to deserialize from.</param>
        public void UnserializeBlob(UndertaleReader reader)
        {
            while (reader.Position % 0x80 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            reader.ReadUndertaleObject(TextureData);
        }

        public override string ToString()
        {
            if (Name != null)
                return Name.Content + " (" + GetType().Name + ")";
            else
                Name = new UndertaleString("Texture Unknown Index");
            return Name.Content + " (" + GetType().Name + ")";
        }

        /// <summary>
        /// Texture data in an <see cref="UndertaleEmbeddedTexture"/>.
        /// </summary>
        public class TexData : UndertaleObject, INotifyPropertyChanged
        {
            private Bitmap _Image;

            /// <summary>
            /// The PNG image data of the texture.
            /// </summary>
            [Obsolete($"{nameof(TextureBlob)} is obsolete. Use {nameof(Image)} instead.", false)]
            public byte[] TextureBlob
            {
                get
                {
                    using MemoryStream final = new();
                    Image.Save(final, ImageFormat.Png);
                    return final.ToArray();
                }
                set => Image = TextureWorker.GetImageFromByteArray(value);
            }

            /// <summary>
            /// The image data of the texture.
            /// </summary>
            public Bitmap Image { get => _Image; set { _Image = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextureBlob))); } }

            public event PropertyChangedEventHandler PropertyChanged;

            private static readonly byte[] PNGHeader = new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 };
            private static readonly byte[] QOIandBZipHeader = new byte[4] { 50, 122, 111, 113 };
            private static readonly byte[] QOIHeader = new byte[4] { 102, 105, 111, 113 };

            public void Serialize(UndertaleWriter writer)
            {
                if (writer.undertaleData.UseQoiFormat)
                {
                    if (writer.undertaleData.UseBZipFormat)
                    {
                        writer.Write(QOIandBZipHeader);

                        // Encode the bitmap back to QOI+BZip2
                        writer.Write((short)Image.Width);
                        writer.Write((short)Image.Height);
                        byte[] data = QoiConverter.GetArrayFromImage(Image, writer.undertaleData.GM2022_3 ? 0 : 4);
                        using MemoryStream input = new MemoryStream(data);
                        using MemoryStream output = new MemoryStream(1024);
                        BZip2.Compress(input, output, false, 9);
                        writer.Write(output);
                    }
                    else
                    {
                        writer.Write(QoiConverter.GetSpanFromImage(Image, writer.undertaleData.GM2022_3 ? 0 : 4));
                    }
                }
                else
                    writer.Write(TextureBlob);
            }

            public void Unserialize(UndertaleReader reader)
            {
                uint startAddress = reader.Position;

                byte[] header = reader.ReadBytes(8);
                if (!header.SequenceEqual(PNGHeader))
                {
                    reader.Position = startAddress;

                    if (header.Take(4).SequenceEqual(QOIandBZipHeader))
                    {
                        reader.undertaleData.UseQoiFormat = true;
                        reader.undertaleData.UseBZipFormat = true;

                        // Don't really care about the width/height, so skip them, as well as header
                        reader.Position += 8;

                        using MemoryStream bufferWrapper = new MemoryStream(reader.Buffer);
                        bufferWrapper.Seek(reader.Offset, SeekOrigin.Begin);
                        using MemoryStream result = new MemoryStream(1024);
                        BZip2.Decompress(bufferWrapper, result, false);
                        reader.Position = (uint)bufferWrapper.Position;
                        result.Seek(0, SeekOrigin.Begin);
                        Image = QoiConverter.GetImageFromSpan(result.GetBuffer());
                        return;
                    }
                    else if (header.Take(4).SequenceEqual(QOIHeader))
                    {
                        reader.undertaleData.UseQoiFormat = true;
                        reader.undertaleData.UseBZipFormat = false;

                        Image = QoiConverter.GetImageFromSpan(reader.Buffer.AsSpan()[reader.Offset..], out int dataLength);
                        reader.Offset += dataLength;
                        return;
                    }
                    else
                        throw new IOException("Didn't find PNG or QOI+BZip2 header");
                }

                // There is no length for the PNG anywhere as far as I can see
                // The only thing we can do is parse the image to find the end
                while (true)
                {
                    // PNG is big endian and BinaryRead can't handle that (damn)
                    uint len = (uint)reader.ReadByte() << 24 | (uint)reader.ReadByte() << 16 | (uint)reader.ReadByte() << 8 | (uint)reader.ReadByte();
                    string type = Encoding.UTF8.GetString(reader.ReadBytes(4));
                    reader.Position += len + 4;
                    if (type == "IEND")
                        break;
                }

                uint length = reader.Position - startAddress;
                reader.Position = startAddress;
                TextureBlob = reader.ReadBytes((int)length);
            }
        }
    }
}
