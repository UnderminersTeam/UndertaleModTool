using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib
{
    public class UndertaleSimpleList<T> : ObservableCollection<T>, UndertaleObject where T : UndertaleObject, new()
    {
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((uint)Count);
            foreach (T obj in this)
                writer.WriteUndertaleObject<T>(obj);
        }

        public void Unserialize(UndertaleReader reader)
        {
            uint count = reader.ReadUInt32();
            Clear();
            for (uint i = 0; i < count; i++)
                Add(reader.ReadUndertaleObject<T>());
        }
    }

    public class UndertalePointerList<T> : ObservableCollection<T>, UndertaleObject where T : UndertaleObject, new()
    {
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((uint)Count);
            foreach (T obj in this)
                writer.WriteUndertaleObjectPointer<T>(obj);
            foreach (T obj in this)
                (obj as UndertaleObjectWithBlobs)?.SerializeBlobBefore(writer);
            foreach (T obj in this)
            {
                writer.WriteUndertaleObject<T>(obj);
                // The last object does NOT get padding (TODO: at least in AUDO)
                if (IndexOf(obj) != Count - 1)
                    (obj as PaddedObject)?.SerializePadding(writer);
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            uint count = reader.ReadUInt32();
            Clear();
            for (uint i = 0; i < count; i++)
                Add(reader.ReadUndertaleObjectPointer<T>());
            if (Count > 0 && reader.Position != reader.GetAddressForUndertaleObject(this[0]))
            {
                int skip = (int)reader.GetAddressForUndertaleObject(this[0]) - (int)reader.Position;
                if (skip > 0)
                {
                    //Console.WriteLine("Skip " + skip + " bytes of blobs");
                    reader.Position = reader.Position + (uint)skip;
                }
                else
                    throw new IOException("Read underflow");
            }
            for (uint i = 0; i < count; i++)
            {
                T obj = reader.ReadUndertaleObject<T>();
                if (!obj.Equals(this[(int)i]))
                    throw new IOException("Something got misaligned...");
                if (i != count - 1)
                    (obj as PaddedObject)?.UnserializePadding(reader);
            }
        }
    }
}
