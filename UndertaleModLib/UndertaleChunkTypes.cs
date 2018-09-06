using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Debug.Assert(Name != null);
            writer.Write(Name.ToCharArray());
            var lenWriter = writer.WriteLengthHere();

            Debug.WriteLine("Writing chunk " + Name);
            lenWriter.FromHere();
            SerializeChunk(writer);
            Length = lenWriter.ToHere();
        }

        public static UndertaleChunk Unserialize(UndertaleReader reader)
        {
            string name = new string(reader.ReadChars(4));
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
            lenReader.ToHere();

            return chunk;
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
