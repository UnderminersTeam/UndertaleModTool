using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib
{
    public interface UndertaleObject
    {
        void Serialize(UndertaleWriter writer);
        void Unserialize(UndertaleReader reader);
    }

    public interface UndertaleObjectWithBlobs : UndertaleObject
    {
        void SerializeBlobBefore(UndertaleWriter writer);
    }

    public interface PaddedObject // TODO: use this everywhere
    {
        void SerializePadding(UndertaleWriter writer);
        void UnserializePadding(UndertaleReader reader);
    }
}
