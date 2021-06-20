using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib
{
    public struct IpcMessage_t
    {
        public short FuncID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 510)]
        public byte[] Buffer;

        public byte[] RawBytes()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(this.FuncID);

            if (this.Buffer != null)
                writer.Write(this.Buffer, 0, 510);

            return stream.ToArray();
        }
    };

    public struct IpcReply_t
    {
        public int AUMIResult; //Always contains a value.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 124)]
        public byte[] Buffer;

        public static IpcReply_t FromBytes(byte[] bytes)
        {
            var reader = new BinaryReader(new MemoryStream(bytes));

            var s = default(IpcReply_t);

            s.AUMIResult = reader.ReadInt32();
            s.Buffer = reader.ReadBytes(124);

            return s;
        }
    };
}
