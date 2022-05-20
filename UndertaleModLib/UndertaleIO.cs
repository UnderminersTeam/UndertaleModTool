﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModLib
{
    /**
     * TODO: This is not the cleanest implementation, but I was focusing on clean interface.
     * Could probably use some refactoring or a complete rewrite.
     */

    public interface UndertaleResourceRef : UndertaleObject
    {
        object Resource { get; set; }
        void PostUnserialize(UndertaleReader reader);
        int SerializeById(UndertaleWriter writer);
    }

    public class UndertaleResourceById<T, ChunkT> : UndertaleResourceRef, IDisposable where T : UndertaleResource, new() where ChunkT : UndertaleListChunk<T>
    {
        public int CachedId { get; set; } = -1;
        public T Resource { get; set; }

        object UndertaleResourceRef.Resource { get => Resource; set => Resource = (T)value; }

        public UndertaleResourceById()
        {
            this.CachedId = -1;
        }

        public UndertaleResourceById(int id = -1)
        {
            this.CachedId = id;
        }

        public UndertaleResourceById(T res)
        {
            this.Resource = res;
        }

        public UndertaleResourceById(T res, int id = -1)
        {
            this.Resource = res;
            this.CachedId = id;
        }

        private static ChunkT FindListChunk(UndertaleData data)
        {
            if (data.FORM.ChunksTypeDict.TryGetValue(typeof(ChunkT), out UndertaleChunk chunk))
                return chunk as ChunkT;
            else
                return null;
        }

        public int SerializeById(UndertaleWriter writer)
        {
            ChunkT chunk = FindListChunk(writer.undertaleData);
            if (chunk != null)
            {
                if (Resource != null)
                {
                    CachedId = chunk.IndexDict[Resource];
                    if (CachedId < 0)
                        throw new IOException("Unregistered object");
                }
                else
                {
                    if (typeof(ChunkT) == typeof(UndertaleChunkAGRP))
                        CachedId = 0;
                    else
                        CachedId = -1;
                }
            }
            return CachedId;
        }

        public void UnserializeById(UndertaleReader reader, int id)
        {
            if (id < -1)
                throw new IOException("Invalid value for resource ID (" + typeof(ChunkT).Name + "): " + id);
            CachedId = id;
            reader.RequestResourceUpdate(this);
        }

        public void PostUnserialize(UndertaleReader reader)
        {
            IList<T> list = FindListChunk(reader.undertaleData)?.List;
            if (list != null)
            {
                if (typeof(ChunkT) == typeof(UndertaleChunkAGRP) && CachedId == reader.undertaleData.GetBuiltinSoundGroupID() && list.Count == 0) // I won't even ask why this works like that
                {
                    Resource = default;
                    return;
                }
                if (CachedId >= list.Count)
                {
                    reader.SubmitWarning("Invalid value for resource ID of type " + typeof(ChunkT).Name + ": " + CachedId + " (there are only " + list.Count + ")");
                    return;
                }
                Resource = CachedId >= 0 ? list[CachedId] : default;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return (Resource?.ToString() ?? "(null)") + GetMarkerSuffix();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Resource = default;
        }

        public string GetMarkerSuffix()
        {
            return "@" + CachedId;
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.resourceIDRefsToWrite.Add(new ValueTuple<uint, UndertaleResourceRef>(writer.Position, this));
            writer.Write((int)0);
        }

        public void Unserialize(UndertaleReader reader)
        {
            UnserializeById(reader, reader.ReadInt32());
        }
    }

    public class UndertaleReader : Util.FileBinaryReader
    {
        /// <summary>
        /// function to delegate warning messages to
        /// </summary>
        /// <param name="warning"></param>
        public delegate void WarningHandlerDelegate(string warning);
        /// <summary>
        /// function to delegate informational messages to
        /// </summary>
        /// <param name="message"></param>
        public delegate void MessageHandlerDelegate(string message);
        private WarningHandlerDelegate WarningHandler;
        private MessageHandlerDelegate MessageHandler;

        public UndertaleReader(Stream input,
                               WarningHandlerDelegate warningHandler = null, MessageHandlerDelegate messageHandler = null) : base(input)
        {
            WarningHandler = warningHandler;
            MessageHandler = messageHandler;
        }

        // TODO: This would be more useful if it reported location like the exceptions did
        public void SubmitWarning(string warning)
        {
            if (WarningHandler != null)
                WarningHandler.Invoke(warning);
            else
                throw new IOException(warning);
        }

        public void SubmitMessage(string message)
        {
            if (MessageHandler != null)
                MessageHandler.Invoke(message);
            else
                Debug.WriteLine(message);
        }

        public string LastChunkName;
        public List<string> AllChunkNames;
        public bool GMS2_3 = false;
        public bool Bytecode14OrLower = false;

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

            string name = ReadChars(4);
            if (name != "FORM")
                throw new IOException("Root chunk is " + name + " not FORM");
            uint length = ReadUInt32();
            data.FORM = new UndertaleChunkFORM();
            DebugUtil.Assert(data.FORM.Name == name);
            data.FORM.Length = length;

            var lenReader = EnsureLengthFromHere(data.FORM.Length);
            data.FORM.UnserializeChunk(this);
            lenReader.ToHere();

            SubmitMessage("Resolving resource IDs...");
            foreach (UndertaleResourceRef res in resUpdate)
                res.PostUnserialize(this);
            resUpdate.Clear();

            data.BuiltinList = new BuiltinList(data);
            Decompiler.AssetTypeResolver.InitializeTypes(data);

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
        private HashSet<uint> unreadObjects = new HashSet<uint>();

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
                unreadObjects.Add(address);
            }
            return (T)obj;
        }

        public uint GetAddressForUndertaleObject(UndertaleObject obj)
        {
            if (obj == null)
                return 0;
            return objectPoolRev[obj];
        }

        public void ReadUndertaleObject<T>(T obj) where T : UndertaleObject, new()
        {
            try
            {
                var expectedAddress = GetAddressForUndertaleObject(obj);
                if (expectedAddress != Position)
                {
                    SubmitWarning("Reading misaligned at " + Position.ToString("X8") + ", realigning back to " + expectedAddress.ToString("X8") + "\nHIGH RISK OF DATA LOSS! The file is probably corrupted, or uses unsupported features\nProceed at your own risk");
                    Position = expectedAddress;
                }
                unreadObjects.Remove(Position);
                obj.Unserialize(this);
            }
            catch (Exception e)
            {
                throw new UndertaleSerializationException(e.Message + "\nat " + Position.ToString("X8") + " while reading object " + typeof(T).FullName, e);
            }
        }

        public void ReadUndertaleObject<T>(T obj, uint endPosition) where T : UndertaleObjectEndPos, new()
        {
            try
            {
                var expectedAddress = GetAddressForUndertaleObject(obj);
                if (expectedAddress != Position)
                {
                    SubmitWarning("Reading misaligned at " + Position.ToString("X8") + ", realigning back to " + expectedAddress.ToString("X8") + "\nHIGH RISK OF DATA LOSS! The file is probably corrupted, or uses unsupported features\nProceed at your own risk");
                    Position = expectedAddress;
                }
                unreadObjects.Remove(Position);
                obj.Unserialize(this, endPosition);
            }
            catch (Exception e)
            {
                throw new UndertaleSerializationException(e.Message + "\nat " + Position.ToString("X8") + " while reading object " + typeof(T).FullName, e);
            }
        }

        public void ReadUndertaleObject<T>(T obj, int length) where T : UndertaleObjectLenCheck, new()
        {
            try
            {
                var expectedAddress = GetAddressForUndertaleObject(obj);
                if (expectedAddress != Position)
                {
                    SubmitWarning("Reading misaligned at " + Position.ToString("X8") + ", realigning back to " + expectedAddress.ToString("X8") + "\nHIGH RISK OF DATA LOSS! The file is probably corrupted, or uses unsupported features\nProceed at your own risk");
                    Position = expectedAddress;
                }
                unreadObjects.Remove(Position);
                obj.Unserialize(this, length);
            }
            catch (Exception e)
            {
                throw new UndertaleSerializationException(e.Message + "\nat " + Position.ToString("X8") + " while reading object " + typeof(T).FullName, e);
            }
        }

        public T ReadUndertaleObject<T>() where T : UndertaleObject, new()
        {
            T obj = GetUndertaleObjectAtAddress<T>(Position);
            ReadUndertaleObject(obj);
            return obj;
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

        public void ThrowIfUnreadObjects()
        {
            if (unreadObjects.Count > 0)
            {
                throw new IOException("Found pointer targets that were never read:\n" + String.Join("\n", unreadObjects.Take(10).Select((x) => "0x" + x.ToString("X8") + " (" + objectPool[x].GetType().Name + ")")) + (unreadObjects.Count > 10 ? "\n(and more, " + unreadObjects.Count + " total)" : ""));
            }
        }

        public class EnsureLengthOperation
        {
            private readonly UndertaleReader reader;
            private readonly int startPos;
            private readonly uint expectedLength;
            internal EnsureLengthOperation(UndertaleReader reader, uint expectedLength)
            {
                this.reader = reader;
                this.startPos = (int)reader.Position;
                this.expectedLength = expectedLength;
            }
            public void ToHere()
            {
                int endPos = (int)reader.Position;
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

        public void Align(int alignment, byte paddingbyte = 0x00)
        {
            while ((Position & (alignment - 1)) != paddingbyte)
            {
                DebugUtil.Assert(ReadByte() == paddingbyte, "Invalid alignment padding");
            }
        }

        public EnsureLengthOperation EnsureLengthFromHere(uint expectedLength)
        {
            return new EnsureLengthOperation(this, expectedLength);
        }
    }

    public class UndertaleWriter : Util.FileBinaryWriter
    {
        internal UndertaleData undertaleData;

        public string LastChunkName;
        public uint LastBytecodeAddress = 0;
        public bool Bytecode14OrLower;

        public delegate void MessageHandlerDelegate(string message);
        private MessageHandlerDelegate MessageHandler;

        public UndertaleWriter(Stream output, MessageHandlerDelegate messageHandler = null) : base(output)
        {
            MessageHandler = messageHandler;
        }

        public void SubmitMessage(string message)
        {
            if (MessageHandler != null)
                MessageHandler.Invoke(message);
            else
                Debug.WriteLine(message);
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
            Bytecode14OrLower = data?.GeneralInfo?.BytecodeVersion <= 14;

            // Figure out the last chunk by iterating identically as it does when serializing
            foreach (var chunk in data.FORM.Chunks)
            {
                LastChunkName = chunk.Key;
            }

            // Generate the object index dictionaries for acceleration of "UndertaleResourceById.SerializeById()"
            var listChunks = data.FORM.Chunks.Values.Select(x => x as IUndertaleListChunk);
            Parallel.ForEach(listChunks.Where(x => x is not null), (chunk) =>
            {
                chunk.GenerateIndexDict();
            });
            UndertaleIO.IsDictionaryCleared = false;

            Write(data.FORM);
        }

        private Dictionary<UndertaleObject, uint> objectPool = new Dictionary<UndertaleObject, uint>();
        private Dictionary<UndertaleObject, List<uint>> pendingWrites = new Dictionary<UndertaleObject, List<uint>>();
        private Dictionary<UndertaleObject, List<uint>> pendingStringWrites = new Dictionary<UndertaleObject, List<uint>>();
        private List<ValueTuple<uint, uint>> intsToWriteParallel = new List<ValueTuple<uint, uint>>();
        public List<ValueTuple<uint, UndertaleResourceRef>> resourceIDRefsToWrite = new List<ValueTuple<uint, UndertaleResourceRef>>();

        public void Flush(UndertaleData data)
        {
            SubmitMessage("Writing object references...");

            var intsToWriteParallelSorted = intsToWriteParallel.AsParallel()
                                                               .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                                               .OrderBy(x => x.Item1);
            foreach (var pair in intsToWriteParallelSorted)
            {
                Position = pair.Item1;
                Write(pair.Item2);
            }

            var resourceIDRefsToWriteSorted = resourceIDRefsToWrite.AsParallel()
                                                                   .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                                                   .OrderBy(x => x.Item1);
            foreach (var pair in resourceIDRefsToWriteSorted)
            {
                int id = pair.Item2.SerializeById(this);
                Position = pair.Item1;
                Write(id);
            }

            SubmitMessage("Clearing temporary dictionaries...");
            var listChunks = data.FORM.Chunks.Values.Select(x => x as IUndertaleListChunk);
            Parallel.ForEach(listChunks.Where(x => x is not null), (chunk) =>
            {
                chunk.ClearIndexDict();
            });
            UndertaleIO.IsDictionaryCleared = true;

            SubmitMessage("Flushing remaining file buffer data to disk...");
            base.Flush();
        }

        public Dictionary<UndertaleObject, uint> GetObjectPool()
        {
            return objectPool;
        }

        public uint GetAddressForUndertaleObject(UndertaleObject obj)
        {
            if (obj == null)
                return 0;
            if (!objectPool.TryGetValue(obj, out uint res))
                throw new KeyNotFoundException();
            return res;
        }

        public void WriteUndertaleObject<T>(T obj) where T : UndertaleObject, new()
        {
            try
            {
                // This isn't a major issue, and this is a performance waster
                //if (objectPool.ContainsKey(obj))
                //    throw new IOException("Writing object twice");
                uint objectAddr = Position;
                if (obj.GetType() == typeof(UndertaleString))
                {
                    if (pendingStringWrites.ContainsKey(obj))
                    {
                        foreach (uint pointerAddr in pendingStringWrites[obj])
                            intsToWriteParallel.Add(new ValueTuple<uint, uint>(pointerAddr, objectAddr + 4));

                        pendingStringWrites.Remove(obj);
                    }
                }
                else
                    objectPool.Add(obj, objectAddr); // strings come later in the file, so no need to add them to the pool
                
                obj.Serialize(this);

                if (pendingWrites.ContainsKey(obj))
                {
                    foreach (uint pointerAddr in pendingWrites[obj])
                        intsToWriteParallel.Add(new ValueTuple<uint, uint>(pointerAddr, objectAddr));

                    pendingWrites.Remove(obj);
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
                Write(0x00000000u);
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
                Write(0x0000000u);
                return;
            }

            if (!pendingStringWrites.ContainsKey(obj))
                pendingStringWrites.Add(obj, new List<uint>());
            pendingStringWrites[obj].Add(Position);
            Write(0xDEADC0DEu);
        }

        public void ThrowIfUnwrittenObjects()
        {
            if ((pendingWrites.Count + pendingStringWrites.Count) != 0)
            {
                var unwrittenObjects = pendingWrites.Concat(pendingStringWrites);
                throw new IOException("Found pointer targets that were never written:\n"
                                      + String.Join("\n", unwrittenObjects.Take(10).Select((x) => x.Key + " at " + String.Join(", ", x.Value.Select((y) => "0x" + y.ToString("X8")))))
                                      + (unwrittenObjects.Count() > 10
                                         ? "\n(and more, " + unwrittenObjects.Count() + " total)"
                                         : ""));
            }
        }

        public void Align(int alignment, byte paddingbyte = 0x00)
        {
            while ((Position & (alignment - 1)) != paddingbyte)
            {
                Write(paddingbyte);
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
    }

    public static class UndertaleIO
    {
        public static bool IsDictionaryCleared { get; set; } = true;

        public static UndertaleData Read(Stream stream, UndertaleReader.WarningHandlerDelegate warningHandler = null,
                                                        UndertaleReader.MessageHandlerDelegate messageHandler = null)
        {
            UndertaleReader reader = new UndertaleReader(stream, warningHandler, messageHandler);
            var data = reader.ReadUndertaleData();
            reader.ThrowIfUnreadObjects();
            return data;
        }

        public static void Write(Stream stream, UndertaleData data, UndertaleWriter.MessageHandlerDelegate messageHandler = null)
        {
            UndertaleWriter writer = new UndertaleWriter(stream, messageHandler);
            writer.WriteUndertaleData(data);
            writer.ThrowIfUnwrittenObjects();
            writer.Flush(data);
        }

        public static Dictionary<uint, UndertaleObject> GenerateOffsetMap(Stream stream)
        {
            UndertaleReader reader = new UndertaleReader(stream);
            reader.ReadUndertaleData();
            reader.ThrowIfUnreadObjects();
            return reader.GetOffsetMap();
        }
    }
}
