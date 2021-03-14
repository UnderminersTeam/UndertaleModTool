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
    public enum AnimSpeedType : uint
    {
        FramesPerSecond = 0,
        FramesPerGameFrame = 1
    }

    public class UndertaleSpineTextureEntry : UndertaleObject, INotifyPropertyChanged
    {
        private int _PageWidth;
        private int _PageHeight;
        private byte[] _PNGBlob;

        public event PropertyChangedEventHandler PropertyChanged;

        public int PageWidth { get => _PageWidth; set { _PageWidth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PageWidth")); } }
        public int PageHeight { get => _PageHeight; set { _PageHeight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PageHeight")); } }
        public byte[] PNGBlob { get => _PNGBlob; set { _PNGBlob = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PNGBlob")); } }

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(PageWidth);
            writer.Write(PageHeight);
            writer.Write(PNGBlob.Length);
            writer.Write(PNGBlob);
        }

        public void Unserialize(UndertaleReader reader)
        {
            PageWidth = reader.ReadInt32();
            PageHeight = reader.ReadInt32();
            PNGBlob = reader.ReadBytes(reader.ReadInt32());
        }

        public override string ToString()
        {
            return $"UndertaleSpineTextureEntry ({PageWidth};{PageHeight})";
        }
    }

    public class UndertaleYYSWFTimelineObject : UndertaleObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int _CharID;
        private int _CharIndex;
        private int _Depth;
        private int _ClippingDepth;
        private float[] _TransformationMatrix; // matrix33
        private int[] _ColorMultiplyMatrix; // vec4 rgba
        private int[] _ColorAdditiveMatrix; // vec4 rgba

        private float _MinX;
        private float _MaxX;
        private float _MinY;
        private float _MaxY;

        public int CharID { get => _CharID; set { _CharID = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CharID")); } }
        public int CharIndex { get => _CharIndex; set { _CharIndex = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CharIndex")); } }
        public int Depth { get => _Depth; set { _Depth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Depth")); } }
        public int ClippingDepth { get => _ClippingDepth; set { _ClippingDepth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ClippingDepth")); } }
        public float[] TransformationMatrix { get => _TransformationMatrix; set { _TransformationMatrix = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TransformationMatrix")); } }
        public int[] ColorMultiplyMatrix { get => _ColorMultiplyMatrix; set { _ColorMultiplyMatrix = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ColorMultiplyMatrix")); } }
        public int[] ColorAdditiveMatrix { get => _ColorAdditiveMatrix; set { _ColorAdditiveMatrix = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ColorAdditiveMatrix")); } }
        public float MinX { get => _MinX; set { _MinX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MinX")); } }
        public float MaxX { get => _MaxX; set { _MaxX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MaxX")); } }
        public float MinY { get => _MinY; set { _MinY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MinY")); } }
        public float MaxY { get => _MaxY; set { _MaxY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MaxY")); } }

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(CharID);
            writer.Write(CharIndex);
            writer.Write(Depth);
            writer.Write(ClippingDepth);
            for (int i = 0; i < 4; i++)
                writer.Write(ColorMultiplyMatrix[i]);
            for (int i = 0; i < 4; i++)
                writer.Write(ColorAdditiveMatrix[i]);
            writer.Write(MinX);
            writer.Write(MaxX);
            writer.Write(MinY);
            writer.Write(MaxY);
            for (int i = 0; i < 9; i++)
                writer.Write(TransformationMatrix[i]);
        }

        public void Unserialize(UndertaleReader reader)
        {
            CharID = reader.ReadInt32();
            CharIndex = reader.ReadInt32();
            Depth = reader.ReadInt32();
            ClippingDepth = reader.ReadInt32();
            ColorMultiplyMatrix = new int[4]; // rgba
            for (int i = 0; i < 4; i++)
                ColorMultiplyMatrix[i] = reader.ReadInt32();
            ColorAdditiveMatrix = new int[4];
            for (int i = 0; i < 4; i++)
                ColorAdditiveMatrix[i] = reader.ReadInt32();
            MinX = reader.ReadSingle();
            MaxX = reader.ReadSingle();
            MinY = reader.ReadSingle();
            MaxY = reader.ReadSingle();
            TransformationMatrix = new float[9];
            for (int i = 0; i < 9; i++)
                TransformationMatrix[i] = reader.ReadSingle();
        }
    }

    public class UndertaleYYSWFTimelineFrame : UndertaleObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private UndertaleSimpleList<UndertaleYYSWFTimelineObject> _FrameObjects;
        private float _MinX;
        private float _MaxX;
        private float _MinY;
        private float _MaxY;

        public UndertaleSimpleList<UndertaleYYSWFTimelineObject> FrameObjects { get => _FrameObjects; set { _FrameObjects = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FrameObjects")); } }
        public float MinX { get => _MinX; set { _MinX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MinX")); } }
        public float MaxX { get => _MaxX; set { _MaxX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MaxX")); } }
        public float MinY { get => _MinY; set { _MinY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MinY")); } }
        public float MaxY { get => _MaxY; set { _MaxY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MaxY")); } }

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(FrameObjects.Count);
            writer.Write(MinX);
            writer.Write(MaxX);
            writer.Write(MinY);
            writer.Write(MaxY);
            foreach (var frameObject in FrameObjects)
            {
                writer.WriteUndertaleObject(frameObject);
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            int ii = reader.ReadInt32();
            MinX = reader.ReadSingle();
            MaxX = reader.ReadSingle();
            MinY = reader.ReadSingle();
            MaxY = reader.ReadSingle();
            FrameObjects = new UndertaleSimpleList<UndertaleYYSWFTimelineObject>();
            for (int i = 0; i < ii; i++)
            {
                var frameObject = reader.ReadUndertaleObject<UndertaleYYSWFTimelineObject>();
                FrameObjects.Add(frameObject);
            }
        }
    }

    public class UndertaleYYSWFCollisionMask : UndertaleObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private byte[] _RLEData; // heavily compressed and pre-processed!

        public byte[] RLEData { get => _RLEData; set { _RLEData = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RLEData")); } }

        public void Serialize(UndertaleWriter writer)
        {
            if (RLEData != null)
            {
                writer.Write(RLEData.Length);
                writer.Write(RLEData);
            }

            writer.Align(4);
        }

        public void Unserialize(UndertaleReader reader)
        {
            RLEData = reader.ReadBytes(reader.ReadInt32());

            reader.Align(4);
        }
    }

    public enum UndertaleYYSWFItemType : int
    {
        ItemInvalid,
        ItemShape,
        ItemBitmap,
        ItemFont,
        ItemTextField,
        ItemSprite
    }

    public class UndertaleYYSWFItem : UndertaleObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int _ID;
        public int ID { get => _ID; set { _ID = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ID")); } }

        private UndertaleYYSWFItemType _ItemType;
        public UndertaleYYSWFItemType ItemType { get => _ItemType; set { _ItemType = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ItemType")); } }

        public UndertaleYYSWFItem()
        {
            ItemType = UndertaleYYSWFItemType.ItemInvalid;
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((int)ItemType);
            writer.Write(ID);
        }

        public void Unserialize(UndertaleReader reader)
        {
            ItemType = (UndertaleYYSWFItemType)reader.ReadInt32();
            ID = reader.ReadInt32();
        }
    }

    public class UndertaleYYSWFTimeline : UndertaleObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int _Framerate;
        private float _MinX;
        private float _MaxX;
        private float _MinY;
        private float _MaxY;
        private int _MaskWidth;
        private int _MaskHeight;
        private UndertaleSimpleList<UndertaleYYSWFItem> _UsedItems;
        private UndertaleSimpleList<UndertaleYYSWFTimelineFrame> _Frames;
        private UndertaleSimpleList<UndertaleYYSWFCollisionMask> _CollisionMasks;

        public int Framerate { get => _Framerate; set { _Framerate = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Framerate")); } }
        public float MinX { get => _MinX; set { _MinX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MinX")); } }
        public float MaxX { get => _MaxX; set { _MaxX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MaxX")); } }
        public float MinY { get => _MinY; set { _MinY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MinY")); } }
        public float MaxY { get => _MaxY; set { _MaxY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MaxY")); } }
        public int MaskWidth { get => _MaskWidth; set { _MaskWidth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MaskWidth")); } }
        public int MaskHeight { get => _MaskHeight; set { _MaskHeight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MaskHeight")); } }
        public UndertaleSimpleList<UndertaleYYSWFItem> UsedItems { get => _UsedItems; set { _UsedItems = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UsedItems")); } }
        public UndertaleSimpleList<UndertaleYYSWFTimelineFrame> Frames { get => _Frames; set { _Frames = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Frames")); } }
        public UndertaleSimpleList<UndertaleYYSWFCollisionMask> CollisionMasks { get => _CollisionMasks; set { _CollisionMasks = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CollisionMasks")); } }

        public void Serialize(UndertaleWriter writer)
        {
            throw new NotImplementedException();
        }

        public void Unserialize(UndertaleReader reader)
        {
            UsedItems = reader.ReadUndertaleObject<UndertaleSimpleList<UndertaleYYSWFItem>>();
            Framerate = reader.ReadInt32();
            int fc = reader.ReadInt32();
            MinX = reader.ReadSingle();
            MaxX = reader.ReadSingle();
            MinY = reader.ReadSingle();
            MaxY = reader.ReadSingle();
            int mc = reader.ReadInt32();
            MaskWidth = reader.ReadInt32();
            MaskHeight = reader.ReadInt32();

            Frames = new UndertaleSimpleList<UndertaleYYSWFTimelineFrame>();
            for (int f = 0; f < fc; f++)
            {
                var yyswfFrame = reader.ReadUndertaleObject<UndertaleYYSWFTimelineFrame>();
                Frames.Add(yyswfFrame);
            }

            CollisionMasks = new UndertaleSimpleList<UndertaleYYSWFCollisionMask>();
            for (int m = 0; m < mc; m++)
            {
                var yyswfMask = reader.ReadUndertaleObject<UndertaleYYSWFCollisionMask>();
                CollisionMasks.Add(yyswfMask);
            }
        }
    }

    public class UndertaleYYSWF : UndertaleObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private byte[] _JPEGTable; // prebaked embedded JPEG? no idea!
        private int _Version;
        private UndertaleYYSWFTimeline _Timeline;

        public byte[] JPEGTable { get => _JPEGTable; set { _JPEGTable = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("JPEGTable")); } }
        public int Version { get => _Version; set { _Version = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Version")); } }
        public UndertaleYYSWFTimeline Timeline { get => _Timeline; set { _Timeline = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Timeline")); } }

        public void Serialize(UndertaleWriter writer)
        {
            writer.Align(4);
            int len = int.MinValue;
            if (JPEGTable != null)
            {
                len |= JPEGTable.Length;
            }

            writer.Write(len);
            writer.Write(Version);
            if (JPEGTable != null)
            {
                writer.Write(JPEGTable);
            }

            writer.Align(4);
            writer.WriteUndertaleObject(Timeline);
        }

        public void Unserialize(UndertaleReader reader)
        {
            reader.Align(4);
            int jpeglen = reader.ReadInt32() & (~int.MinValue); // the length is ORed with int.MinValue.
            Version = reader.ReadInt32();
            Debug.Assert(Version == 8, "Invalid YYSWF version data!");

            if (jpeglen > 0)
            {
                JPEGTable = reader.ReadBytes(jpeglen);
            }

            reader.Align(4);
            Timeline = reader.ReadUndertaleObject<UndertaleYYSWFTimeline>();
        }
    }

    public class UndertaleSprite : UndertaleNamedResource, PrePaddedObject, INotifyPropertyChanged
    {
        private UndertaleString _Name;
        private uint _Width;
        private uint _Height;
        private int _MarginLeft;
        private int _MarginRight;
        private int _MarginBottom;
        private int _MarginTop;
        private bool _Transparent;
        private bool _Smooth;
        private bool _Preload;
        private uint _BBoxMode;
        private SepMaskType _SepMasks; // Whether or not multiple collision masks will be used. 0-2.
        private int _OriginX;
        private int _OriginY;
        private uint _GMS2Version = 1;
        private SpriteType _SSpriteType = 0;
        private float _GMS2PlaybackSpeed = 15.0f;
        private AnimSpeedType _GMS2PlaybackSpeedType = 0;
        private bool _IsSpecialType = false;
        private int _SpineVersion;
        private string _SpineJSON;
        private string _SpineAtlas;
        private UndertaleSimpleList<UndertaleSpineTextureEntry> _SpineTextures; // a list of embedded PNGs really.

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public uint Width { get => _Width; set { _Width = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Width")); } }
        public uint Height { get => _Height; set { _Height = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Height")); } }
        public int MarginLeft { get => _MarginLeft; set { _MarginLeft = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MarginLeft")); } }
        public int MarginRight { get => _MarginRight; set { _MarginRight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MarginRight")); } }
        public int MarginBottom { get => _MarginBottom; set { _MarginBottom = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MarginBottom")); } }
        public int MarginTop { get => _MarginTop; set { _MarginTop = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MarginTop")); } }
        public bool Transparent { get => _Transparent; set { _Transparent = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Transparent")); } }
        public bool Smooth { get => _Smooth; set { _Smooth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Smooth")); } }
        public bool Preload { get => _Preload; set { _Preload = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Preload")); } }
        public uint BBoxMode { get => _BBoxMode; set { _BBoxMode = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BBoxMode")); } }
        public SepMaskType SepMasks { get => _SepMasks; set { _SepMasks = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SepMasks")); } }
        public int OriginX { get => _OriginX; set { _OriginX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OriginX")); } }
        public int OriginY { get => _OriginY; set { _OriginY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OriginY")); } }
        public UndertaleSimpleList<TextureEntry> Textures { get; private set; } = new UndertaleSimpleList<TextureEntry>();
        public ObservableCollection<MaskEntry> CollisionMasks { get; } = new ObservableCollection<MaskEntry>();
        
        // Special sprite types (always used in GMS2)
        public uint SVersion { get => _GMS2Version; set { _GMS2Version = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SVersion")); } }
        public SpriteType SSpriteType { get => _SSpriteType; set { _SSpriteType = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SSpriteType")); } }
        public float GMS2PlaybackSpeed { get => _GMS2PlaybackSpeed; set { _GMS2PlaybackSpeed = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2PlaybackSpeed")); } }
        public AnimSpeedType GMS2PlaybackSpeedType { get => _GMS2PlaybackSpeedType; set { _GMS2PlaybackSpeedType = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2PlaybackSpeedType")); } }
        public bool IsSpecialType { get => _IsSpecialType; set { _IsSpecialType = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSpecialType")); } }

        public int SpineVersion { get => _SpineVersion; set { _SpineVersion = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpineVersion")); } }
        public string SpineJSON { get => _SpineJSON; set { _SpineJSON = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpineJSON")); } }
        public string SpineAtlas { get => _SpineAtlas; set { _SpineAtlas = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpineAtlas")); } }
        public UndertaleSimpleList<UndertaleSpineTextureEntry> SpineTextures { get => _SpineTextures; set { _SpineTextures = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpineTextures")); } }

        public bool IsSpineSprite { get => SpineJSON != null && SpineAtlas != null && SpineTextures != null; }
        public bool IsYYSWFSprite { get => YYSWF != null; }

        private UndertaleYYSWF _YYSWF;

        public UndertaleYYSWF YYSWF { get => _YYSWF; set { _YYSWF = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("YYSWF")); } }

        public UndertaleSequence V2Sequence;

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }

        public MaskEntry NewMaskEntry()
        {
            MaskEntry newEntry = new MaskEntry();
            uint len = (Width + 7) / 8 * Height;
            newEntry.Data = new byte[len];
            return newEntry;
        }

        public enum SpriteType : uint
        {
            Normal = 0,
            SWF = 1,
            Spine = 2
        }

        public enum SepMaskType : uint
        {
            AxisAlignedRect = 0,
            Precise = 1,
            RotatedRect = 2
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
            writer.Write((uint) SepMasks);
            writer.Write(OriginX);
            writer.Write(OriginY);
            if (IsSpecialType)
            {
                uint patchPos = 0;

                writer.Write(-1);
                writer.Write(SVersion);
                writer.Write((uint)SSpriteType);
                if (writer.undertaleData.GeneralInfo?.Major >= 2)
                {
                    writer.Write(GMS2PlaybackSpeed);
                    writer.Write((uint)GMS2PlaybackSpeedType);
                    if (SVersion >= 2)
                    {
                        patchPos = writer.Position;
                        writer.Write((int)0);
                    }
                }

                switch (SSpriteType)
                {
                    case SpriteType.Normal:
                        writer.WriteUndertaleObject(Textures);
                        WriteMaskData(writer);
                        break;
                    case SpriteType.SWF:
                        
                        break;
                    case SpriteType.Spine:
                        writer.Align(4);
                        
                        byte[] encodedJson = EncodeSpineBlob(Encoding.UTF8.GetBytes(SpineJSON));
                        byte[] encodedAtlas = EncodeSpineBlob(Encoding.UTF8.GetBytes(SpineAtlas));

                        // the header.
                        writer.Write(SpineVersion);
                        writer.Write(encodedJson.Length);
                        writer.Write(encodedAtlas.Length);
                        writer.Write(SpineTextures.Count);

                        // the data.
                        writer.Write(encodedJson);
                        writer.Write(encodedAtlas);

                        // the length is stored in the header, so we can't use the list's method.
                        foreach (var tex in SpineTextures)
                        {
                            writer.WriteUndertaleObject(tex);
                        }

                        break;
                }

                // Sequence
                if (patchPos != 0 && V2Sequence != null) // Normal compiler also checks for sprite type to be normal, but whatever!
                {
                    uint returnTo = writer.Position;
                    writer.Position = patchPos;
                    writer.Write(returnTo);
                    writer.Position = returnTo;
                    writer.Write((int)1);
                    writer.WriteUndertaleObject(V2Sequence);
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

        private byte[] DecodeSpineBlob(byte[] blob)
        {
            // don't ask.
            uint k = 42;
            for (int i = 0; i < blob.Length; i++)
            {
                blob[i] -= (byte)k;
                k *= k + 1;
            }
            return blob;
        }

        private byte[] EncodeSpineBlob(byte[] blob)
        {
            // don't ask.
            uint k = 42;
            for (int i = 0; i < blob.Length; i++)
            {
                blob[i] += (byte)k;
                k *= k + 1;
            }
            return blob;
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Width = reader.ReadUInt32();
            Height = reader.ReadUInt32();
            MarginLeft = reader.ReadInt32();
            MarginRight = reader.ReadInt32();
            MarginBottom = reader.ReadInt32();
            MarginTop = reader.ReadInt32();
            Transparent = reader.ReadBoolean();
            Smooth = reader.ReadBoolean();
            Preload = reader.ReadBoolean();
            BBoxMode = reader.ReadUInt32();
            SepMasks = (SepMaskType) reader.ReadUInt32();
            OriginX = reader.ReadInt32();
            OriginY = reader.ReadInt32();
            if (reader.ReadInt32() == -1) // technically this seems to be able to occur on older versions, for special sprite types
            {
                int sequenceOffset = 0;

                IsSpecialType = true;
                SVersion = reader.ReadUInt32();
                SSpriteType = (SpriteType)reader.ReadUInt32();
                if (reader.undertaleData.GeneralInfo?.Major >= 2)
                {
                    GMS2PlaybackSpeed = reader.ReadSingle();
                    GMS2PlaybackSpeedType = (AnimSpeedType)reader.ReadUInt32();
                    if (SVersion >= 2)
                        sequenceOffset = reader.ReadInt32();
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
                            YYSWF = reader.ReadUndertaleObject<UndertaleYYSWF>();

                            Console.WriteLine("Attempt to read YYSWF data...");
                        }
                        break;
                    case SpriteType.Spine:
                        {
                            reader.Align(4);

                            SpineVersion = reader.ReadInt32();
                            Debug.Assert(SpineVersion == 2, "Invalid Spine format version number, expected 2, got " + SpineVersion);
                            int jsonLength = reader.ReadInt32();
                            int atlasLength = reader.ReadInt32();
                            int textures = reader.ReadInt32();

                            SpineJSON = Encoding.UTF8.GetString(DecodeSpineBlob(reader.ReadBytes(jsonLength)));
                            SpineAtlas = Encoding.UTF8.GetString(DecodeSpineBlob(reader.ReadBytes(atlasLength)));

                            // the length is stored before json and atlases so we can't use ReadUndertaleObjectList
                            // same goes for serialization.
                            SpineTextures = new UndertaleSimpleList<UndertaleSpineTextureEntry>();
                            for (int t = 0; t < textures; t++)
                            {
                                SpineTextures.Add(reader.ReadUndertaleObject<UndertaleSpineTextureEntry>());
                            }
                        }
                        break;
                }

                if (sequenceOffset != 0)
                {
                    if (reader.ReadInt32() != 1)
                        throw new IOException("Expected 1");
                    V2Sequence = reader.ReadUndertaleObject<UndertaleSequence>();
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
            uint len = (Width + 7) / 8 * Height;
            CollisionMasks.Clear();
            uint total = 0;
            for (uint i = 0; i < MaskCount; i++)
            {
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

        public uint CalculateMaskDataSize(uint width, uint height, uint maskcount)
        {
            uint roundedWidth = (width + 7) / 8 * 8; // round to multiple of 8
            uint dataBits = roundedWidth * height * maskcount;
            uint dataBytes = ((dataBits + 31) / 32 * 32) / 8; // round to multiple of 4 bytes
            return dataBytes;
        }

        public void SerializePrePadding(UndertaleWriter writer)
        {
            writer.Align(4);
        }

        public void UnserializePrePadding(UndertaleReader reader)
        {
            reader.Align(4);
        }
    }
}
