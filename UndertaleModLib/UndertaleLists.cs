using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using UndertaleModLib.Models;

namespace UndertaleModLib
{
    public abstract class UndertaleListBase<T> : ObservableCollection<T>
    {
        public readonly List<T> internalList;

        public UndertaleListBase()
        {
            try
            {
                FieldInfo itemsField = typeof(Collection<T>)
                                       .GetField("items", BindingFlags.NonPublic | BindingFlags.Instance);
                internalList = (List<T>)itemsField.GetValue(this);

            }
            catch (Exception e)
            {
                throw new UndertaleSerializationException($"{e.Message}\nwhile trying to initialize \"UndertalePointerList<{typeof(T).FullName}>\".");
            }
        }

        /// <inheritdoc />
        public abstract void Serialize(UndertaleWriter writer);

        /// <inheritdoc />
        public abstract void Unserialize(UndertaleReader reader);

        public void SetCapacity(uint capacity)
        {
            try
            {
                internalList.Capacity = (int)capacity;
            }
            catch (Exception e)
            {
                throw new UndertaleSerializationException($"{e.Message}\nwhile trying to \"SetCapacity()\" of \"UndertalePointerList<{typeof(T).FullName}>\".");
            }
        }
    }

    public class UndertaleSimpleList<T> : UndertaleListBase<T>, UndertaleObject where T : UndertaleObject, new()
    {
        /// <inheritdoc />
        public override void Serialize(UndertaleWriter writer)
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

        /// <inheritdoc />
        public override void Unserialize(UndertaleReader reader)
        {
            uint count = reader.ReadUInt32();
            Clear();
            SetCapacity(count);
            for (uint i = 0; i < count; i++)
            {
                try
                {
                    internalList.Add(reader.ReadUndertaleObject<T>());
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile reading item " + (i + 1) + " of " + count + " in a list of " + typeof(T).FullName, e);
                }
            }
        }
    }

    public class UndertaleSimpleListString : UndertaleListBase<UndertaleString>, UndertaleObject
    {
        /// <inheritdoc />
        public override void Serialize(UndertaleWriter writer)
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

        /// <inheritdoc />
        public override void Unserialize(UndertaleReader reader)
        {
            uint count = reader.ReadUInt32();
            Clear();
            SetCapacity(count);
            for (uint i = 0; i < count; i++)
            {
                try
                {
                    internalList.Add(reader.ReadUndertaleString());
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile reading item " + (i + 1) + " of " + count + " in a string-list", e);
                }
            }
        }
    }

    public class UndertaleSimpleListShort<T> : UndertaleListBase<T>, UndertaleObject where T : UndertaleObject, new()
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

        /// <inheritdoc />
        public override void Serialize(UndertaleWriter writer)
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

        /// <inheritdoc />
        public override void Unserialize(UndertaleReader reader)
        {
            ushort count = reader.ReadUInt16();
            Clear();
            SetCapacity(count);
            for (ushort i = 0; i < count; i++)
            {
                try
                {
                    internalList.Add(reader.ReadUndertaleObject<T>());
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile reading item " + (i + 1) + " of " + count + " in a list of " + typeof(T).FullName, e);
                }
            }
        }
    }

    public class UndertalePointerList<T> : UndertaleListBase<T>, UndertaleObject where T : UndertaleObject, new()
    {
        /// <inheritdoc />
        public override void Serialize(UndertaleWriter writer)
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
                    T obj = this[i];

                    (obj as PrePaddedObject)?.SerializePrePadding(writer);

                    writer.WriteUndertaleObject<T>(obj);

                    // The last object does NOT get padding (TODO: at least in AUDO)
                    if (i != Count - 1)
                        (obj as PaddedObject)?.SerializePadding(writer);
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile writing item " + (i + 1) + " of " + Count + " in a list of " + typeof(T).FullName, e);
                }
            }
        }

        /// <inheritdoc />
        public override void Unserialize(UndertaleReader reader)
        {
            uint count = reader.ReadUInt32();
            Clear();
            SetCapacity(count);
            for (uint i = 0; i < count; i++)
            {
                try
                {
                    internalList.Add(reader.ReadUndertaleObjectPointer<T>());
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile reading pointer to item " + (i + 1) + " of " + count + " in a list of " + typeof(T).FullName, e);
                }
            }
            if (Count > 0)
            {
                uint pos = reader.GetAddressForUndertaleObject(this[0]);
                if (reader.Position != pos)
                {
                    uint skip = pos - reader.Position;
                    if (skip > 0)
                    {
                        //Console.WriteLine("Skip " + skip + " bytes of blobs");
                        reader.Position += skip;
                    }
                    else
                        throw new IOException("First list item starts inside the pointer list?!?!");
                }
            }
            for (uint i = 0; i < count; i++)
            {
                try
                {
                    T obj = this[(int)i];

                    (obj as PrePaddedObject)?.UnserializePrePadding(reader);

                    reader.ReadUndertaleObject(obj);

                    if (i != count - 1)
                        (obj as PaddedObject)?.UnserializePadding(reader);
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
        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader, uint endPosition)
        {
            uint count = reader.ReadUInt32();
            Clear();
            SetCapacity(count);
            List<uint> pointers = new((int)count);
            for (uint i = 0; i < count; i++)
            {
                try
                {
                    uint ptr = reader.ReadUInt32();
                    pointers.Add(ptr);
                    internalList.Add(reader.GetUndertaleObjectAtAddress<T>(ptr));
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException(e.Message + "\nwhile reading pointer to item " + (i + 1) + " of " + count + " in a list of " + typeof(T).FullName, e);
                }
            }
            if (Count > 0)
            {
                uint pos = reader.GetAddressForUndertaleObject(this[0]);
                if (reader.Position != pos)
                {
                    uint skip = pos - reader.Position;
                    if (skip > 0)
                    {
                        //Console.WriteLine("Skip " + skip + " bytes of blobs");
                        reader.Position += skip;
                    }
                    else
                        throw new IOException("First list item starts inside the pointer list?!?!");
                }
            }
            for (uint i = 0; i < count; i++)
            {
                try
                {
                    T obj = this[(int)i];

                    (obj as PrePaddedObject)?.UnserializePrePadding(reader);
                    if ((i + 1) < count)
                        reader.ReadUndertaleObject(obj, (int)(pointers[(int)i + 1] - reader.Position));
                    else
                        reader.ReadUndertaleObject(obj, (int)(endPosition - reader.Position));
                    if (i != count - 1)
                        (obj as PaddedObject)?.UnserializePadding(reader);
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
