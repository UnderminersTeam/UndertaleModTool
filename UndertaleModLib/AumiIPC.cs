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
        /// <summary>
        /// A numerical value (1 to 4 for the latest AUMI implementation).
        /// </summary>
        /// <remarks>
        ///  The following table describes and explains the numerical values:
        /// <list type="table">
        ///     <listheader>
        ///         <term>number</term>
        ///         <description>Explanation</description>
        ///     </listheader>
        ///     <item>
        ///         <term>1</term>
        ///         <description>Test Communication - Always returns a string that's 128 characters long.</description>
        ///     </item>
        ///     <item>
        ///         <term>2</term>
        ///         <description>Get Function By Index - Returns information about a function at a specified index in the runner.</description>
        ///     </item>
        ///     <item>
        ///         <term>3</term>
        ///         <description>Get Function By Name - Returns information about a function with a specified name.</description>
        ///     </item>
        ///     <item>
        ///         <term>4</term>
        ///         <description>Execute Code - Executes precompiled bytecode in the global context.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        public short FuncID;

        /// <summary>
        /// A 512 byte buffer containing information which accompanies <see cref="FuncID"/>.
        /// </summary>
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
        /// <summary>
        /// A numerical value from 0 to n, where 0 means success.
        /// </summary>
        /// <remarks>Anything other than 0 means failure, where the number specifies the actual reason.</remarks>
        public int AUMIResult; // Always contains a value.

        /// <summary>
        /// A 128 byte buffer, might not always be filled in.
        /// </summary>
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
