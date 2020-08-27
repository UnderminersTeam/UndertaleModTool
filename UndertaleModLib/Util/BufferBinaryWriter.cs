using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Util
{
    public class BufferBinaryWriter : IDisposable
    {
        private const int BaseBufferSize = 1024 * 1024 * 32;

        private readonly Stream stream;
        private byte[] buffer;
        private long offset;
        private int currentSize;
        private Encoding encoding;

        public uint Position { get => (uint)offset; set
            {
                ResizeToFit((int)value);
                offset = value;
            } }

        public byte[] RawBuffer { get => buffer; }
        public Encoding Encoding { get => encoding; }

        public BufferBinaryWriter(Stream stream)
        {
            this.stream = stream;
            buffer = new byte[BaseBufferSize];
            currentSize = 0;
            offset = 0;

            encoding = new UTF8Encoding(false);
        }

        private void ResizeToFit(int size)
        {
            while (size > buffer.Length)
                Array.Resize(ref buffer, buffer.Length * 2);
            if (currentSize < size)
                currentSize = size;
        }

        public void Write(byte value)
        {
            ResizeToFit((int)offset + 1);
            buffer[offset++] = value;
        }

        public void Write(sbyte value)
        {
            ResizeToFit((int)offset + 1);
            buffer[offset++] = (byte)value;
        }

        public virtual void Write(bool value)
        {
            ResizeToFit((int)offset + 1);
            buffer[offset++] = (byte)(value ? 1 : 0);
        }

        public void Write(byte[] value)
        {
            ResizeToFit((int)offset + value.Length);
            Buffer.BlockCopy(value, 0, buffer, (int)offset, value.Length);
            offset += value.Length;
        }

        public void Write(char[] value)
        {
            ResizeToFit((int)offset + value.Length);
            foreach (char c in value)
                buffer[offset++] = Convert.ToByte(c);
        }

        public void Write(ushort value)
        {
            ResizeToFit((int)offset + 2);
            buffer[offset++] = (byte)(value & 0xFF);
            buffer[offset++] = (byte)((value >> 8) & 0xFF);
        }

        public void Write(short value)
        {
            ResizeToFit((int)offset + 2);
            buffer[offset++] = (byte)(value & 0xFF);
            buffer[offset++] = (byte)((value >> 8) & 0xFF);
        }

        public void Write(float value)
        {
            ResizeToFit((int)offset + 4);
            byte[] bytes = BitConverter.GetBytes(value);
            buffer[offset++] = bytes[0];
            buffer[offset++] = bytes[1];
            buffer[offset++] = bytes[2];
            buffer[offset++] = bytes[3];
        }

        public void Write(int value)
        {
            ResizeToFit((int)offset + 4);
            buffer[offset++] = (byte)(value & 0xFF);
            buffer[offset++] = (byte)((value >> 8) & 0xFF);
            buffer[offset++] = (byte)((value >> 16) & 0xFF);
            buffer[offset++] = (byte)((value >> 24) & 0xFF);
        }

        public void Write(uint value)
        {
            ResizeToFit((int)offset + 4);
            buffer[offset++] = (byte)(value & 0xFF);
            buffer[offset++] = (byte)((value >> 8) & 0xFF);
            buffer[offset++] = (byte)((value >> 16) & 0xFF);
            buffer[offset++] = (byte)((value >> 24) & 0xFF);
        }

        public void Write(double value)
        {
            ResizeToFit((int)offset + 8);
            byte[] bytes = BitConverter.GetBytes(value);
            buffer[offset++] = bytes[0];
            buffer[offset++] = bytes[1];
            buffer[offset++] = bytes[2];
            buffer[offset++] = bytes[3];
            buffer[offset++] = bytes[4];
            buffer[offset++] = bytes[5];
            buffer[offset++] = bytes[6];
            buffer[offset++] = bytes[7];
        }

        public void Write(ulong value)
        {
            ResizeToFit((int)offset + 8);
            buffer[offset++] = (byte)(value & 0xFF);
            buffer[offset++] = (byte)((value >> 8) & 0xFF);
            buffer[offset++] = (byte)((value >> 16) & 0xFF);
            buffer[offset++] = (byte)((value >> 24) & 0xFF);
            buffer[offset++] = (byte)((value >> 32) & 0xFF);
            buffer[offset++] = (byte)((value >> 40) & 0xFF);
            buffer[offset++] = (byte)((value >> 48) & 0xFF);
            buffer[offset++] = (byte)((value >> 56) & 0xFF);
        }

        public void Write(long value)
        {
            ResizeToFit((int)offset + 8);
            buffer[offset++] = (byte)(value & 0xFF);
            buffer[offset++] = (byte)((value >> 8) & 0xFF);
            buffer[offset++] = (byte)((value >> 16) & 0xFF);
            buffer[offset++] = (byte)((value >> 24) & 0xFF);
            buffer[offset++] = (byte)((value >> 32) & 0xFF);
            buffer[offset++] = (byte)((value >> 40) & 0xFF);
            buffer[offset++] = (byte)((value >> 48) & 0xFF);
            buffer[offset++] = (byte)((value >> 56) & 0xFF);
        }

        public void WriteGMString(string value)
        {
            int len = encoding.GetByteCount(value);
            ResizeToFit((int)offset + len + 5);
            buffer[offset++] = (byte)(len & 0xFF);
            buffer[offset++] = (byte)((len >> 8) & 0xFF);
            buffer[offset++] = (byte)((len >> 16) & 0xFF);
            buffer[offset++] = (byte)((len >> 24) & 0xFF);
            encoding.GetBytes(value, 0, value.Length, buffer, (int)offset);
            offset += len;
            buffer[offset++] = 0;
        }

        public virtual void Flush()
        {
            stream.Write(buffer, 0, currentSize);
        }

        public void Dispose()
        {
            stream.Close();
        }
    }
}
