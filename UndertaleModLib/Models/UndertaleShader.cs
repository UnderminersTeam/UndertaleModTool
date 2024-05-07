using System;

namespace UndertaleModLib.Models;

/// <summary>
/// A shader entry for a data file.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleShader : UndertaleNamedResource, IDisposable
{
    /// <summary>
    /// The vertex shader attributes a shader can have.
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class VertexShaderAttribute : UndertaleObject, IStaticChildObjectsSize, IDisposable
    {
        /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
        public static readonly uint ChildObjectsSize = 4;

        /// <summary>
        /// The name of the vertex shader attribute.
        /// </summary>
        public UndertaleString Name { get; set; }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Name = null;
        }
    }

    public uint EntryEnd;

    /// <summary>
    /// The name of the shader.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// The type the shader uses.
    /// </summary>
    public ShaderType Type { get; set; }

    //TODO: these all should get renamed properly akin to naming conventions.
    /// <summary>
    /// The GLSL ES vertex code this shader uses.
    /// </summary>
    public UndertaleString GLSL_ES_Vertex { get; set; }

    /// <summary>
    /// The GLSL ES fragment code this shader uses.
    /// </summary>
    public UndertaleString GLSL_ES_Fragment { get; set; }

    /// <summary>
    /// The GLSL vertex code this shader uses.
    /// </summary>
    public UndertaleString GLSL_Vertex { get; set; }

    /// <summary>
    /// The GLSL fragment code this shader uses.
    /// </summary>
    public UndertaleString GLSL_Fragment { get; set; }

    /// <summary>
    /// The HLSL9 vertex code this shader uses.
    /// </summary>
    public UndertaleString HLSL9_Vertex { get; set; }

    /// <summary>
    /// The HLSL9 fragment code this shader uses.
    /// </summary>
    public UndertaleString HLSL9_Fragment { get; set; }

    /// <summary>
    /// The version of this shader entry.
    /// </summary>
    public int Version { get; set; } = 2;

    /// <summary>
    /// The HLSL11 Vertex data of this shader.
    /// </summary>
    public UndertaleRawShaderData HLSL11_VertexData;

    /// <summary>
    /// The HLSL11 Pixel data of this shader.
    /// </summary>
    public UndertaleRawShaderData HLSL11_PixelData;

    /// <summary>
    /// The PSSL Vertex data of this shader.
    /// </summary>
    public UndertaleRawShaderData PSSL_VertexData;

    /// <summary>
    /// The PSSL Pixel data of this shader.
    /// </summary>
    public UndertaleRawShaderData PSSL_PixelData;

    /// <summary>
    /// The Cg_PSVita Vertex data of this shader.
    /// </summary>
    public UndertaleRawShaderData Cg_PSVita_VertexData;

    /// <summary>
    /// The Cg_PSVita Pixel data of this shader.
    /// </summary>
    public UndertaleRawShaderData Cg_PSVita_PixelData;

    /// <summary>
    /// The Cg_PS3 Vertex data of this shader.
    /// </summary>
    public UndertaleRawShaderData Cg_PS3_VertexData;

    /// <summary>
    /// The Cg_PS3 Pixel data of this shader.
    /// </summary>
    public UndertaleRawShaderData Cg_PS3_PixelData;

    /// <summary>
    /// A list of Vertex Shader attributes this shader uses.
    /// </summary>
    public UndertaleSimpleList<VertexShaderAttribute> VertexShaderAttributes { get; set; } = new UndertaleSimpleList<VertexShaderAttribute>();

    /// <summary>
    /// Initializes a new instance of <see cref="UndertaleShader"/>.
    /// </summary>
    public UndertaleShader()
    {
        HLSL11_VertexData = new UndertaleRawShaderData();
        HLSL11_PixelData = new UndertaleRawShaderData();
        PSSL_VertexData = new UndertaleRawShaderData();
        PSSL_PixelData = new UndertaleRawShaderData();
        Cg_PSVita_VertexData = new UndertaleRawShaderData();
        Cg_PSVita_PixelData = new UndertaleRawShaderData();
        Cg_PS3_VertexData = new UndertaleRawShaderData();
        Cg_PS3_PixelData = new UndertaleRawShaderData();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content + " (" + GetType().Name + ")";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        HLSL11_VertexData?.Dispose();
        HLSL11_PixelData?.Dispose();
        PSSL_VertexData?.Dispose();
        PSSL_PixelData?.Dispose();
        Cg_PSVita_VertexData?.Dispose();
        Cg_PSVita_PixelData?.Dispose();
        Cg_PS3_VertexData?.Dispose();
        Cg_PS3_PixelData?.Dispose();
        foreach (var attr in VertexShaderAttributes)
            attr?.Dispose();

        Name = null;
        GLSL_ES_Vertex = null;
        GLSL_ES_Fragment = null;
        GLSL_Vertex = null;
        GLSL_Fragment = null;
        HLSL9_Vertex = null;
        HLSL9_Fragment = null;
        HLSL11_VertexData = null;
        HLSL11_PixelData = null;
        PSSL_VertexData = null;
        PSSL_PixelData = null;
        Cg_PSVita_VertexData = null;
        Cg_PSVita_PixelData = null;
        Cg_PS3_VertexData = null;
        Cg_PS3_PixelData = null;
        VertexShaderAttributes = new();
    }

    private static void WritePadding(UndertaleWriter writer, int amount)
    {
        while ((writer.Position & amount) != 0)
        {
            writer.Write((byte)0);
        }
    }

    private static void ReadPadding(UndertaleReader reader, int amount)
    {
        while ((reader.AbsPosition & amount) != 0)
        {
            if (reader.ReadByte() != 0)
                throw new UndertaleSerializationException("Failed to read shader padding: should be some zero bytes");
        }
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.Write((uint)Type | 0x80000000u); // in big-endian?

        writer.WriteUndertaleString(GLSL_ES_Vertex);
        writer.WriteUndertaleString(GLSL_ES_Fragment);
        writer.WriteUndertaleString(GLSL_Vertex);
        writer.WriteUndertaleString(GLSL_Fragment);
        writer.WriteUndertaleString(HLSL9_Vertex);
        writer.WriteUndertaleString(HLSL9_Fragment);

        HLSL11_VertexData.Serialize(writer, false);
        HLSL11_PixelData.Serialize(writer, false);

        writer.WriteUndertaleObject(VertexShaderAttributes);

        writer.Write(Version);

        PSSL_VertexData.Serialize(writer);
        PSSL_PixelData.Serialize(writer);
        Cg_PSVita_VertexData.Serialize(writer);
        Cg_PSVita_PixelData.Serialize(writer);

        if (Version >= 2)
        {
            Cg_PS3_VertexData.Serialize(writer);
            Cg_PS3_PixelData.Serialize(writer);
        }

        if (!HLSL11_VertexData.IsNull)
        {
            WritePadding(writer, 7);

            HLSL11_VertexData.WriteData(writer);
        }
        if (!HLSL11_PixelData.IsNull)
        {
            WritePadding(writer, 7);

            HLSL11_PixelData.WriteData(writer);
        }

        if (!PSSL_VertexData.IsNull)
        {
            WritePadding(writer, 7);

            PSSL_VertexData.WriteData(writer);
        }
        if (!PSSL_PixelData.IsNull)
        {
            WritePadding(writer, 7);

            PSSL_PixelData.WriteData(writer);
        }

        if (!Cg_PSVita_VertexData.IsNull)
        {
            WritePadding(writer, 7);

            Cg_PSVita_VertexData.WriteData(writer);
        }
        if (!Cg_PSVita_PixelData.IsNull)
        {
            WritePadding(writer, 7);

            Cg_PSVita_PixelData.WriteData(writer);
        }

        if (Version >= 2)
        {
            if (!Cg_PS3_VertexData.IsNull)
            {
                WritePadding(writer, 15);

                Cg_PS3_VertexData.WriteData(writer);
            }
            if (!Cg_PS3_PixelData.IsNull)
            {
                WritePadding(writer, 15);

                Cg_PS3_PixelData.WriteData(writer);
            }
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        Type = (ShaderType)(reader.ReadUInt32() & 0x7FFFFFFFu); // in big endian?

        GLSL_ES_Vertex = reader.ReadUndertaleString();
        GLSL_ES_Fragment = reader.ReadUndertaleString();
        GLSL_Vertex = reader.ReadUndertaleString();
        GLSL_Fragment = reader.ReadUndertaleString();
        HLSL9_Vertex = reader.ReadUndertaleString();
        HLSL9_Fragment = reader.ReadUndertaleString();

        HLSL11_VertexData.Unserialize(reader, false);
        HLSL11_PixelData.Unserialize(reader, false);

        VertexShaderAttributes = reader.ReadUndertaleObject<UndertaleSimpleList<VertexShaderAttribute>>();

        Version = reader.ReadInt32();

        PSSL_VertexData.Unserialize(reader);
        PSSL_PixelData.Unserialize(reader);
        Cg_PSVita_VertexData.Unserialize(reader);
        Cg_PSVita_PixelData.Unserialize(reader);
        if (Version >= 2)
        {
            Cg_PS3_VertexData.Unserialize(reader);
            Cg_PS3_PixelData.Unserialize(reader);
        }

        if (!HLSL11_VertexData.IsNull)
        {
            ReadPadding(reader, 7);

            // Calculate length of data
            uint next;
            if (!HLSL11_PixelData.IsNull)
                next = HLSL11_PixelData._Position;
            else
                next = EntryEnd;
            int length = (int)(next - reader.AbsPosition);
            HLSL11_VertexData.ReadData(reader, length, HLSL11_VertexData.IsNull);
        }
        if (!HLSL11_PixelData.IsNull)
        {
            ReadPadding(reader, 7);

            // Calculate length of data
            uint next;
            if (!PSSL_VertexData.IsNull)
                next = PSSL_VertexData._Position;
            else
                next = EntryEnd;
            int length = (int)(next - reader.AbsPosition);
            HLSL11_PixelData.ReadData(reader, length, HLSL11_PixelData.IsNull);
        }

        if (!PSSL_VertexData.IsNull)
        {
            ReadPadding(reader, 7);

            // Calculate length of data
            uint next;
            if (!PSSL_PixelData.IsNull)
                next = PSSL_PixelData._Position;
            else
                next = EntryEnd;
            int length = (int)(next - reader.AbsPosition);
            PSSL_VertexData.ReadData(reader, length, PSSL_PixelData.IsNull);
        }
        if (!PSSL_PixelData.IsNull)
        {
            ReadPadding(reader, 7);

            // Calculate length of data
            uint next;
            if (!Cg_PSVita_VertexData.IsNull)
                next = Cg_PSVita_VertexData._Position;
            else
                next = EntryEnd;
            int length = (int)(next - reader.AbsPosition);
            PSSL_PixelData.ReadData(reader, length, Cg_PSVita_VertexData.IsNull);
        }

        if (!Cg_PSVita_VertexData.IsNull)
        {
            ReadPadding(reader, 7);

            // Calculate length of data
            uint next;
            if (!Cg_PSVita_PixelData.IsNull)
                next = Cg_PSVita_PixelData._Position;
            else
                next = EntryEnd;
            int length = (int)(next - reader.AbsPosition);
            Cg_PSVita_VertexData.ReadData(reader, length, Cg_PSVita_PixelData.IsNull);
        }
        if (!Cg_PSVita_PixelData.IsNull)
        {
            ReadPadding(reader, 7);

            // Calculate length of data
            uint next;
            if (!Cg_PS3_VertexData.IsNull)
                next = Cg_PS3_VertexData._Position;
            else
                next = EntryEnd;
            int length = (int)(next - reader.AbsPosition);
            Cg_PSVita_PixelData.ReadData(reader, length, Cg_PS3_VertexData.IsNull);
        }

        if (Version >= 2)
        {
            if (!Cg_PS3_VertexData.IsNull)
            {
                ReadPadding(reader, 15);

                // Calculate length of data
                uint next;
                if (!Cg_PS3_PixelData.IsNull)
                    next = Cg_PS3_PixelData._Position;
                else
                    next = EntryEnd;
                int length = (int)(next - reader.AbsPosition);
                Cg_PS3_VertexData.ReadData(reader, length, Cg_PS3_PixelData.IsNull);
            }
            if (!Cg_PS3_PixelData.IsNull)
            {
                ReadPadding(reader, 15);

                // Calculate length of data
                uint next = EntryEnd; // final possible data, nothing else to check for
                int length = (int)(next - reader.AbsPosition);
                Cg_PS3_PixelData.ReadData(reader, length, true);
            }
        }
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position += 40;

        // Since shaders are stored in a pointer list, and there are no
        // more child objects that are in the pool, then there is no
        // need to unserializing remaining elements
        return 1 + UndertaleSimpleList<VertexShaderAttribute>.UnserializeChildObjectCount(reader);
    }

    /// <summary>
    /// Possible shader types a shader can have.
    /// </summary>
    /// <remarks>All console shaders (and HLSL11?) are compiled using confidential SDK tools when
    /// GMAssetCompiler builds the game (for PSVita it's psp2cgc shader compiler).</remarks>
    public enum ShaderType : uint
    {
        /// <summary>
        /// Shader uses GLSL_ES
        /// </summary>
        GLSL_ES = 1,
        /// <summary>
        /// Shader uses GLSL
        /// </summary>
        GLSL = 2,
        /// <summary>
        /// Shader uses HLSL9
        /// </summary>
        HLSL9 = 3,
        /// <summary>
        /// Shader uses HLSL11
        /// </summary>
        HLSL11 = 4,
        /// <summary>
        /// Shader uses PSSL
        /// </summary>
        /// <remarks>PSSL is a shading language used only in PS4, based on HLSL11.</remarks>
        PSSL = 5,
        /// <summary>
        /// Shader uses for the PSVita
        /// </summary>
        /// <remarks>Cg stands for "C for graphics" made by NVIDIA and used in PSVita and PS3 (they have their own variants of Cg), based on HLSL9.</remarks>
        Cg_PSVita = 6,
        /// <summary>
        /// Shader uses Cg for the PS3
        /// </summary>
        /// <remarks>Cg stands for "C for graphics" made by NVIDIA and used in PSVita and PS3 (they have their own variants of Cg), based on HLSL9.</remarks>
        Cg_PS3 = 7
    }

    public class UndertaleRawShaderData : IDisposable
    {
        internal uint _Position;
        internal uint _PointerLocation;
        internal uint _Length; // note: this is not always an accurate value, use Data.Length if necessary
        public byte[] Data;
        public bool IsNull = true;
        
        public void Serialize(UndertaleWriter writer, bool writeLength = true)
        {
            _PointerLocation = writer.Position;

            // This value is *very important*, as this is the default and is commonly used
            writer.Write(0x00000000);

            if (writeLength)
                writer.Write((Data == null) ? 0 : Data.Length);
        }

        public void Unserialize(UndertaleReader reader, bool readLength = true)
        {
            _PointerLocation = (uint)reader.AbsPosition;
            _Position = reader.ReadUInt32();
            if (readLength)
                _Length = reader.ReadUInt32();

            IsNull = (_Position == 0x00000000u);
        }

        public void WriteData(UndertaleWriter writer)
        {
            // Technically should not happen
            if (IsNull)
                throw new UndertaleSerializationException("Cannot write null shader data");

            // Update the pointer (manually)
            _Position = writer.Position;
            writer.Position = _PointerLocation;
            writer.Write(_Position);
            writer.Position = _Position;

            writer.Write(Data);
        }

        public void ReadData(UndertaleReader reader, int length, bool isLast = false)
        {
            if (_Length != 0) // _Length is the "expected" length here
            {
                if (_Length > length)
                    throw new UndertaleSerializationException("Failed to compute length of shader data: instructed to read less data than expected.");
                else if (_Length < length)
                {
                    if (isLast && ((reader.AbsPosition + length) % 16 == 0)) // Normal for the last element due to chunk padding, just trust the system
                    {
                        length = (int)_Length;
                    }
                    else
                    {
                        throw new UndertaleSerializationException("Failed to compute length of shader data: instructed to read more data than expected." +
                            (isLast ? " Shader data was the last in the shader, but given length was incorrectly padded." : ""));
                    }
                }
            }

            _Length = (uint)length;
            Data = reader.ReadBytes(length);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Data = null;
        }
    }
}