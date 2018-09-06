using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleSprite : UndertaleObject
    {
        public UndertaleString Name { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public uint MarginLeft { get; set; }
        public uint MarginRight { get; set; }
        public uint MarginBottom { get; set; }
        public uint MarginTop { get; set; }
        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }
        public uint Unknown3 { get; set; }
        public uint BBoxMode { get; set; }
        public uint SepMasks { get; set; }
        public uint OriginX { get; set; }
        public uint OriginY { get; set; }
        public UndertaleSimpleList<TextureEntry> Textures { get; private set; } = new UndertaleSimpleList<TextureEntry>();
        //public uint MaskCount => (uint)MaskData.Length;
        public byte[][] CollisionMaskData { get; set; }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }

        public class TextureEntry : UndertaleObject
        {
            public UndertaleTexturePage TextureId { get; set; }

            public void Serialize(UndertaleWriter writer)
            {
                writer.WriteUndertaleObjectPointer(TextureId);
            }

            public void Unserialize(UndertaleReader reader)
            {
                TextureId = reader.ReadUndertaleObjectPointer<UndertaleTexturePage>();
            }
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(MarginLeft);
            writer.Write(MarginRight);
            writer.Write(MarginBottom);
            writer.Write(MarginTop);
            writer.Write(Unknown1);
            writer.Write(Unknown2);
            writer.Write(Unknown3);
            writer.Write(BBoxMode);
            writer.Write(SepMasks);
            writer.Write(OriginX);
            writer.Write(OriginY);
            writer.WriteUndertaleObject(Textures);
            writer.Write((uint)CollisionMaskData.Length);
            uint total = 0;
            foreach(byte[] mask in CollisionMaskData)
            {
                writer.Write(mask);
                total += (uint)mask.Length;
            }
            while(total % 4 != 0)
            {
                writer.Write((byte)0);
                total++;
            }
            Debug.Assert(total == CalculateMaskDataSize(Width, Height, (uint)CollisionMaskData.Length));
        }
        
        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Width = reader.ReadUInt32();
            Height = reader.ReadUInt32();
            MarginLeft = reader.ReadUInt32();
            MarginRight = reader.ReadUInt32();
            MarginBottom = reader.ReadUInt32();
            MarginTop = reader.ReadUInt32();
            Unknown1 = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            Unknown3 = reader.ReadUInt32();
            BBoxMode = reader.ReadUInt32();
            SepMasks = reader.ReadUInt32();
            OriginX = reader.ReadUInt32();
            OriginY = reader.ReadUInt32();
            Textures = reader.ReadUndertaleObject<UndertaleSimpleList<TextureEntry>>();
            uint MaskCount = reader.ReadUInt32();
            CollisionMaskData = new byte[MaskCount][];
            uint total = 0;
            for(uint i = 0; i < MaskCount; i++)
            {
                uint len = (Width + 7) / 8 * Height;
                CollisionMaskData[i] = reader.ReadBytes((int)len);
                total += len;
            }
            while(total % 4 != 0)
            {
                if (reader.ReadByte() != 0)
                    throw new IOException("Mask padding");
                total++;
            }
            Debug.Assert(total == CalculateMaskDataSize(Width, Height, MaskCount));
        }
        
        /**
         * This is just a stream of bits, with each row aligned to a full byte
         * and the whole array aligned to 4 bytes
         * For some reason this code looks scary even though the concept is really simple :P
         */

        /*private byte[] PackMaskData(bool[][][] unpacked)
        {
            byte[] packed = new byte[CalculateMaskDataSize()];
            uint i = 0;
            byte temp = 0;

            for(uint maskId = 0; maskId < MaskCount; maskId++)
            {
                for(uint y = 0; y < Height; y++)
                {
                    for(uint x = 0; x < (Width+7) / 8 * 8; x++)
                    {
                        temp = (byte)((temp << 1) | (x < Width ? (unpacked[maskId][y][x] ? 1 : 0) : 0));
                        i++;
                        if (i % 8 == 0)
                        {
                            packed[(i/8)-1] = temp;
                            temp = 0;
                        }
                    }
                }
            }
            if (i % 32 != 0)
            {
                while (i % 32 != 0)
                {
                    while (i % 8 != 0 || (i%8 == 0 && i%32 != 0))
                    {
                        temp <<= 1;
                        i++;
                    }
                    packed[(i / 8) - 1] = temp;
                    temp = 0;
                }
            }
            Debug.Assert(i / 8 == packed.Length);
            return packed;
        }

        private bool[][][] UnpackMaskData(byte[] packed)
        {
            bool[][][] unpacked = new bool[MaskCount][][];
            uint i = 0;
            byte temp = packed[0];

            for (uint maskId = 0; maskId < MaskCount; maskId++)
            {
                unpacked[maskId] = new bool[Height][];
                for (uint y = 0; y < Height; y++)
                {
                    unpacked[maskId][y] = new bool[Width];
                    for (uint x = 0; x < (Width + 7) / 8 * 8; x++)
                    {
                        if (x < Width)
                            unpacked[maskId][y][x] = (temp & 0x80) != 0;
                        temp <<= 1;
                        i++;
                        if (i % 8 == 0)
                        {
                            temp = i/8 < packed.Length ? packed[i / 8] : (byte)0;
                        }
                    }
                }
            }
            Debug.Assert(((i + 31) / 32 * 32)/8 == packed.Length);
            return unpacked;
        }*/

        public uint CalculateMaskDataSize(uint width, uint height, uint maskcount)
        {
            uint roundedWidth = (width + 7) / 8 * 8; // round to multiple of 8
            uint dataBits = roundedWidth * height * maskcount;
            uint dataBytes = ((dataBits + 31) / 32 * 32) / 8; // round to multiple of 4 bytes
            return dataBytes;
        }
    }
}
