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
