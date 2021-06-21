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
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] Buffer;

        public byte[] RawBytes()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(this.FuncID);

            if (this.Buffer != null)
                writer.Write(this.Buffer, 0, 512);

            return stream.ToArray();
        }
    };

    public struct IpcReply_t
    {
        public int AUMIResult; // Always contains a value.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] Buffer;

        public static IpcReply_t FromBytes(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            var s = default(IpcReply_t);

            s.AUMIResult = reader.ReadInt32();
            s.Buffer = reader.ReadBytes(128);

            return s;
        }
    };
}
