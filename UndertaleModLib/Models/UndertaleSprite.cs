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
    public class UndertaleSprite : UndertaleNamedResource, PrePaddedObject, INotifyPropertyChanged
    {
        private UndertaleString _Name;
        private uint _Width;
        private uint _Height;
        private uint _MarginLeft;
        private uint _MarginRight;
        private uint _MarginBottom;
        private uint _MarginTop;
        private bool _Transparent;
        private bool _Smooth;
        private bool _Preload;
        private uint _BBoxMode;
        private uint _SepMasks;
        private uint _OriginX;
        private uint _OriginY;
        private uint _GMS2UnknownAlways1 = 1;
        private SpriteType _SSpriteType = 0;
        private float _GMS2PlaybackSpeed = 15.0f;
        private uint _GMS2PlaybackSpeedType = 0;
        private bool _IsSpecialType = false;

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public uint Width { get => _Width; set { _Width = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Width")); } }
        public uint Height { get => _Height; set { _Height = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Height")); } }
        public uint MarginLeft { get => _MarginLeft; set { _MarginLeft = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MarginLeft")); } }
        public uint MarginRight { get => _MarginRight; set { _MarginRight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MarginRight")); } }
        public uint MarginBottom { get => _MarginBottom; set { _MarginBottom = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MarginBottom")); } }
        public uint MarginTop { get => _MarginTop; set { _MarginTop = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MarginTop")); } }
        public bool Transparent { get => _Transparent; set { _Transparent = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Transparent")); } }
        public bool Smooth { get => _Smooth; set { _Smooth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Smooth")); } }
        public bool Preload { get => _Preload; set { _Preload = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Preload")); } }
        public uint BBoxMode { get => _BBoxMode; set { _BBoxMode = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BBoxMode")); } }
        public uint SepMasks { get => _SepMasks; set { _SepMasks = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SepMasks")); } }
        public uint OriginX { get => _OriginX; set { _OriginX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OriginX")); } }
        public uint OriginY { get => _OriginY; set { _OriginY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OriginY")); } }
        public UndertaleSimpleList<TextureEntry> Textures { get; private set; } = new UndertaleSimpleList<TextureEntry>();
        public ObservableCollection<MaskEntry> CollisionMasks { get; } = new ObservableCollection<MaskEntry>();
        
        // Special sprite types (always used in GMS2)
        public uint SUnknownAlways1 { get => _GMS2UnknownAlways1; set { _GMS2UnknownAlways1 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SUnknownAlways1")); } }
        public SpriteType SSpriteType { get => _SSpriteType; set { _SSpriteType = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SSpriteType")); } }
        public float GMS2PlaybackSpeed { get => _GMS2PlaybackSpeed; set { _GMS2PlaybackSpeed = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2PlaybackSpeed")); } }
        public uint GMS2PlaybackSpeedType { get => _GMS2PlaybackSpeedType; set { _GMS2PlaybackSpeedType = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2PlaybackSpeedType")); } }
        public bool IsSpecialType { get => _IsSpecialType; set { _IsSpecialType = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSpecialType")); } }

        public byte[] S_Spine_Data;
        public byte[] S_SWF_Data;

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }

        public enum SpriteType : uint
        {
            Normal = 0,
            SWF = 1,
            Spine = 2
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
            writer.Write(Transparent);
            writer.Write(Smooth);
            writer.Write(Preload);
            writer.Write(BBoxMode);
            writer.Write(SepMasks);
            writer.Write(OriginX);
            writer.Write(OriginY);
            if (IsSpecialType)
            {
                writer.Write(-1);
                writer.Write(SUnknownAlways1);
                writer.Write((uint)SSpriteType);
                if (writer.undertaleData.GeneralInfo?.Major >= 2)
                {
                    writer.Write(GMS2PlaybackSpeed);
                    writer.Write(GMS2PlaybackSpeedType);
                }

                switch (SSpriteType)
                {
                    case SpriteType.Normal:
                        writer.WriteUndertaleObject(Textures);
                        WriteMaskData(writer);
                        break;
                    case SpriteType.SWF:
                        writer.Write(8);
                        writer.WriteUndertaleObject(Textures);
                        Align3(writer);
                        writer.Write(S_SWF_Data);
                        break;
                    case SpriteType.Spine:
                        Align3(writer);
                        writer.Write(S_Spine_Data);
                        break;
                }
            }
            else
            {
                writer.WriteUndertaleObject(Textures);
                WriteMaskData(writer);
            }
        }

        private void WriteMaskData(UndertaleWriter writer)
        {
            writer.Write((uint)CollisionMasks.Count);
            uint total = 0;
            foreach (var mask in CollisionMasks)
            {
                writer.Write(mask.Data);
                total += (uint)mask.Data.Length;
            }
            while (total % 4 != 0)
            {
                writer.Write((byte)0);
                total++;
            }
            Debug.Assert(total == CalculateMaskDataSize(Width, Height, (uint)CollisionMasks.Count));
        }

        private void Align3(UndertaleReader reader)
        {
            while ((reader.Position & 3) != 0)
            {
                Debug.Assert(reader.ReadByte() == 0, "Invalid sprite alignment padding");
            }
        }

        private void Align3(UndertaleWriter writer)
        {
            while ((writer.Position & 3) != 0)
            {
                writer.Write((byte)0);
            }
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
            Transparent = reader.ReadBoolean();
            Smooth = reader.ReadBoolean();
            Preload = reader.ReadBoolean();
            BBoxMode = reader.ReadUInt32();
            SepMasks = reader.ReadUInt32();
            OriginX = reader.ReadUInt32();
            OriginY = reader.ReadUInt32();
            if (reader.ReadInt32() == -1) // technically this seems to be able to occur on older versions, for special sprite types
            {
                IsSpecialType = true;
                SUnknownAlways1 = reader.ReadUInt32();
                SSpriteType = (SpriteType)reader.ReadUInt32();
                if (reader.undertaleData.GeneralInfo?.Major >= 2)
                {
                    GMS2PlaybackSpeed = reader.ReadSingle();
                    GMS2PlaybackSpeedType = reader.ReadUInt32();
                }

                switch (SSpriteType)
                {
                    case SpriteType.Normal:
                        Textures = reader.ReadUndertaleObject<UndertaleSimpleList<TextureEntry>>();
                        ReadMaskData(reader);
                        break;
                    case SpriteType.SWF:
                        {
                            //// ATTENTION: This code does not work all the time for some reason. ////

                            Debug.Assert(reader.ReadUInt32() == 8, "Invalid SWF sprite format");
                            Textures = reader.ReadUndertaleObject<UndertaleSimpleList<TextureEntry>>();
                            Align3(reader);

                            // Determining the length of the buffer literally requires the parsing of it... here goes.
                            uint begin = reader.Position;
                            Align3(reader);
                            int length1 = reader.ReadInt32() & 0x7FFFFFFF;
                            Debug.Assert(reader.ReadUInt32() == 8, "Invalid SWF sprite format after length1");
                            reader.Position += (uint)length1;
                            Align3(reader);
                            uint test = reader.ReadUInt32();
                            reader.Position += (test * 8) + 4;
                            uint count1 = reader.ReadUInt32();
                            reader.Position += 16;
                            int count2 = reader.ReadInt32();
                            reader.Position += 8;
                            for (int i = 0; i < count1; i++)
                            {
                                reader.Position += (reader.ReadUInt32() * 96) + 16;
                            }
                            for (int i = 0; i < count2; i++)
                            {
                                reader.Position += reader.ReadUInt32();
                                Align3(reader);
                            }
                            uint length = reader.Position - begin;
                            reader.Position = begin;

                            // Now that the length is calculated, read all of it into a buffer
                            S_SWF_Data = reader.ReadBytes((int)length);
                        }
                        break;
                    case SpriteType.Spine:
                        {
                            Align3(reader);

                            uint begin = reader.Position;
                            reader.ReadUInt32(); // version number
                            uint jsonLength = reader.ReadUInt32();
                            uint atlasLength = reader.ReadUInt32();
                            uint textureLength = reader.ReadUInt32();
                            reader.ReadUInt32(); // atlas tex width
                            reader.ReadUInt32(); // atlas tex height
                            reader.Position = begin;

                            S_Spine_Data = reader.ReadBytes((int)(24 + jsonLength + atlasLength + textureLength));
                        }
                        break;
                }
            }
            else
            {
                reader.Position -= 4;
                Textures = reader.ReadUndertaleObject<UndertaleSimpleList<TextureEntry>>();
                ReadMaskData(reader);
            }
        }

        private void ReadMaskData(UndertaleReader reader)
        {
            uint MaskCount = reader.ReadUInt32();
            CollisionMasks.Clear();
            uint total = 0;
            for (uint i = 0; i < MaskCount; i++)
            {
                uint len = (Width + 7) / 8 * Height;
                CollisionMasks.Add(new MaskEntry(reader.ReadBytes((int)len)));
                total += len;
            }
            while (total % 4 != 0)
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

        public void SerializePrePadding(UndertaleWriter writer)
        {
            Align3(writer);
        }

        public void UnserializePrePadding(UndertaleReader reader)
        {
            Align3(reader);
        }
    }
}
