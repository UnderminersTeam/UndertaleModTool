using System;
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

        public void Serialize(UndertaleWriter writer)
        {
            try
            {
                Debug.Assert(Name != null);
                writer.Write(Name.ToCharArray());
                var lenWriter = writer.WriteLengthHere();

                Debug.WriteLine("Writing chunk " + Name);
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
                name = new string(reader.ReadChars(4));
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
                Debug.Assert(chunk.Name == name);
                chunk.Length = length;

                Debug.WriteLine("Reading chunk " + chunk.Name);
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
    }

    public abstract class UndertaleSingleChunk<T> : UndertaleChunk where T : UndertaleObject, new()
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

        public override string ToString()
        {
            return Object.ToString();
        }
    }

    public abstract class UndertaleListChunk<T> : UndertaleChunk where T : UndertaleObject, new()
    {
        public UndertalePointerList<T> List = new UndertalePointerList<T>();

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            List.Serialize(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            List.Unserialize(reader);
        }
    }

    public abstract class UndertaleSimpleListChunk<T> : UndertaleChunk where T : UndertaleObject, new()
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
    }

    public abstract class UndertaleEmptyChunk : UndertaleChunk
    {
        internal override void SerializeChunk(UndertaleWriter writer)
        {
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
        }
    }
}
