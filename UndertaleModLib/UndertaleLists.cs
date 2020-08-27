using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModLib
{
    public class UndertaleSimpleList<T> : ObservableCollection<T>, UndertaleObject where T : UndertaleObject, new()
    {
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((uint)Count);
            for (int i = 0; i < Count; i++)
            {
                try
                {
                    writer.WriteUndertaleObject<T>(this[i]);
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile writing item " + (i + 1) + " of " + Count + " in a list of " + typeof(T).FullName, e);
                }
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            uint count = reader.ReadUInt32();
            Clear();
            for (uint i = 0; i < count; i++)
            {
                try
                {
                    Add(reader.ReadUndertaleObject<T>());
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile reading item " + (i + 1) + " of " + count + " in a list of " + typeof(T).FullName, e);
                }
            }
        }
    }

    public class UndertaleSimpleListString : ObservableCollection<UndertaleString>, UndertaleObject
    {
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((uint)Count);
            for (int i = 0; i < Count; i++)
            {
                try
                {
                    writer.WriteUndertaleString(this[i]);
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile writing item " + (i + 1) + " of " + Count + " in a string-list", e);
                }
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            uint count = reader.ReadUInt32();
            Clear();
            for (uint i = 0; i < count; i++)
            {
                try
                {
                    Add(reader.ReadUndertaleString());
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile reading item " + (i + 1) + " of " + count + " in a string-list", e);
                }
            }
        }
    }

    public class UndertaleSimpleListShort<T> : ObservableCollection<T>, UndertaleObject where T : UndertaleObject, new()
    {
        public UndertaleSimpleListShort()
        {
            base.CollectionChanged += EnsureShortCount;
        }

        private void EnsureShortCount(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count > Int16.MaxValue)
                throw new InvalidOperationException("Count of short SimpleList exceeds maximum number allowed.");
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((ushort)Count);
            for (int i = 0; i < Count; i++)
            {
                try
                {
                    writer.WriteUndertaleObject<T>(this[i]);
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile writing item " + (i + 1) + " of " + Count + " in a list of " + typeof(T).FullName, e);
                }
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            ushort count = reader.ReadUInt16();
            Clear();
            for (ushort i = 0; i < count; i++)
            {
                try
                {
                    Add(reader.ReadUndertaleObject<T>());
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile reading item " + (i + 1) + " of " + count + " in a list of " + typeof(T).FullName, e);
                }
            }
        }
    }

    public class UndertalePointerList<T> : ObservableCollection<T>, UndertaleObject where T : UndertaleObject, new()
    {
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((uint)Count);
            foreach (T obj in this)
                writer.WriteUndertaleObjectPointer<T>(obj);
            for (int i = 0; i < Count; i++)
            {
                try
                {
                    (this[i] as UndertaleObjectWithBlobs)?.SerializeBlobBefore(writer);
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile writing blob for item " + (i + 1) + " of " + Count + " in a list of " + typeof(T).FullName, e);
                }
            }
            for (int i = 0; i < Count; i++)
            {
                try
                {
                    (this[i] as PrePaddedObject)?.SerializePrePadding(writer);

                    writer.WriteUndertaleObject<T>(this[i]);

                    // The last object does NOT get padding (TODO: at least in AUDO)
                    if (i != Count - 1)
                        (this[i] as PaddedObject)?.SerializePadding(writer);
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile writing item " + (i + 1) + " of " + Count + " in a list of " + typeof(T).FullName, e);
                }
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            uint count = reader.ReadUInt32();
            Clear();
            for (uint i = 0; i < count; i++)
            {
                try
                {
                    Add(reader.ReadUndertaleObjectPointer<T>());
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile reading pointer to item " + (i + 1) + " of " + count + " in a list of " + typeof(T).FullName, e);
                }
            }
            if (Count > 0 && reader.Position != reader.GetAddressForUndertaleObject(this[0]))
            {
                int skip = (int)reader.GetAddressForUndertaleObject(this[0]) - (int)reader.Position;
                if (skip > 0)
                {
                    //Console.WriteLine("Skip " + skip + " bytes of blobs");
                    reader.Position = reader.Position + (uint)skip;
                }
                else
                    throw new IOException("First list item starts inside the pointer list?!?!");
            }
            for (uint i = 0; i < count; i++)
            {
                try
                {
                    (this[(int)i] as PrePaddedObject)?.UnserializePrePadding(reader);
                    reader.ReadUndertaleObject(this[(int)i]);
                    if (i != count - 1)
                        (this[(int)i] as PaddedObject)?.UnserializePadding(reader);
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile reading item " + (i + 1) + " of " + count + " in a list of " + typeof(T).FullName, e);
                }
            }
        }
    }

    public class UndertalePointerListLenCheck<T> : UndertalePointerList<T>, UndertaleObjectEndPos where T : UndertaleObjectLenCheck, new()
    {
        public void Unserialize(UndertaleReader reader, uint endPosition)
        {
            uint count = reader.ReadUInt32();
            Clear();
            List<uint> pointers = new List<uint>();
            for (uint i = 0; i < count; i++)
            {
                try
                {
                    uint ptr = reader.ReadUInt32();
                    pointers.Add(ptr);
                    Add(reader.GetUndertaleObjectAtAddress<T>(ptr));
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile reading pointer to item " + (i + 1) + " of " + count + " in a list of " + typeof(T).FullName, e);
                }
            }
            if (Count > 0 && reader.Position != reader.GetAddressForUndertaleObject(this[0]))
            {
                int skip = (int)reader.GetAddressForUndertaleObject(this[0]) - (int)reader.Position;
                if (skip > 0)
                {
                    //Console.WriteLine("Skip " + skip + " bytes of blobs");
                    reader.Position = reader.Position + (uint)skip;
                }
                else
                    throw new IOException("First list item starts inside the pointer list?!?!");
            }
            for (uint i = 0; i < count; i++)
            {
                try
                {
                    (this[(int)i] as PrePaddedObject)?.UnserializePrePadding(reader);
                    if ((i + 1) < count)
                        reader.ReadUndertaleObject(this[(int)i], (int)(pointers[(int)i + 1] - reader.Position));
                    else
                        reader.ReadUndertaleObject(this[(int)i], (int)(endPosition - reader.Position));
                    if (i != count - 1)
                        (this[(int)i] as PaddedObject)?.UnserializePadding(reader);
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile reading item " + (i + 1) + " of " + count + " in a list of " + typeof(T).FullName, e);
                }
            }
        }
    }

    public class UndertaleSimpleResourcesList<T, ChunkT> : UndertaleSimpleList<UndertaleResourceById<T, ChunkT>> where T : UndertaleResource, new() where ChunkT : UndertaleListChunk<T>
    {
        // TODO: Allow direct access to Resource elements?
    }
}
