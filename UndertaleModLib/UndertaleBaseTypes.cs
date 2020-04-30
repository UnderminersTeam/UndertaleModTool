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
        void Serialize(UndertaleWriter writer);
        void Unserialize(UndertaleReader reader);
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
        void SerializePadding(UndertaleWriter writer);
        void UnserializePadding(UndertaleReader reader);
    }

    public interface PrePaddedObject
    {
        void SerializePrePadding(UndertaleWriter writer);
        void UnserializePrePadding(UndertaleReader reader);
    }
    
    public enum ResourceType : int
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
        bool SearchMatches(string filter);
    }
}
