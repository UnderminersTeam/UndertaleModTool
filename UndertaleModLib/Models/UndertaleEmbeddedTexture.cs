using ICSharpCode.SharpZipLib.BZip2;
using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UndertaleModLib.Util;

namespace UndertaleModLib.Models;

/// <summary>
/// An embedded texture entry in the data file.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleEmbeddedTexture : UndertaleNamedResource, IDisposable
{
    /// <summary>
    /// The name of the embedded texture entry.
    /// </summary>
    /// <remarks>
    /// This is UTMT specific. The data file does not contain names for embedded textures.
    /// </remarks>
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
    private TexData _textureData = new();

    /// <summary>
    /// Helper variable for whether or not this texture is to be stored externally or not.
    /// </summary>
    public bool TextureExternal { get; set; }


    /// <summary>
    /// Helper variable for whether or not a texture was loaded yet.
    /// </summary>
    public bool TextureLoaded { get; set; }

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
        if (writer.undertaleData.IsVersionAtLeast(2, 0, 6))
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
            writer.Write(0); // Ensure null pointer is written with external texture
        else
            writer.WriteUndertaleObjectPointer(_textureData);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Scaled = reader.ReadUInt32();
        if (reader.undertaleData.IsVersionAtLeast(2, 0, 6))
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
            _textureBlockSize = writer.Position - texStartPos;
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

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        uint count = 0;

        reader.Position += 4; // "Scaled"
        if (reader.undertaleData.IsVersionAtLeast(2, 0, 6))
            reader.Position += 4; // "GeneratedMips"
        if (reader.undertaleData.IsVersionAtLeast(2022, 3))
            reader.Position += 4; // "_textureBlockSize"
        if (reader.undertaleData.IsVersionAtLeast(2022, 9))
            reader.Position += 12; // "TextureWidth", "TextureHeight", "IndexInGroup"

        if (reader.ReadUInt32() != 0)
        {
            // If the texture data pointer isn't null, then this is an internal texture,
            // which will create another object in the pool when reading the blob.
            count++;
        }

        return count;
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

    // 1x1 blank image
    private static readonly TexData _placeholderTexture = new() { Image = new GMImage(1, 1) };
    private static readonly object _textureLoadLock = new();

    /// <summary>
    /// Attempts to load the corresponding external texture. Should only happen in 2022.9 and above.
    /// </summary>
    /// <returns>The texture data of the external texture or a placeholder texture if it can't be loaded.</returns>
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
                                       TextureInfo.Name.Content + "_" + IndexInGroup + TextureInfo.Extension.Content);
            if (!File.Exists(path))
                return _placeholderTexture;

            // Load file!
            try
            {
                using FileStream fs = new(path, FileMode.Open);
                using FileBinaryReader fbr = new(fs);
                texData = new TexData();
                texData.Unserialize(fbr, fs.Length, true);
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
        private GMImage _image;

        /// <summary>
        /// The underlying image of the texture.
        /// </summary>
        public GMImage Image 
        { 
            get => _image;
            set
            {
                _image = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The width of the texture.
        /// </summary>
        public int Width => _image.Width;

        /// <summary>
        /// The height of the texture.
        /// </summary>
        public int Height => _image.Height;

        /// <summary>
        /// Whether this texture uses the QOI format.
        /// </summary>
        public bool FormatQOI => _image.Format is GMImage.ImageFormat.Qoi or GMImage.ImageFormat.Bz2Qoi;

        /// <summary>
        /// Whether this texture uses the BZ2 format. (Always used in combination with QOI.)
        /// </summary>
        public bool FormatBZ2 => _image.Format is GMImage.ImageFormat.Bz2Qoi;

        /// <summary>
        /// If located within a data file, this is the upper bound on the end position of the image data (or start of the next texture blob).
        /// </summary>
        /// <remarks>
        /// All data between the actual end position and this maximum end position should be 0x00 byte padding.
        /// </remarks>
        private int _maxEndOfStreamPosition { get; set; } = -1;

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        
        /// <summary>
        /// Invoked whenever the effective value of any dependency property has been updated.
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            Serialize(writer, writer.undertaleData.IsVersionAtLeast(2022, 5));
        }

        /// <summary>
        /// Serializes the texture to any type of writer (can be any destination file).
        /// </summary>
        public void Serialize(FileBinaryWriter writer, bool gm2022_5)
        {
            if (Image.Format == GMImage.ImageFormat.RawBgra)
            {
                throw new Exception("Unexpected raw RGBA image");
            }

            Image.WriteToBinaryWriter(writer, gm2022_5);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Unserialize(reader, _maxEndOfStreamPosition, reader.undertaleData.IsVersionAtLeast(2022, 5));
        }

        /// <summary>
        /// Unserializes the texture from any type of reader (can be from any source).
        /// </summary>
        /// <param name="reader"><see cref="IBinaryReader"/> to read the texture's image from.</param>
        /// <param name="maxEndOfStreamPosition">Upper bound on the end of the texture's image data (e.g., for padding).</param>
        /// <param name="gm2022_5">Whether to unserialize the image data using GameMaker 2022.5+ format.</param>
        public void Unserialize(IBinaryReader reader, long maxEndOfStreamPosition, bool gm2022_5)
        {
            if (maxEndOfStreamPosition == -1)
            {
                throw new Exception("Expected max end of stream position to be set before unserializing");
            }

            Image = GMImage.FromBinaryReader(reader, maxEndOfStreamPosition, gm2022_5);
        }

        /// <summary>
        /// Sets the upper bound on the position of the end of the image stream, for use when loading a full data file.
        /// </summary>
        /// <remarks>
        /// All data between the actual end position and this maximum end position should be padding (zero bytes).
        /// </remarks>
        public void SetMaxEndOfStreamPosition(int position)
        {
            _maxEndOfStreamPosition = position;
        }


        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _image = null;
        }
    }
}