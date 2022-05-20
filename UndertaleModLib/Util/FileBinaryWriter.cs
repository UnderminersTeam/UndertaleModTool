using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace UndertaleModLib.Util
{
    public class FileBinaryWriter : BinaryWriter
    {
        private readonly Encoding encoding = new UTF8Encoding(false);

        public uint Position
        {
            get => (uint)OutStream.Position;
            set => OutStream.Position = value;
        }

        public Encoding Encoding { get => encoding; }
        public Stream BaseStream => OutStream;

        public FileBinaryWriter(Stream stream, Encoding encoding = null) : base(stream)
        {
            if (encoding is not null)
                this.encoding = encoding;
        }

        public void Write(MemoryStream value)
        {
            value.CopyTo(OutStream);
        }

        public override void Write(char[] value)
        {
            foreach (char c in value)
                OutStream.WriteByte(Convert.ToByte(c));
        }

        public void WriteInt24(int value)
        {
            Span<byte> buffer = stackalloc byte[3];
            buffer[0] = (byte)(value & 0xFF);
            buffer[1] = (byte)((value >> 8) & 0xFF);
            buffer[2] = (byte)((value >> 16) & 0xFF);
            OutStream.Write(buffer);
        }

        public void WriteUInt24(uint value)
        {
            Span<byte> buffer = stackalloc byte[3];
            buffer[0] = (byte)(value & 0xFF);
            buffer[1] = (byte)((value >> 8) & 0xFF);
            buffer[2] = (byte)((value >> 16) & 0xFF);
            OutStream.Write(buffer);
        }

        public void WriteGMString(string value)
        {
            int len = encoding.GetByteCount(value);
            Span<byte> buffer = stackalloc byte[Math.Max(4, len)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, len);
            OutStream.Write(buffer[..4]);

            encoding.GetBytes(value.AsSpan(), buffer);
            OutStream.Write(len < 4 ? buffer[..len] : buffer);
            OutStream.WriteByte(0);
        }
    }
}
