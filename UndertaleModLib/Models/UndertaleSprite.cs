using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleSprite : UndertaleNamedResource, INotifyPropertyChanged
    {
        private UndertaleString _Name;
        private uint _Width;
        private uint _Height;
        private uint _MarginLeft;
        private uint _MarginRight;
        private uint _MarginBottom;
        private uint _MarginTop;
        private uint _Unknown1;
        private uint _Unknown2;
        private uint _Unknown3;
        private uint _BBoxMode;
        private uint _SepMasks;
        private uint _OriginX;
        private uint _OriginY;
        private int _GMS2Unknown1 = -1;
        private uint _GMS2Unknown2 = 1;
        private uint _GMS2Unknown3 = 0;
        private float _GMS2Unknown4 = 15.0f; // maybe animation speed?
        private uint _GMS2Unknown5 = 0;

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public uint Width { get => _Width; set { _Width = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Width")); } }
        public uint Height { get => _Height; set { _Height = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Height")); } }
        public uint MarginLeft { get => _MarginLeft; set { _MarginLeft = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MarginLeft")); } }
        public uint MarginRight { get => _MarginRight; set { _MarginRight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MarginRight")); } }
        public uint MarginBottom { get => _MarginBottom; set { _MarginBottom = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MarginBottom")); } }
        public uint MarginTop { get => _MarginTop; set { _MarginTop = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MarginTop")); } }
        public uint Unknown1 { get => _Unknown1; set { _Unknown1 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown1")); } }
        public uint Unknown2 { get => _Unknown2; set { _Unknown2 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown2")); } }
        public uint Unknown3 { get => _Unknown3; set { _Unknown3 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown3")); } }
        public uint BBoxMode { get => _BBoxMode; set { _BBoxMode = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BBoxMode")); } }
        public uint SepMasks { get => _SepMasks; set { _SepMasks = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SepMasks")); } }
        public uint OriginX { get => _OriginX; set { _OriginX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OriginX")); } }
        public uint OriginY { get => _OriginY; set { _OriginY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OriginY")); } }
        public UndertaleSimpleList<TextureEntry> Textures { get; private set; } = new UndertaleSimpleList<TextureEntry>();
        public ObservableCollection<MaskEntry> CollisionMasks { get; } = new ObservableCollection<MaskEntry>();
        
        public int GMS2Unknown1 { get => _GMS2Unknown1; set { _GMS2Unknown1 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2Unknown1")); } }
        public uint GMS2Unknown2 { get => _GMS2Unknown2; set { _GMS2Unknown2 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2Unknown2")); } }
        public uint GMS2Unknown3 { get => _GMS2Unknown3; set { _GMS2Unknown3 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2Unknown3")); } }
        public float GMS2Unknown4 { get => _GMS2Unknown4; set { _GMS2Unknown4 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2Unknown4")); } }
        public uint GMS2Unknown5 { get => _GMS2Unknown5; set { _GMS2Unknown5 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2Unknown5")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }

        public class TextureEntry : UndertaleObject, INotifyPropertyChanged
        {
            private UndertaleTexturePageItem _Texture;
            public UndertaleTexturePageItem Texture { get => _Texture; set { _Texture = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Texture")); } }

            public event PropertyChangedEventHandler PropertyChanged;

            public void Serialize(UndertaleWriter writer)
            {
                writer.WriteUndertaleObjectPointer(Texture);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Texture = reader.ReadUndertaleObjectPointer<UndertaleTexturePageItem>();
            }
        }

        public class MaskEntry : INotifyPropertyChanged
        {
            private byte[] _Data;
            public byte[] Data { get => _Data; set { _Data = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Data")); } }

            public event PropertyChangedEventHandler PropertyChanged;

            public MaskEntry()
            {
            }

            public MaskEntry(byte[] data)
            {
                this.Data = data;
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
            if (writer.undertaleData.GeneralInfo.Major >= 2)
            {
                writer.Write(GMS2Unknown1);
                writer.Write(GMS2Unknown2);
                writer.Write(GMS2Unknown3);
                writer.Write(GMS2Unknown4);
                writer.Write(GMS2Unknown5);
            }
            writer.WriteUndertaleObject(Textures);
            writer.Write((uint)CollisionMasks.Count);
            uint total = 0;
            foreach(var mask in CollisionMasks)
            {
                writer.Write(mask.Data);
                total += (uint)mask.Data.Length;
            }
            while(total % 4 != 0)
            {
                writer.Write((byte)0);
                total++;
            }
            Debug.Assert(total == CalculateMaskDataSize(Width, Height, (uint)CollisionMasks.Count));
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
            if (reader.undertaleData.GeneralInfo.Major >= 2)
            {
                GMS2Unknown1 = reader.ReadInt32();
                GMS2Unknown2 = reader.ReadUInt32();
                GMS2Unknown3 = reader.ReadUInt32();
                GMS2Unknown4 = reader.ReadSingle();
                GMS2Unknown5 = reader.ReadUInt32();
            }
            Textures = reader.ReadUndertaleObject<UndertaleSimpleList<TextureEntry>>();
            uint MaskCount = reader.ReadUInt32();
            CollisionMasks.Clear();
            uint total = 0;
            for(uint i = 0; i < MaskCount; i++)
            {
                uint len = (Width + 7) / 8 * Height;
                CollisionMasks.Add(new MaskEntry(reader.ReadBytes((int)len)));
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
