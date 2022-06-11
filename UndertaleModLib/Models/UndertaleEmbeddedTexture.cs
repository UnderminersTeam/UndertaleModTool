using ICSharpCode.SharpZipLib.BZip2;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using UndertaleModLib.Util;

namespace UndertaleModLib.Models;

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
    /// Whether or not this embedded texture is scaled. TODO: i think this is wrong?
    /// </summary>
    public uint Scaled { get; set; }

    /// <summary>
    /// The amount of generated mipmap levels. <br/>
    /// GameMaker Studio: 2 only.
    /// </summary>
    public uint GeneratedMips { get; set; }

    /// <summary>
    /// TODO: something. <br/>
    /// GameMaker: Studio 2 only.
    /// </summary>
    public uint TextureBlockSize { get; set; }

    /// <summary>
    /// The texture data in the embedded image.
    /// </summary>
    public TexData TextureData { get; set; } = new TexData();

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(Scaled);
        if (writer.undertaleData.GeneralInfo.Major >= 2)
            writer.Write(GeneratedMips);
        if (writer.undertaleData.GM2022_3)
            writer.Write(TextureBlockSize);
        writer.WriteUndertaleObjectPointer(TextureData);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
        private byte[] _textureBlob;

        /// <summary>
        /// The image data of the texture.
        /// </summary>
        public byte[] TextureBlob { get => _textureBlob; set { _textureBlob = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextureBlob))); } }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        private static readonly byte[] pngHeader = { 137, 80, 78, 71, 13, 10, 26, 10 };
        private static readonly byte[] qoiAndBZipHeader = { 50, 122, 111, 113 };
        private static readonly byte[] qoiHeader = { 102, 105, 111, 113 };

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            if (writer.undertaleData.UseQoiFormat)
            {
                if (writer.undertaleData.UseBZipFormat)
                {
                    writer.Write(qoiAndBZipHeader);

                    // Encode the PNG data back to QOI+BZip2
                    using Bitmap bmp = TextureWorker.GetImageFromByteArray(TextureBlob);
                    writer.Write((short)bmp.Width);
                    writer.Write((short)bmp.Height);
                    byte[] data = QoiConverter.GetArrayFromImage(bmp, writer.undertaleData.GM2022_3 ? 0 : 4);
                    using MemoryStream input = new MemoryStream(data);
                    using MemoryStream output = new MemoryStream(1024);
                    BZip2.Compress(input, output, false, 9);
                    writer.Write(output);
                }
                else
                {
                    // Encode the PNG data back to QOI
                    writer.Write(QoiConverter.GetSpanFromImage(TextureWorker.GetImageFromByteArray(TextureBlob),
                        writer.undertaleData.GM2022_3 ? 0 : 4));
                }
            }
            else
                writer.Write(TextureBlob);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            uint startAddress = reader.Position;

            byte[] header = reader.ReadBytes(8);
            if (!header.SequenceEqual(pngHeader))
            {
                reader.Position = startAddress;

                if (header.Take(4).SequenceEqual(qoiAndBZipHeader))
                {
                    reader.undertaleData.UseQoiFormat = true;
                    reader.undertaleData.UseBZipFormat = true;

                    // Don't really care about the width/height, so skip them, as well as header
                    reader.Position += 8;

                    // Need to fully decompress and convert the QOI data to PNG for compatibility purposes (at least for now)
                    using MemoryStream result = new MemoryStream(1024);
                    BZip2.Decompress(reader.Stream, result, false);
                    result.Seek(0, SeekOrigin.Begin);
                    using Bitmap bmp = QoiConverter.GetImageFromSpan(result.GetBuffer());
                    using MemoryStream final = new MemoryStream();
                    bmp.Save(final, ImageFormat.Png);
                    TextureBlob = final.ToArray();
                    return;
                }
                else if (header.Take(4).SequenceEqual(qoiHeader))
                {
                    reader.undertaleData.UseQoiFormat = true;
                    reader.undertaleData.UseBZipFormat = false;

                    // Need to convert the QOI data to PNG for compatibility purposes (at least for now)
                    using Bitmap bmp = QoiConverter.GetImageFromStream(reader.Stream);
                    using MemoryStream final = new MemoryStream();
                    bmp.Save(final, ImageFormat.Png);
                    TextureBlob = final.ToArray();
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