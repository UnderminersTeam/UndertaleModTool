using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModLib
{
    public abstract class UndertaleChunk
    {
        public abstract string Name { get; }
        public uint Length { get; internal set; }

        internal abstract void SerializeChunk(UndertaleWriter writer);
        internal abstract void UnserializeChunk(UndertaleReader reader);
        internal abstract uint UnserializeObjectCount(UndertaleReader reader);

        public void Serialize(UndertaleWriter writer)
        {
            try
            {
                Util.DebugUtil.Assert(Name != null);
                writer.Write(Name.ToCharArray());
                var lenWriter = writer.WriteLengthHere();

                writer.SubmitMessage("Writing chunk " + Name);
                lenWriter.FromHere();
                SerializeChunk(writer);
                
                if (Name != "FORM" && Name != writer.LastChunkName)
                {
                    UndertaleGeneralInfo generalInfo = Name == "GEN8" ? ((UndertaleChunkGEN8)this).Object : writer.undertaleData?.GeneralInfo;
                    // These versions introduced new padding
                    // all chunks now start on 16-byte boundaries
                    // (but the padding is included with length of previous chunk)
                    // TODO: what about the debug data??
                    if (generalInfo != null && (generalInfo.Major >= 2 || (generalInfo.Major == 1 && generalInfo.Build >= 9999)))
                    {
                        int e = writer.undertaleData.PaddingAlignException;
                        uint pad = (e == -1 ? 16 : (uint)e);
                        while (writer.Position % pad != 0)
                        {
                            writer.Write((byte)0);
                        }
                    }
                }

                Length = lenWriter.ToHere();
            }
            catch (UndertaleSerializationException e)
            {
                throw new UndertaleSerializationException(e.Message + " in chunk " + Name, e);
            }
            catch (Exception e)
            {
                throw new UndertaleSerializationException(e.Message + "\nat " + writer.Position.ToString("X8") + " while reading chunk " + Name, e);
            }
        }

        public static UndertaleChunk Unserialize(UndertaleReader reader)
        {
            string name = "(unknown)";
            try
            {
                name = reader.ReadChars(4);
                uint length = reader.ReadUInt32();

                // TODO: I can't think of a cleaner way to do this...
                Type type = Type.GetType(typeof(UndertaleChunk).FullName + name);
                if (type == null)
                {
                    throw new IOException("Unknown chunk " + name + "!!!");
                    /*Debug.WriteLine("Unknown chunk " + name + "!!!");
                    reader.Position = reader.Position + length;
                    return null;*/
                }

                UndertaleChunk chunk = (UndertaleChunk)Activator.CreateInstance(type);
                Util.DebugUtil.Assert(chunk.Name == name,
                                      $"Chunk name mismatch: expected \"{name}\", got \"{chunk.Name}\".");
                chunk.Length = length;

                reader.SubmitMessage("Reading chunk " + chunk.Name);
                var lenReader = reader.EnsureLengthFromHere(chunk.Length);
                chunk.UnserializeChunk(reader);

                if (name != "FORM" && name != reader.LastChunkName)
                {
                    UndertaleGeneralInfo generalInfo = name == "GEN8" ? ((UndertaleChunkGEN8)chunk).Object : reader.undertaleData.GeneralInfo;
                    // These versions introduced new padding
                    // all chunks now start on 16-byte boundaries
                    // (but the padding is included with length of previous chunk)
                    if (generalInfo.Major >= 2 || (generalInfo.Major == 1 && generalInfo.Build >= 9999))
                    {
                        int e = reader.undertaleData.PaddingAlignException;
                        uint pad = (e == -1 ? 16 : (uint)e);
                        while (reader.Position % pad != 0)
                        {
                            if (reader.ReadByte() != 0)
                            {
                                reader.Position -= 1;
                                if (reader.Position % 4 == 0)
                                    reader.undertaleData.PaddingAlignException = 4;
                                else
                                    reader.undertaleData.PaddingAlignException = 1;
                                break;
                            }
                        }
                    }
                }

                lenReader.ToHere();

                return chunk;
            }
            catch (UndertaleSerializationException e)
            {
                throw new UndertaleSerializationException(e.Message + " in chunk " + name, e);
            }
            catch (Exception e)
            {
                throw new UndertaleSerializationException(e.Message + "\nat " + reader.Position.ToString("X8") + " while reading chunk " + name, e);
            }
        }
        public static uint CountChunkChildObjects(UndertaleReader reader)
        {
            string name = "(unknown)";
            try
            {
                name = reader.ReadChars(4);
                uint length = reader.ReadUInt32();

                Type type = Type.GetType(typeof(UndertaleChunk).FullName + name);
                if (type == null)
                    throw new IOException("Unknown chunk " + name + "!!!");

                UndertaleChunk chunk = (UndertaleChunk)Activator.CreateInstance(type);
                Util.DebugUtil.Assert(chunk.Name == name,
                                      $"Chunk name mismatch: expected \"{name}\", got \"{chunk.Name}\".");
                chunk.Length = length;

                uint chunkStart = reader.Position;

                reader.SubmitMessage("Counting objects of chunk " + chunk.Name);
                uint count = chunk.UnserializeObjectCount(reader);

                reader.Position = chunkStart + chunk.Length;

                return count;
            }
            catch (UndertaleSerializationException e)
            {
                throw new UndertaleSerializationException(e.Message + " in chunk " + name, e);
            }
            catch (Exception e)
            {
                throw new UndertaleSerializationException(e.Message + "\nat " + reader.Position.ToString("X8") + " while counting objects of chunk " + name, e);
            }
        }
    }

    public interface IUndertaleSingleChunk
    {
        UndertaleObject GetObject();
    }
    public interface IUndertaleListChunk
    {
        IList GetList();
        void GenerateIndexDict();
        void ClearIndexDict();
    }
    public interface IUndertaleSimpleListChunk
    {
        IList GetList();
    }


    public abstract class UndertaleSingleChunk<T> : UndertaleChunk, IUndertaleSingleChunk where T : UndertaleObject, new()
    {
        public T Object;

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            writer.WriteUndertaleObject(Object);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            Object = reader.ReadUndertaleObject<T>();
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            uint count = 1;

            count += reader.GetChildObjectCount<T>();

            return count;
        }

        public UndertaleObject GetObject() => Object;

        public override string ToString()
        {
            return Object.ToString();
        }
    }

    public abstract class UndertaleListChunk<T> : UndertaleChunk, IUndertaleListChunk where T : UndertaleObject, new()
    {
        public UndertalePointerList<T> List = new UndertalePointerList<T>();
        public Dictionary<T, int> IndexDict;

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            List.Serialize(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            List.Unserialize(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            return UndertalePointerList<T>.UnserializeChildObjectCount(reader);
        }

        public IList GetList() => List;
        public void GenerateIndexDict()
        {
            if (IndexDict is not null)
                return;

            IndexDict = new();
            for (int i = 0; i < List.Count; i++)
                IndexDict[List[i]] = i;
        }
        public void ClearIndexDict()
        {
            IndexDict.Clear();
            IndexDict = null;
        }
    }

    public abstract class UndertaleAlignUpdatedListChunk<T> : UndertaleListChunk<T> where T : UndertaleObject, new()
    {
        public bool Align = true;
        protected static int Alignment = 4;

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            writer.Write(List.Count);
            uint baseAddr = writer.Position;
            for (int i = 0; i < List.Count; i++)
                writer.Write(0);
            for (int i = 0; i < List.Count; i++)
            {
                if (Align)
                {
                    while (writer.Position % Alignment != 0)
                        writer.Write((byte)0);
                }
                uint returnTo = writer.Position;
                writer.Position = baseAddr + ((uint)i * 4);
                writer.Write(returnTo);
                writer.Position = returnTo;
                writer.WriteUndertaleObject(List[i]);
            }
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            uint count = reader.ReadUInt32();
            List.SetCapacity(count);

            for (int i = 0; i < count; i++)
                Align &= (reader.ReadUInt32() % Alignment == 0);
            for (int i = 0; i < count; i++)
            {
                if (Align)
                {
                    while (reader.Position % Alignment != 0)
                        if (reader.ReadByte() != 0)
                            throw new IOException("AlignUpdatedListChunk padding error");
                }
                List.InternalAdd(reader.ReadUndertaleObject<T>());
            }
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            uint count = reader.ReadUInt32();
            if (count == 0)
                return 0;

            Type t = typeof(T);
            if (t != typeof(UndertaleBackground) && t != typeof(UndertaleString))
                throw new InvalidOperationException(
                    "\"UndertaleAlignUpdatedListChunk<T>\" supports the count unserialization only for backgrounds and strings.");

            return count;
        }
    }

    public abstract class UndertaleSimpleListChunk<T> : UndertaleChunk, IUndertaleSimpleListChunk where T : UndertaleObject, new()
    {
        public UndertaleSimpleList<T> List = new UndertaleSimpleList<T>();

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            List.Serialize(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            List.Unserialize(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            return reader.ReadUInt32();
        }

        public IList GetList() => List;
    }

    public abstract class UndertaleEmptyChunk : UndertaleChunk
    {
        internal override void SerializeChunk(UndertaleWriter writer)
        {
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader) => 0;
    }
}
