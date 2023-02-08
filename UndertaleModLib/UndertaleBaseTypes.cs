using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModLib
{
    public interface UndertaleObject
    {
        /// <summary>
        /// Serializes this <see cref="UndertaleObject"/> into a specified <see cref="UndertaleWriter"/>.
        /// </summary>
        /// <param name="writer">Where to serialize to.</param>
        void Serialize(UndertaleWriter writer);

        /// <summary>
        /// Deserializes this <see cref="UndertaleObject"/> from a specified <see cref="UndertaleReader"/>.
        /// </summary>
        /// <param name="reader">Where to deserialize from.</param>
        void Unserialize(UndertaleReader reader);

        /*
         * As for C# 10, it's impossible to inherit static methods from an interface :(
         * (so this method is for inheriting XML commentary only)
         */
        /// <summary>
        /// Deserializes the total child object count of this object from specified <see cref="UndertaleReader"/>.
        /// </summary>
        /// <param name="reader">Where to deserialize from.</param>
        /// <returns>The object count.</returns>
        static uint UnserializeChildObjectCount(UndertaleReader reader) => 0;
        
    }

    public interface UndertaleObjectLenCheck : UndertaleObject
    {
        void Unserialize(UndertaleReader reader, int length);
    }

    public interface UndertaleObjectEndPos : UndertaleObject
    {
        void Unserialize(UndertaleReader reader, uint endPosition);
    }

    public interface UndertaleObjectWithBlobs
    {
        void SerializeBlobBefore(UndertaleWriter writer);
    }

    public interface PaddedObject // TODO: use this everywhere
    {
        /// <summary>
        /// TODO!
        /// </summary>
        /// <param name="writer">Where to serialize to.</param>
        void SerializePadding(UndertaleWriter writer);

        /// <summary>
        /// TODO!
        /// </summary>
        /// <param name="reader">Where to deserialize from.</param>
        void UnserializePadding(UndertaleReader reader);
    }

    public interface PrePaddedObject
    {

        /// <summary>
        /// TODO!
        /// </summary>
        /// <param name="writer">Where to serialize to.</param>
        void SerializePrePadding(UndertaleWriter writer);

        /// <summary>
        /// TODO!
        /// </summary>
        /// <param name="reader">Where to deserialize from.</param>
        void UnserializePrePadding(UndertaleReader reader);
    }

    public enum ResourceType
    {
        None = -1,
        Object = 0,
        Sprite = 1,
        Sound = 2,
        Room = 3,
        Unknown = 4, // Probably removed
        Path = 5,
        Script = 6,
        Font = 7,
        Timeline = 8,
        Background = 9,
        Shader = 10,

        // GMS2.3+
        Sequence = 11,
        AnimCurve = 12
    }

    public interface UndertaleResource : UndertaleObject
    {
    }

    public interface UndertaleNamedResource : UndertaleResource
    {
        UndertaleString Name { get; set; }
    }

    public interface ISearchable
    {
        /// <summary>
        /// Returns a value indicating whether a specified substring occurs within this object.
        /// </summary>
        /// <param name="filter">The string to seek. Case insensitive.</param>
        /// <returns><see langword="true"/> if <paramref name="filter"/> occurs within this object, or if
        /// <paramref name="filter"/> is the empty string (""); otherwise, false.</returns>
        bool SearchMatches(string filter);
    }

    public interface IStaticChildObjCount
    {
        /// <summary>
        /// The total child object count of the current object type.
        /// Used for the object count unserialization.
        /// </summary>
        public static readonly uint ChildObjectCount = 0;
    }
    public interface IStaticChildObjectsSize
    {
        /// <summary>
        /// The summary child objects size of the current object type.
        /// Used for the object count unserialization.
        /// </summary>
        public static readonly uint ChildObjectsSize = 0;
    }
}
