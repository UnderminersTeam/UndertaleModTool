using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UndertaleModLib.Models;

namespace UndertaleModLib
{
    /**
     * TODO: This is not the cleanest implementation, but I was focusing on clean interface.
     * Could probably use some refactoring or a complete rewrite.
     */

    public interface UndertaleResourceRef
    {
        Type ResourceType { get; }
        object Resource { get; set; }
        void PostUnserialize(UndertaleReader reader);
    }

    public class UndertaleResourceById<T> : UndertaleResourceRef where T : UndertaleResource, new()
    {
        public string ResourceChunkType { get; }
        public int CachedId { get; set; } = -1;
        public T Resource { get; set; }

        public Type ResourceType => typeof(T);
        object UndertaleResourceRef.Resource { get => Resource; set => Resource = (T)value; }

        public UndertaleResourceById(string type, int id = -1)
        {
            this.ResourceChunkType = type;
            this.CachedId = id;
        }

        public UndertaleResourceById(string type, T res)
        {
            this.ResourceChunkType = type;
            this.Resource = res;
        }

        public int Serialize(UndertaleWriter writer)
        {
            if (Resource != null)
            {
                CachedId = ((UndertaleListChunk<T>)writer.undertaleData.FORM.Chunks[ResourceChunkType]).List.IndexOf(Resource);
                if (CachedId < 0)
                    throw new IOException("Unregistered object");
            }
            else
            {
                CachedId = -1;
            }
            return CachedId;
        }

        public void Unserialize(UndertaleReader reader, int id)
        {
            if (id < 0 && id != -1)
                throw new IOException("Invalid value for resource ID (" + ResourceChunkType + "): " + id);
            CachedId = id;
            reader.RequestResourceUpdate(this);
        }

        public void PostUnserialize(UndertaleReader reader)
        {
            IList<T> list = ((UndertaleListChunk<T>)reader.undertaleData.FORM.Chunks[ResourceChunkType]).List;
            if (CachedId >= list.Count)
                throw new IOException("Invalid value for resource ID of type " + ResourceChunkType + ": " + CachedId + " (there are only " + list.Count + ")");
            Resource = CachedId >= 0 ? list[CachedId] : default(T);
        }

        public override string ToString()
        {
            return String.Format("{0}@{1}", Resource?.ToString() ?? "(null)", CachedId);
        }
    }

    public class UndertaleReader : BinaryReader
    {
        public UndertaleReader(Stream input) : base(input)
        {
        }

        public uint Position
        {
            get { return (uint)BaseStream.Position; }
            set { BaseStream.Seek((int)value, SeekOrigin.Begin); }
        }

        public UndertaleChunk ReadUndertaleChunk()
        {
            return UndertaleChunk.Unserialize(this);
        }

        private List<UndertaleResourceRef> resUpdate = new List<UndertaleResourceRef>();
        internal UndertaleData undertaleData;

        public UndertaleData ReadUndertaleData()
        {
            UndertaleData data = new UndertaleData();
            undertaleData = data;

            resUpdate.Clear();

            string name = new string(ReadChars(4));
            if (name != "FORM")
                throw new IOException("Root chunk is " + name + " not FORM");
            uint length = ReadUInt32();
            data.FORM = new UndertaleChunkFORM();
            Debug.Assert(data.FORM.Name == name);
            data.FORM.Length = length;

            var lenReader = EnsureLengthFromHere(data.FORM.Length);
            data.FORM.UnserializeChunk(this);
            lenReader.ToHere();

            foreach (UndertaleResourceRef res in resUpdate)
                res.PostUnserialize(this);
            resUpdate.Clear();

            return data;
        }

        internal void RequestResourceUpdate(UndertaleResourceRef res)
        {
            resUpdate.Add(res);
        }

        public override bool ReadBoolean()
        {
            uint a = ReadUInt32();
            if (a == 0)
                return false;
            if (a == 1)
                return true;
            throw new IOException("Invalid boolean value: " + a);
        }

        private Dictionary<uint, UndertaleObject> objectPool = new Dictionary<uint, UndertaleObject>();
        private Dictionary<UndertaleObject, uint> objectPoolRev = new Dictionary<UndertaleObject, uint>();

        public Dictionary<uint, UndertaleObject> GetOffsetMap()
        {
            return objectPool;
        }

        public Dictionary<UndertaleObject, uint> GetOffsetMapRev()
        {
            return objectPoolRev;
        }

        public T GetUndertaleObjectAtAddress<T>(uint address) where T : UndertaleObject, new()
        {
            if (address == 0)
                return default(T);
            UndertaleObject obj;
            if (!objectPool.TryGetValue(address, out obj))
            {
                obj = new T();
                objectPool.Add(address, obj);
                objectPoolRev.Add(obj, address);
            }
            return (T)obj;
        }

        public uint GetAddressForUndertaleObject(UndertaleObject obj)
        {
            if (obj == null)
                return 0;
            return objectPoolRev[obj];
        }

        public T ReadUndertaleObject<T>() where T : UndertaleObject, new()
        {
            try
            {
                T obj = GetUndertaleObjectAtAddress<T>(Position);
                obj.Unserialize(this);
                return obj;
            }
            catch(Exception e)
            {
                throw new UndertaleSerializationException(e.Message + "\nat " + Position.ToString("X8") + " while reading object " + typeof(T).FullName, e);
            }
        }

        public T ReadUndertaleObjectPointer<T>() where T : UndertaleObject, new()
        {
            return GetUndertaleObjectAtAddress<T>(ReadUInt32());
        }

        public UndertaleString ReadUndertaleString()
        {
            uint addr = ReadUInt32();

            if (addr == 0)
                return null;

            // Normally, the strings point directly to the string content
            // This may be done that way because it's faster when the game accesses them, but for our purposes it's better to access the whole string resource object
            return GetUndertaleObjectAtAddress<UndertaleString>(addr - 4);
        }

        public class EnsureLengthOperation
        {
            private readonly UndertaleReader reader;
            private readonly int startPos;
            private readonly uint expectedLength;
            internal EnsureLengthOperation(UndertaleReader reader, uint expectedLength)
            {
                this.reader = reader;
                this.startPos = (int)reader.BaseStream.Position;
                this.expectedLength = expectedLength;
            }
            public void ToHere()
            {
                int endPos = (int)reader.BaseStream.Position;
                uint length = (uint)(endPos - startPos);
                if (length != expectedLength)
                {
                    int diff = (int)expectedLength - (int)length;
                    Console.WriteLine("WARNING: File specified length " + expectedLength + ", but read only " + length + " (" + diff + " padding?)");
                    if (diff > 0)
                        reader.Position = reader.Position + (uint)diff;
                    else
                        throw new IOException("Read underflow");
                }
            }
        }

        public EnsureLengthOperation EnsureLengthFromHere(uint expectedLength)
        {
            return new EnsureLengthOperation(this, expectedLength);
        }

        public int ReadInt24()
        {
            return ReadByte() | ReadByte() << 8 | (sbyte)ReadByte() << 16;
        }

        public uint ReadUInt24()
        {
            return (uint)(ReadByte() | ReadByte() << 8 | ReadByte() << 16);
        }
    }

    public class UndertaleWriter : BinaryWriter
    {
        internal UndertaleData undertaleData;

        public UndertaleWriter(Stream output) : base(output)
        {
        }

        public uint Position
        {
            get { return (uint)BaseStream.Position; }
            set { Seek((int)value, SeekOrigin.Begin); }
        }

        public void Write(UndertaleChunk obj)
        {
            obj.Serialize(this);
        }

        public override void Write(bool b)
        {
            Write(b ? (uint)1 : (uint)0);
        }

        public void WriteUndertaleData(UndertaleData data)
        {
            undertaleData = data;
            Write(data.FORM);
        }

        private Dictionary<UndertaleObject, uint> objectPool = new Dictionary<UndertaleObject, uint>();
        private Dictionary<UndertaleObject, List<uint>> pendingWrites = new Dictionary<UndertaleObject, List<uint>>();
        private Dictionary<UndertaleObject, List<uint>> pendingStringWrites = new Dictionary<UndertaleObject, List<uint>>();

        public uint GetAddressForUndertaleObject(UndertaleObject obj)
        {
            if (obj == null)
                return 0;
            if (!objectPool.ContainsKey(obj))
                throw new KeyNotFoundException();
            return objectPool[obj];
        }

        public void WriteUndertaleObject<T>(T obj) where T : UndertaleObject, new()
        {
            try {
                if (objectPool.ContainsKey(obj))
                    throw new IOException("Writing object twice");
                uint objectAddr = Position;
                objectPool.Add(obj, objectAddr);
                obj.Serialize(this);
                if (pendingWrites.ContainsKey(obj))
                {
                    uint currentPos = Position;
                    foreach (uint pointerAddr in pendingWrites[obj])
                    {
                        Position = pointerAddr;
                        Write(objectAddr);
                    }
                    Position = currentPos;
                }
                if (pendingStringWrites.ContainsKey(obj))
                {
                    uint currentPos = Position;
                    foreach (uint pointerAddr in pendingStringWrites[obj])
                    {
                        Position = pointerAddr;
                        Write(objectAddr + 4);
                    }
                    Position = currentPos;
                }
            }
            catch (Exception e)
            {
                throw new UndertaleSerializationException(e.Message + "\nat " + Position.ToString("X8") + " while writing object " + typeof(T).FullName, e);
            }
        }

        public void WriteUndertaleObjectPointer<T>(T obj) where T : UndertaleObject, new()
        {
            if (obj == null)
            {
                Write((uint)0x00000000u);
                return;
            }

            if (objectPool.ContainsKey(obj))
            {
                Write(objectPool[obj]);
            }
            else
            {
                if (!pendingWrites.ContainsKey(obj))
                    pendingWrites.Add(obj, new List<uint>());
                pendingWrites[obj].Add(Position);
                Write(0xDEADC0DEu);
            }
        }

        public void WriteUndertaleString(UndertaleString obj)
        {
            if (obj == null)
            {
                Write((uint)0x0000000u);
                return;
            }

            if (objectPool.ContainsKey(obj))
            {
                Write(objectPool[obj] + 4);
            }
            else
            {
                if (!pendingStringWrites.ContainsKey(obj))
                    pendingStringWrites.Add(obj, new List<uint>());
                pendingStringWrites[obj].Add(Position);
                Write(0xDEADC0DEu);
            }
        }

        public class WriteLengthOperation
        {
            private readonly UndertaleWriter writer;
            private readonly uint writePos;
            private uint? startPos = null;
            internal WriteLengthOperation(UndertaleWriter writer)
            {
                this.writer = writer;
                this.writePos = writer.Position;
                writer.Write(0xDEADC0DEu);
            }
            public void FromHere()
            {
                this.startPos = writer.Position;
            }
            public uint ToHere()
            {
                if (!startPos.HasValue)
                    throw new InvalidOperationException("Forgot to call FromHere()");

                uint endPos = writer.Position;
                writer.Position = writePos;
                uint valueToWrite = endPos - startPos.Value;
                writer.Write(valueToWrite);
                writer.Position = endPos;
                return valueToWrite;
            }
        }

        public WriteLengthOperation WriteLengthHere()
        {
            return new WriteLengthOperation(this);
        }

        public void WriteInt24(int val)
        {
            Write((byte)val);
            Write((byte)(val >> 8));
            Write((byte)(val >> 16));
        }
        //TODO: UInt24?
    }

    public static class UndertaleIO
    {
        public static UndertaleData Read(Stream stream)
        {
            UndertaleReader reader = new UndertaleReader(stream);
            return reader.ReadUndertaleData();
        }

        public static void Write(Stream stream, UndertaleData data)
        {
            UndertaleWriter writer = new UndertaleWriter(stream);
            writer.WriteUndertaleData(data);
        }

        public static Dictionary<uint, UndertaleObject> GenerateOffsetMap(Stream stream)
        {
            UndertaleReader reader = new UndertaleReader(stream);
            reader.ReadUndertaleData();
            return reader.GetOffsetMap();
        }
    }
}
