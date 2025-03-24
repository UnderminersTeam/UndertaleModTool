using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using UndertaleModLib.Models;

namespace UndertaleModLib
{
    /// <summary>
    /// Basic observable list type, usable for data models to be presented in interfaces.
    /// </summary>
    public class UndertaleObservableList<T> : ObservableCollection<T>
    {
        // Reference to the private internal list inside Collection<T>
        private readonly List<T> internalList;

        // Internal list field (name is guaranteed not to change due to the underlying type being serializable)
        private static readonly FieldInfo itemsField = typeof(Collection<T>).GetField("items", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Creates a new observable list, which is empty and has default capacity.
        /// </summary>
        public UndertaleObservableList()
        {
            // Get ahold of internal list
            internalList = (List<T>)itemsField.GetValue(this);
        }

        /// <summary>
        /// Creates a new observable list, which is empty and has the specified initial capacity.
        /// </summary>
        public UndertaleObservableList(int capacity)
        {
            // Get ahold of internal list
            internalList = (List<T>)itemsField.GetValue(this);

            // Set capacity directly
            internalList.Capacity = capacity;
        }

        /// <summary>
        /// Sets the capacity of the internal list of this collection.
        /// </summary>
        public void SetCapacity(int capacity)
        {
            internalList.Capacity = capacity;
        }

        /// <summary>
        /// Sets the capacity of the internal list of this collection.
        /// </summary>
        public void SetCapacity(uint capacity) => SetCapacity((int)capacity);

        /// <summary>
        /// Adds an item to the internal list of this collection (that is, without notifying any observers).
        /// </summary>
        /// <param name="item">Item to add.</param>
        public void InternalAdd(T item)
        {
            internalList.Add(item);
        }
    }

    /// <summary>
    /// Simple list object, serialized as a 32-bit count followed immediately by all objects in the list.
    /// </summary>
    public class UndertaleSimpleList<T> : UndertaleObservableList<T>, UndertaleObject where T : UndertaleObject, new()
    {
        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            // Write list count
            int count = Count;
            writer.Write((uint)count);

            // Write actual objects
            int i = 0;
            try
            {
                for (; i < count; i++)
                {
                    writer.WriteUndertaleObject(this[i]);
                }
            }
            catch (UndertaleSerializationException e)
            {
                throw new UndertaleSerializationException($"{e.Message}\nwhile writing item {i + 1} of {count} in a list of {typeof(T).FullName}", e);
            }
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            // Read count and initialize list with that capacity
            uint count = reader.ReadUInt32();
            Clear();
            SetCapacity(count);

            // Read actual objects
            uint i = 0;
            try
            {
                for (; i < count; i++)
                {
                    InternalAdd(reader.ReadUndertaleObject<T>());
                }
            }
            catch (UndertaleSerializationException e)
            {
                throw new UndertaleSerializationException($"{e.Message}\nwhile reading item {i + 1} of {count} in a list of {typeof(T).FullName}", e);
            }
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            // Read base object count
            uint count = reader.ReadUInt32();
            if (count == 0)
            {
                // Short-circuit if there's no objects
                return 0;
            }

            // If the objects are resource refs, the size is known as 4 bytes
            if (typeof(T).IsAssignableTo(typeof(UndertaleResourceRef)))
            {
                // UndertaleResourceById<T, ChunkT> = 4 bytes
                reader.Position += count * 4;

                return count;
            }

            // If objects have a static child object size/count, simply multiply to determine the number of total objects
            if (typeof(T).IsAssignableTo(typeof(IStaticChildObjectsSize)))
            {
                uint subSize = reader.GetStaticChildObjectsSize(typeof(T));
                uint subCount = 0;

                if (typeof(T).IsAssignableTo(typeof(IStaticChildObjCount)))
                {
                    subCount = reader.GetStaticChildCount(typeof(T));
                }

                reader.Position += count * subSize;

                return count + (count * subCount);
            }

            // Determine object counts recursively for all objects
            Func<UndertaleReader, uint> unserializeFunc = reader.GetUnserializeCountFunc(typeof(T));
            uint totalCount = 0;
            uint i = 0;
            try
            {
                for (; i < count; i++)
                {
                    totalCount += 1 + unserializeFunc(reader);
                }
            }
            catch (UndertaleSerializationException e)
            {
                throw new UndertaleSerializationException($"{e.Message}\nwhile reading child object count of item {i + 1} of {count} in a list of {typeof(T).FullName}", e);
            }

            return totalCount;
        }
    }

    /// <summary>
    /// Simple list object, specifically for strings, serialized as a count followed immediately by all strings in the list.
    /// </summary>
    public class UndertaleSimpleListString : UndertaleObservableList<UndertaleString>, UndertaleObject
    {
        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            // Write count
            int count = Count;
            writer.Write((uint)count);

            // Write all of the strings
            int i = 0;
            try
            {
                for (; i < count; i++)
                {
                    writer.WriteUndertaleString(this[i]);
                }
            }
            catch (UndertaleSerializationException e)
            {
                throw new UndertaleSerializationException($"{e.Message}\nwhile writing item {i + 1} of {count} in a string-list", e);
            }
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            // Read count and initialize list with that capacity
            uint count = reader.ReadUInt32();
            Clear();
            SetCapacity(count);

            // Read all of the strings
            uint i = 0;
            try
            {
                for (; i < count; i++)
                {
                    InternalAdd(reader.ReadUndertaleString());
                }
            }
            catch (UndertaleSerializationException e)
            {
                throw new UndertaleSerializationException($"{e.Message}\nwhile reading item {i + 1} of {count} in a string-list", e);
            }
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            uint count = reader.ReadUInt32();
            reader.Position += count * 4;
            return 0;
        }
    }

    /// <summary>
    /// Simple list object, serialized as a 16-bit count followed immediately by all objects in the list.
    /// </summary>
    public class UndertaleSimpleListShort<T> : UndertaleObservableList<T>, UndertaleObject where T : UndertaleObject, new()
    {
        private void EnsureShortCount()
        {
            if (Count > Int16.MaxValue)
                throw new InvalidOperationException("Count of short SimpleList exceeds maximum number allowed.");
        }

        /// <inheritdoc cref="Collection{T}.Add(T)"/>
        public new void Add(T item)
        {
            base.Add(item);
            EnsureShortCount();
        }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            // Write list count
            int count = Count;
            writer.Write((ushort)count);

            // Write actual objects
            int i = 0;
            try
            {
                for (; i < count; i++)
                {
                    writer.WriteUndertaleObject(this[i]);
                }
            }
            catch (UndertaleSerializationException e)
            {
                throw new UndertaleSerializationException($"{e.Message}\nwhile writing item {i + 1} of {count} in a list of {typeof(T).FullName}", e);
            }
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            // Read count and initialize list with that capacity
            ushort count = reader.ReadUInt16();
            Clear();
            SetCapacity(count);

            // Read actual objects
            ushort i = 0;
            try
            {
                for (; i < count; i++)
                {
                    InternalAdd(reader.ReadUndertaleObject<T>());
                }
            }
            catch (UndertaleSerializationException e)
            {
                throw new UndertaleSerializationException($"{e.Message}\nwhile reading item {i + 1} of {count} in a list of {typeof(T).FullName}", e);
            }
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            // Read base object count
            ushort count = reader.ReadUInt16();
            if (count == 0)
            {
                // Short-circuit if there's no objects
                return 0;
            }

            // If objects have a static child object size/count, simply multiply to determine the number of total objects
            if (typeof(T).IsAssignableTo(typeof(IStaticChildObjectsSize)))
            {
                uint subSize = reader.GetStaticChildObjectsSize(typeof(T));
                uint subCount = 0;

                if (typeof(T).IsAssignableTo(typeof(IStaticChildObjCount)))
                {
                    subCount = reader.GetStaticChildCount(typeof(T));
                }

                reader.Position += count * subSize;

                return count + (count * subCount);
            }

            // Determine object counts recursively for all objects
            Func<UndertaleReader, uint> unserializeFunc = reader.GetUnserializeCountFunc(typeof(T));
            uint totalCount = 0;
            uint i = 0;
            try
            {
                for (; i < count; i++)
                {
                    totalCount += 1 + unserializeFunc(reader);
                }
            }
            catch (UndertaleSerializationException e)
            {
                throw new UndertaleSerializationException($"{e.Message}\nwhile reading child object count of item {i + 1} of {count} in a list of {typeof(T).FullName}", e);
            }

            return totalCount;
        }
    }

    public class UndertalePointerList<T> : UndertaleObservableList<T>, UndertaleObject where T : UndertaleObject, new()
    {
        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            // Write count
            int count = Count;
            writer.Write((uint)count);

            // Write all pointers
            foreach (T obj in this)
            {
                writer.WriteUndertaleObjectPointer<T>(obj);
            }

            // Write blobs, if necessary for the given type
            if (typeof(T).IsAssignableTo(typeof(UndertaleObjectWithBlobs)))
            {
                int i = 0;
                try
                {
                    for (; i < count; i++)
                    {
                        ((UndertaleObjectWithBlobs)this[i]).SerializeBlobBefore(writer);
                    }
                }
                catch (UndertaleSerializationException e)
                {
                    throw new UndertaleSerializationException($"{e.Message}\nwhile writing blob for item {i + 1} of {count} in a list of {typeof(T).FullName}", e);
                }
            }

            // Write actual objects
            int j = 0;
            try
            {
                for (; j < count; j++)
                {
                    T obj = this[j];

                    // Serialize pre-padding, if this is a type that requires it
                    if (typeof(T).IsAssignableTo(typeof(PrePaddedObject)))
                    {
                        ((PrePaddedObject)obj).SerializePrePadding(writer);
                    }

                    // Write object
                    writer.WriteUndertaleObject(obj);

                    // Serialize padding, if this is a type that requires it
                    if (typeof(T).IsAssignableTo(typeof(PaddedObject)))
                    {
                        // The last object does NOT get padding (TODO: at least in AUDO)
                        if (j != count - 1)
                        {
                            ((PaddedObject)obj).SerializePadding(writer);
                        }
                    }
                }
            }
            catch (UndertaleSerializationException e)
            {
                throw new UndertaleSerializationException($"{e.Message}\nwhile writing item {j + 1} of {count} in a list of {typeof(T).FullName}", e);
            }
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            // Read count and initialize list with that capacity
            uint count = reader.ReadUInt32();
            Clear();
            SetCapacity(count);

            // Read in all object pointers
            uint i = 0;
            try
            {
                for (; i < count; i++)
                {
                    InternalAdd(reader.ReadUndertaleObjectPointer<T>());
                }
            }
            catch (UndertaleSerializationException e)
            {
                throw new UndertaleSerializationException($"{e.Message}\nwhile reading pointer to item {i + 1} of {count} in a list of {typeof(T).FullName}", e);
            }

            // Advance to start of first object (particularly, if blobs exist)
            if (count > 0)
            {
                uint pos = reader.GetAddressForUndertaleObject(this[0]);
                if (reader.AbsPosition != pos)
                {
                    long skip = pos - reader.AbsPosition;
                    if (skip > 0)
                    {
                        reader.AbsPosition += skip;
                    }
                    else
                    {
                        throw new IOException("First list item starts inside pointer list");
                    }
                }
            }

            // Read in actual objects
            uint j = 0;
            try
            {
                for (; j < count; j++)
                {
                    T obj = this[(int)j];
                    
                    // Unserialize pre-padding, if this is a type that requires it
                    if (typeof(T).IsAssignableTo(typeof(PrePaddedObject)))
                    {
                        ((PrePaddedObject)obj).UnserializePrePadding(reader);
                    }

                    // Read object
                    reader.ReadUndertaleObject(obj);

                    // Unserialize padding, if this is a type that requires it
                    if (typeof(T).IsAssignableTo(typeof(PaddedObject)))
                    {
                        // The last object does NOT get padding (TODO: at least in AUDO)
                        if (j != count - 1)
                        {
                            ((PaddedObject)obj).UnserializePadding(reader);
                        }
                    }
                }
            }
            catch (UndertaleSerializationException e)
            {
                throw new UndertaleSerializationException($"{e.Message}\nwhile reading item {j + 1} of {count} in a list of {typeof(T).FullName}", e);
            }
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            // Read base object count
            uint count = reader.ReadUInt32();
            if (count == 0)
            {
                // Short-circuit if there's no objects
                return 0;
            }

            // If objects have a static child object size/count, simply multiply to determine the number of total objects
            if (typeof(T).IsAssignableTo(typeof(IStaticChildObjectsSize)))
            {
                uint subSize = reader.GetStaticChildObjectsSize(typeof(T));
                uint subCount = 0;

                if (typeof(T).IsAssignableTo(typeof(IStaticChildObjCount)))
                {
                    subCount = reader.GetStaticChildCount(typeof(T));
                }

                reader.Position += (count * 4) + (count * subSize);

                return count + (count * subCount);
            }

            // Read pointers of all objects
            uint[] pointers = reader.ListPtrsPool.Rent((int)count);
            for (uint i = 0; i < count; i++)
            {
                pointers[i] = reader.ReadUInt32();
            }

            // Advance to start of first object (particularly, if blobs exist)
            uint pos = pointers[0];
            if (reader.AbsPosition != pos)
            {
                long skip = pos - reader.AbsPosition;
                if (skip > 0)
                {
                    reader.AbsPosition += skip;
                }
                else
                {
                    throw new IOException("First list item starts inside pointer list");
                }
            }

            // Determine object counts recursively for all objects
            var unserializeFunc = reader.GetUnserializeCountFunc(typeof(T));
            uint totalCount = 0;
            uint j = 0;
            try
            {
                for (; j < count; j++)
                {
                    reader.AbsPosition = pointers[j];
                    totalCount += 1 + unserializeFunc(reader);
                }
            }
            catch (UndertaleSerializationException e)
            {
                throw new UndertaleSerializationException($"{e.Message}\nwhile reading child object count of item {j + 1} of {count} in a list of {typeof(T).FullName}", e);
            }
            finally
            {
                reader.ListPtrsPool.Return(pointers);
            }

            return totalCount;
        }
    }

    /// <summary>
    /// Shorthand for <see cref="UndertaleSimpleList{T}"/> containing <see cref="UndertaleResourceById{T, ChunkT}"/>.
    /// </summary>
    public class UndertaleSimpleResourcesList<T, ChunkT> : UndertaleSimpleList<UndertaleResourceById<T, ChunkT>> where T : UndertaleResource, new() where ChunkT : UndertaleListChunk<T>
    {
        // TODO: Allow direct access to Resource elements?
    }
}
