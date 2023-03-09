using ICSharpCode.SharpZipLib.BZip2;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UndertaleModLib.Util;

namespace UndertaleModLib.Models;

/// <summary>
/// An embedded texture entry in the data file.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleEmbeddedTexture : UndertaleNamedResource, IDisposable,
                                        IStaticChildObjCount, IStaticChildObjectsSize
{
    /// <inheritdoc cref="IStaticChildObjCount.ChildObjectCount" />
    public static readonly uint ChildObjectCount = 1;

    /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
    public static readonly uint ChildObjectsSize = 4; // minimal size

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
    /// GameMaker Studio 2 only.
    /// </summary>
    public uint GeneratedMips { get; set; }

    /// <summary>
    /// Size of the texture attached to this texture page in bytes. Only appears in GM 2022.3+.
    /// </summary>
    private uint _textureBlockSize { get; set; }

    /// <summary>
    /// The position of the placeholder <see cref="_textureBlockSize">TextureBlockSize</see> value
    /// to be overwritten in SerializeBlob. <br/>
    /// Only used internally for GM 2022.3+ support.
    /// </summary>
    private uint _textureBlockSizeLocation { get; set; }

    /// <summary>
    /// The texture data in the embedded image.
    /// </summary>
    public TexData TextureData
    {
        get => _textureData ??= LoadExternalTexture();
        set => _textureData = value;
    }
    private TexData _textureData = new TexData();


    /// <summary>
    /// Helper variable for whether or not this texture is to be stored externally or not.
    /// </summary>
    public bool TextureExternal { get; set; } = false;


    /// <summary>
    /// Helper variable for whether or not a texture was loaded yet.
    /// </summary>
    public bool TextureLoaded { get; set; } = false;

    /// <summary>
    /// Width of the texture. 2022.9+ only.
    /// </summary>
    public int TextureWidth { get; set; }

    /// <summary>
    /// Height of the texture. 2022.9+ only.
    /// </summary>
    public int TextureHeight { get; set; }

    /// <summary>
    /// Index of the texture in the texture group. 2022.9+ only.
    /// </summary>
    public int IndexInGroup { get; set; }

    /// <summary>
    /// Helper reference to texture group info, if found in the data file.
    /// </summary>
    public UndertaleTextureGroupInfo TextureInfo { get; set; }

    /// <summary>
    /// Helper for 2022.9+ support. Stores copy of the path to the data file.
    /// </summary>
    private string _2022_9_GameDirectory { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(Scaled);
        if (writer.undertaleData.IsGameMaker2())
            writer.Write(GeneratedMips);
        if (writer.undertaleData.IsVersionAtLeast(2022, 3))
        {
            // We're going to overwrite this later with the actual size
            // of our texture block, so save the position
            _textureBlockSizeLocation = writer.Position;
            writer.Write(_textureBlockSize);
        }
        if (writer.undertaleData.IsVersionAtLeast(2022, 9))
        {
            writer.Write(TextureWidth);
            writer.Write(TextureHeight);
            writer.Write(IndexInGroup);
        }
        if (TextureExternal)
            writer.Write((int)0); // Ensure null pointer is written with external texture
        else
            writer.WriteUndertaleObjectPointer(_textureData);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Scaled = reader.ReadUInt32();
        if (reader.undertaleData.IsGameMaker2())
            GeneratedMips = reader.ReadUInt32();
        if (reader.undertaleData.IsVersionAtLeast(2022, 3))
            _textureBlockSize = reader.ReadUInt32();
        if (reader.undertaleData.IsVersionAtLeast(2022, 9))
        {
            TextureWidth = reader.ReadInt32();
            TextureHeight = reader.ReadInt32();
            IndexInGroup = reader.ReadInt32();
            _2022_9_GameDirectory = reader.Directory;
        }
        _textureData = reader.ReadUndertaleObjectPointer<TexData>();
        TextureExternal = (_textureData == null);
    }

    /// <summary>
    /// Serializes the in-file texture blob for this texture.
    /// </summary>
    /// <param name="writer">Where to serialize to.</param>
    public void SerializeBlob(UndertaleWriter writer)
    {
        // If external, don't serialize blob
        // Has sanity check for data being null as well, although the external flag should be set
        // FIXME: Implement external texture writing
        // When we implement the above, we should also write the texture's actual size to
        // TextureBlockSize because GM does it
        // (behavior observed in a VM game built with Runtime 2022.11.1.75)
        if (_textureData == null || TextureExternal)
            return;

        // padding
        while (writer.Position % 0x80 != 0)
            writer.Write((byte)0);

        var texStartPos = writer.Position;
        writer.WriteUndertaleObject(_textureData);

        if (writer.undertaleData.IsVersionAtLeast(2022, 3))
        {
            _textureBlockSize = texStartPos - writer.Position;
            // Write the actual size of the texture block in
            // the place of _textureBlockSize
            var posBackup = writer.Position;
            writer.Position = _textureBlockSizeLocation;
            writer.Write(_textureBlockSize);
            writer.Position = posBackup;
        }
    }

    /// <summary>
    /// Deserializes the in-file texture blob for this texture.
    /// </summary>
    /// <param name="reader">Where to deserialize from.</param>
    public void UnserializeBlob(UndertaleReader reader)
    {
        // If external, don't deserialize blob
        // Has sanity check for data being null as well, although the external flag should be set
        if (_textureData == null || TextureExternal)
            return;

        while (reader.AbsPosition % 0x80 != 0)
            if (reader.ReadByte() != 0)
                throw new IOException("Padding error!");

        reader.ReadUndertaleObject(_textureData);
        TextureLoaded = true;
    }

    /// <summary>
    /// Assigns texture group info to every embedded texture in the supplied data file.
    /// </summary>
    public static void FindAllTextureInfo(UndertaleData data)
    {
        if (data.TextureGroupInfo != null)
        {
            foreach (var info in data.TextureGroupInfo)
            {
                foreach (var tex in info.TexturePages)
                    tex.Resource.TextureInfo = info;
            }
        }
    }

    // 1x1 black pixel in PNG format
    private static TexData _placeholderTexture = new()
    {
        TextureBlob = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 
            0x00, 0x00, 0x00, 0x01, 0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, 0xDE, 0x00, 0x00, 0x00, 0x01, 0x73, 0x52, 0x47, 
            0x42, 0x00, 0xAE, 0xCE, 0x1C, 0xE9, 0x00, 0x00, 0x00, 0x04, 0x67, 0x41, 0x4D, 0x41, 0x00, 0x00, 0xB1, 0x8F, 0x0B, 0xFC, 
            0x61, 0x05, 0x00, 0x00, 0x00, 0x09, 0x70, 0x48, 0x59, 0x73, 0x00, 0x00, 0x0E, 0xC3, 0x00, 0x00, 0x0E, 0xC3, 0x01, 0xC7, 
            0x6F, 0xA8, 0x64, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54, 0x18, 0x57, 0x63, 0x60, 0x60, 0x60, 0x00, 0x00, 0x00, 
            0x04, 0x00, 0x01, 0x5C, 0xCD, 0xFF, 0x69, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        }
    };
    private static object _textureLoadLock = new();

    /// <summary>
    /// Attempts to load the corresponding external texture. Should only happen in 2022.9 and above.
    /// </summary>
    /// <returns></returns>
    public TexData LoadExternalTexture()
    {
        lock (_textureLoadLock)
        {
            if (TextureLoaded)
                return _textureData;

            TexData texData;

            if (_2022_9_GameDirectory == null)
                return _placeholderTexture;

            // Try to find file on disk
            string path = Path.Combine(_2022_9_GameDirectory, TextureInfo.Directory.Content,
                                       TextureInfo.Name.Content + "_" + IndexInGroup.ToString() + TextureInfo.Extension.Content);
            if (!File.Exists(path))
                return _placeholderTexture;

            // Load file!
            try
            {
                using FileStream fs = new(path, FileMode.Open);
                using FileBinaryReader fbr = new(fs);
                texData = new TexData();
                texData.Unserialize(fbr, true);
                TextureLoaded = true;
            }
            catch (IOException)
            {
                return _placeholderTexture;
            }

            return texData;
        }
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

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _textureData?.Dispose();
        _textureData = null;
        Name = null;
        TextureInfo = null;
    }

    /// <summary>
    /// Texture data in an <see cref="UndertaleEmbeddedTexture"/>.
    /// </summary>
    public class TexData : UndertaleObject, INotifyPropertyChanged, IDisposable
    {
        private byte[] _textureBlob;
        private static MemoryStream sharedStream;

        /// <summary>
        /// The image data of the texture.
        /// </summary>
        public byte[] TextureBlob
        {
            get => _textureBlob;
            set
            {
                _textureBlob = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The width of the texture.
        /// In case of an invalid texture data, this will be <c>-1</c>.
        /// </summary>
        public int Width
        {
            get
            {
                if (_textureBlob is null || _textureBlob.Length < 24)
                    return -1;

                ReadOnlySpan<byte> span = _textureBlob.AsSpan();
                return BinaryPrimitives.ReadInt32BigEndian(span[16..20]);
            }
        }
        /// <summary>
        /// The height of the texture.
        /// In case of an invalid texture data, this will be <c>-1</c>.
        /// </summary>
        public int Height
        {
            get
            {
                if (_textureBlob is null || _textureBlob.Length < 24)
                    return -1;

                ReadOnlySpan<byte> span = _textureBlob.AsSpan();
                return BinaryPrimitives.ReadInt32BigEndian(span[20..24]);
            }
        }

        /// <summary>
        /// Whether this texture uses QOI format.
        /// </summary>
        public bool FormatQOI { get; set; } = false;

        /// <summary>
        /// Whether this texture uses BZ2 format. (Always used in combination with QOI.)
        /// </summary>
        public bool FormatBZ2 { get; set; } = false;

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Header used for PNG files.
        /// </summary>
        public static readonly byte[] PNGHeader = { 137, 80, 78, 71, 13, 10, 26, 10 };

        /// <summary>
        /// Header used for GameMaker QOI + BZ2 files.
        /// </summary>
        public static readonly byte[] QOIAndBZip2Header = { 50, 122, 111, 113 };

        /// <summary>
        /// Header used for GameMaker QOI files.
        /// </summary>
        public static readonly byte[] QOIHeader = { 102, 105, 111, 113 };

        /// <summary>
        /// Frees up <see cref="sharedStream"/> from memory.
        /// </summary>
        public static void ClearSharedStream()
        {
            sharedStream?.Dispose();
            sharedStream = null;
        }

        /// <summary>
        /// Initializes <see cref="sharedStream"/> with a specified initial size.
        /// </summary>
        /// <param name="size">Initial size of <see cref="sharedStream"/> in bytes</param>
        public static void InitSharedStream(int size) => sharedStream = new(size);

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            Serialize(writer, writer.undertaleData.IsVersionAtLeast(2022, 3), writer.undertaleData.IsVersionAtLeast(2022, 5));
        }

        /// <summary>
        /// Serializes the texture to any type of writer (can be any destination file).
        /// </summary>
        public void Serialize(FileBinaryWriter writer, bool gm2022_3, bool gm2022_5)
        {
            if (FormatQOI)
            {
                if (FormatBZ2)
                {
                    writer.Write(QOIAndBZip2Header);

                    // Encode the PNG data back to QOI+BZip2
                    using Bitmap bmp = TextureWorker.GetImageFromByteArray(TextureBlob);
                    writer.Write((short)bmp.Width);
                    writer.Write((short)bmp.Height);
                    byte[] qoiData = QoiConverter.GetArrayFromImage(bmp, gm2022_3 ? 0 : 4);
                    using MemoryStream input = new MemoryStream(qoiData);
                    if (sharedStream.Length != 0)
                        sharedStream.Seek(0, SeekOrigin.Begin);
                    BZip2.Compress(input, sharedStream, false, 9);
                    if (gm2022_5)
                        writer.Write((uint)qoiData.Length);
                    writer.Write(sharedStream.GetBuffer().AsSpan()[..(int)sharedStream.Position]);
                }
                else
                {
                    // Encode the PNG data back to QOI
                    using Bitmap bmp = TextureWorker.GetImageFromByteArray(TextureBlob);
                    writer.Write(QoiConverter.GetSpanFromImage(bmp, gm2022_3 ? 0 : 4));
                }
            }
            else
                writer.Write(TextureBlob);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Unserialize(reader, reader.undertaleData.IsVersionAtLeast(2022, 5));
        }

        /// <summary>
        /// Unserializes the texture from any type of reader (can be from any source).
        /// </summary>
        public void Unserialize(IBinaryReader reader, bool gm2022_5)
        {
            sharedStream ??= new();

            long startAddress = reader.Position;

            byte[] header = reader.ReadBytes(8);
            if (!header.SequenceEqual(PNGHeader))
            {
                reader.Position = startAddress;

                if (header.Take(4).SequenceEqual(QOIAndBZip2Header))
                {
                    FormatQOI = true;
                    FormatBZ2 = true;

                    // Don't really care about the width/height, so skip them, as well as header
                    reader.Position += (uint)(gm2022_5 ? 12 : 8);

                    // Need to fully decompress and convert the QOI data to PNG for compatibility purposes (at least for now)
                    if (sharedStream.Length != 0)
                        sharedStream.Seek(0, SeekOrigin.Begin);
                    BZip2.Decompress(reader.Stream, sharedStream, false);
                    ReadOnlySpan<byte> decompressed = sharedStream.GetBuffer().AsSpan()[..(int)sharedStream.Position];
                    using Bitmap bmp = QoiConverter.GetImageFromSpan(decompressed);
                    sharedStream.Seek(0, SeekOrigin.Begin);
                    bmp.Save(sharedStream, ImageFormat.Png);
                    TextureBlob = new byte[(int)sharedStream.Position];
                    sharedStream.Seek(0, SeekOrigin.Begin);
                    sharedStream.Read(TextureBlob, 0, TextureBlob.Length);
                    return;
                }
                else if (header.Take(4).SequenceEqual(QOIHeader))
                {
                    FormatQOI = true;
                    FormatBZ2 = false;

                    // Need to convert the QOI data to PNG for compatibility purposes (at least for now)
                    using Bitmap bmp = QoiConverter.GetImageFromStream(reader.Stream);
                    if (sharedStream.Length != 0)
                        sharedStream.Seek(0, SeekOrigin.Begin);
                    bmp.Save(sharedStream, ImageFormat.Png);
                    TextureBlob = new byte[(int)sharedStream.Position];
                    sharedStream.Seek(0, SeekOrigin.Begin);
                    sharedStream.Read(TextureBlob, 0, TextureBlob.Length);
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
                uint type = reader.ReadUInt32();
                reader.Position += len + 4;
                if (type == 0x444e4549) // 0x444e4549 -> "IEND"
                    break;
            }

            long length = reader.Position - startAddress;
            reader.Position = startAddress;
            TextureBlob = reader.ReadBytes((int)length);
        }


        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _textureBlob = null;
            ClearSharedStream();
        }
    }
}