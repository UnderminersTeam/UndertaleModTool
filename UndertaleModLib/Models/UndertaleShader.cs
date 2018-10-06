using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    // TODO: INotifyPropertyChanged and other cleaning
    public class UndertaleShader : UndertaleObject
    {
        public uint _EntryEnd;

        public UndertaleString Name;
        public ShaderType Type;
        
        public UndertaleString GLSL_ES_Vertex;
        public UndertaleString GLSL_ES_Fragment;
        public UndertaleString GLSL_Vertex;
        public UndertaleString GLSL_Fragment;
        public UndertaleString HLSL9_Vertex;
        public UndertaleString HLSL9_Fragment;

        public UndertaleRawShaderData HLSL11_VertexData;
        public UndertaleRawShaderData HLSL11_PixelData;
        public UndertaleRawShaderData PSSL_VertexData;
        public UndertaleRawShaderData PSSL_PixelData;
        public UndertaleRawShaderData Cg_VertexData;
        public UndertaleRawShaderData Cg_PixelData;
        public UndertaleRawShaderData Cg_PS3_VertexData;
        public UndertaleRawShaderData Cg_PS3_PixelData;

        // Unfortunately SimpleList does not support string serialization... (need to do it manually)
        public List<UndertaleString> VertexShaderAttributes;

        public UndertaleShader()
        {
            HLSL11_VertexData = new UndertaleRawShaderData();
            HLSL11_PixelData = new UndertaleRawShaderData();
            PSSL_VertexData = new UndertaleRawShaderData();
            PSSL_PixelData = new UndertaleRawShaderData();
            Cg_VertexData = new UndertaleRawShaderData();
            Cg_PixelData = new UndertaleRawShaderData();
            Cg_PS3_VertexData = new UndertaleRawShaderData();
            Cg_PS3_PixelData = new UndertaleRawShaderData();
        }

        private void WritePadding(UndertaleWriter writer, int amount)
        {
            while ((writer.BaseStream.Position & amount) != 0)
            {
                writer.Write((byte)0);
            }
        }

        private void ReadPadding(UndertaleReader reader, int amount)
        {
            while ((reader.BaseStream.Position & amount) != 0)
            {
                if (reader.ReadByte() != 0)
                    throw new UndertaleSerializationException("Failed to read shader padding: should be some zero bytes");
            }
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write((uint)Type);

            writer.WriteUndertaleString(GLSL_ES_Vertex);
            writer.WriteUndertaleString(GLSL_ES_Fragment);
            writer.WriteUndertaleString(GLSL_Vertex);
            writer.WriteUndertaleString(GLSL_Fragment);
            writer.WriteUndertaleString(HLSL9_Vertex);
            writer.WriteUndertaleString(HLSL9_Fragment);

            HLSL11_VertexData.Serialize(writer, false);
            HLSL11_PixelData.Serialize(writer, false);

            // Need to serialize the array manually: UndertaleString has special serialization needs
            writer.Write(VertexShaderAttributes.Count);
            for (int i = 0; i < VertexShaderAttributes.Count; i++)
            {
                writer.WriteUndertaleString(VertexShaderAttributes[i]);
            }

            writer.Write((int)2);

            PSSL_VertexData.Serialize(writer);
            PSSL_PixelData.Serialize(writer);
            Cg_VertexData.Serialize(writer);
            Cg_PixelData.Serialize(writer);
            Cg_PS3_VertexData.Serialize(writer);
            Cg_PS3_PixelData.Serialize(writer);

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

            if (!Cg_VertexData.IsNull)
            {
                WritePadding(writer, 7);

                Cg_VertexData.WriteData(writer);
            }
            if (!Cg_PixelData.IsNull)
            {
                WritePadding(writer, 7);

                Cg_PixelData.WriteData(writer);
            }

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

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Type = (ShaderType)reader.ReadUInt32();

            GLSL_ES_Vertex = reader.ReadUndertaleString();
            GLSL_ES_Fragment = reader.ReadUndertaleString();
            GLSL_Vertex = reader.ReadUndertaleString();
            GLSL_Fragment = reader.ReadUndertaleString();
            HLSL9_Vertex = reader.ReadUndertaleString();
            HLSL9_Fragment = reader.ReadUndertaleString();

            HLSL11_VertexData.Unserialize(reader, false);
            HLSL11_PixelData.Unserialize(reader, false);

            // Need to parse the Array/SimpleList manually here because UndertaleString
            // has a special serialization
            int count = reader.ReadInt32();
            VertexShaderAttributes = new List<UndertaleString>(count);
            for (int i = 0; i < count; i++)
            {
                VertexShaderAttributes.Add(reader.ReadUndertaleString());
            }

            if (reader.ReadInt32() != 2)
                throw new UndertaleSerializationException("Value in shader should be 2 at all times");

            PSSL_VertexData.Unserialize(reader);
            PSSL_PixelData.Unserialize(reader);
            Cg_VertexData.Unserialize(reader);
            Cg_PixelData.Unserialize(reader);
            Cg_PS3_VertexData.Unserialize(reader);
            Cg_PS3_PixelData.Unserialize(reader);

            if (!HLSL11_VertexData.IsNull)
            {
                ReadPadding(reader, 7);

                // Calculate length of data
                uint next = 0;
                if (!HLSL11_PixelData.IsNull)
                    next = HLSL11_PixelData._Position;
                else
                    next = _EntryEnd;
                int length = (int)(next - reader.Position);
                HLSL11_VertexData.ReadData(reader, length);
            }
            if (!HLSL11_PixelData.IsNull)
            {
                ReadPadding(reader, 7);

                // Calculate length of data
                uint next = 0;
                if (!PSSL_VertexData.IsNull)
                    next = PSSL_VertexData._Position;
                else
                    next = _EntryEnd;
                int length = (int)(next - reader.Position);
                HLSL11_PixelData.ReadData(reader, length);
            }

            if (!PSSL_VertexData.IsNull)
            {
                ReadPadding(reader, 7);

                // Calculate length of data
                uint next = 0;
                if (!PSSL_PixelData.IsNull)
                    next = PSSL_PixelData._Position;
                else
                    next = _EntryEnd;
                int length = (int)(next - reader.Position);
                PSSL_VertexData.ReadData(reader, length);
            }
            if (!PSSL_PixelData.IsNull)
            {
                ReadPadding(reader, 7);

                // Calculate length of data
                uint next = 0;
                if (!Cg_VertexData.IsNull)
                    next = Cg_VertexData._Position;
                else
                    next = _EntryEnd;
                int length = (int)(next - reader.Position);
                PSSL_PixelData.ReadData(reader, length);
            }

            if (!Cg_VertexData.IsNull)
            {
                ReadPadding(reader, 7);

                // Calculate length of data
                uint next = 0;
                if (!Cg_PixelData.IsNull)
                    next = Cg_PixelData._Position;
                else
                    next = _EntryEnd;
                int length = (int)(next - reader.Position);
                Cg_VertexData.ReadData(reader, length);
            }
            if (!Cg_PixelData.IsNull)
            {
                ReadPadding(reader, 7);

                // Calculate length of data
                uint next = 0;
                if (!Cg_PS3_VertexData.IsNull)
                    next = Cg_PS3_VertexData._Position;
                else
                    next = _EntryEnd;
                int length = (int)(next - reader.Position);
                Cg_PixelData.ReadData(reader, length);
            }

            if (!Cg_PS3_VertexData.IsNull)
            {
                ReadPadding(reader, 15);

                // Calculate length of data
                uint next = 0;
                if (!Cg_PS3_PixelData.IsNull)
                    next = Cg_PS3_PixelData._Position;
                else
                    next = _EntryEnd;
                int length = (int)(next - reader.Position);
                Cg_PS3_VertexData.ReadData(reader, length);
            }
            if (!Cg_PS3_PixelData.IsNull)
            {
                ReadPadding(reader, 15);

                // Calculate length of data
                uint next = _EntryEnd; // final possible data, nothing else to check for
                int length = (int)(next - reader.Position);
                Cg_PS3_PixelData.ReadData(reader, length);
            }
        }

        // The "unknown" types are actually known, but what they correspond to is unknown.
        // They are PSSL (PlayStation Shader Language), Cg, and Cg_PS3 (for PS3 seemingly)
        public enum ShaderType : uint
        {
            GLSL_ES = 0x01000080,
            GLSL = 0x02000080,
            Unknown1 = 0x03000080,
            HLSL = 0x04000080,
            Unknown2 = 0x05000080,
            Unknown3 = 0x06000080,
            Unknown4 = 0x07000080
        }

        public class UndertaleRawShaderData
        {
            internal uint _Position;
            internal uint _PointerLocation;
            internal uint _Length; // note: this is not always an accurate value, use Data.Length if necessary
            public byte[] Data;
            public bool IsNull;

            public UndertaleRawShaderData()
            {
                _Position = 0;
                _PointerLocation = 0;
                _Length = 0;
                IsNull = true;
            }

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
                _PointerLocation = reader.Position;
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

            public void ReadData(UndertaleReader reader, int length)
            {
                if (_Length != 0 && _Length != length)
                    throw new UndertaleSerializationException("Failed to compute length of shader data");

                _Length = (uint)length;
                Data = reader.ReadBytes(length);
            }
        }
    }
}
