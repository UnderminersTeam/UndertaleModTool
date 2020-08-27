using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Util
{
    public class BufferBinaryReader : IDisposable
    {
        private const int MaxBufferSize = 1024 * 1024 * 16;

        private readonly Stream stream;
        private readonly long streamSize;
        private readonly byte[] buffer;
        private int bufferSize;
        private long offset;
        private int bufferOffset;
        private Encoding encoding;
        public Encoding Encoding { get => encoding; }

        public uint Position { get => (uint)offset; set 
            {
                int diff = (int)(value - offset);
                int relative = bufferOffset + diff;
                if (relative >= 0 && relative < bufferSize)
                {
                    bufferOffset += diff;
                    offset = value;
                }
                else
                {
                    offset = value;
                    bufferSize = 0;
                }
            } }
        public long Length { get => streamSize; }

        public BufferBinaryReader(Stream stream)
        {
            this.stream = stream;
            streamSize = stream.Length;
            bufferSize = (streamSize < MaxBufferSize) ? (int)streamSize : MaxBufferSize;
            buffer = new byte[bufferSize];
            offset = 0;
            bufferOffset = 0;
            NextBuffer();

            encoding = new UTF8Encoding(false);
        }

        private void NextBuffer(bool aboutToRead = true)
        {
            bufferOffset = 0;
            bufferSize = (offset + MaxBufferSize > streamSize) ? (int)(streamSize - offset) : MaxBufferSize;
            if (aboutToRead)
            {
                if (bufferSize <= 0)
                    throw new Exception("Reading beyond end of stream");
            }
            else if (bufferSize < 0)
                throw new Exception("Seeking beyond end of stream");
            if (stream.Position != offset)
                stream.Seek(offset, SeekOrigin.Begin);
            stream.Read(buffer, 0, bufferSize);
        }

        public void SmallReadAt(uint position, int size)
        {
            offset = position;
            bufferOffset = 0;
            bufferSize = (offset + size > streamSize) ? (int)(streamSize - offset) : size;
            if (bufferSize < 0)
                throw new Exception("Seeking beyond end of stream");
            stream.Seek(offset, SeekOrigin.Begin);
            stream.Read(buffer, 0, bufferSize);
        }

        public byte ReadByte()
        {
            if (bufferOffset + 1 >= bufferSize)
                NextBuffer();

            var val = buffer[bufferOffset++];
            offset++;
            return val;
        }

        public virtual bool ReadBoolean()
        {
            return ReadByte() != 0;
        }

        public string ReadChars(int count)
        {
            if (bufferOffset + count >= bufferSize)
                NextBuffer();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
                sb.Append(Convert.ToChar(buffer[bufferOffset++]));
            offset += count;
            return sb.ToString();
        }

        public byte[] ReadBytes(int count)
        {
            if (bufferOffset + count >= bufferSize)
                NextBuffer();

            byte[] val = new byte[count];

            if (count > MaxBufferSize)
            {
                // Do it manually
                if (stream.Position != offset)
                    stream.Seek(offset, SeekOrigin.Begin);
                stream.Read(val, 0, count);
                bufferOffset += count;
                offset += count;
                return val;
            }

            Buffer.BlockCopy(buffer, bufferOffset, val, 0, count);
            bufferOffset += count;
            offset += count;
            return val;
        }

        public short ReadInt16()
        {
            if (bufferOffset + 2 >= bufferSize)
                NextBuffer();

            var val = (short)(buffer[bufferOffset++] | buffer[bufferOffset++] << 8);
            offset += 2;
            return val;
        }

        public ushort ReadUInt16()
        {
            if (bufferOffset + 2 >= bufferSize)
                NextBuffer();

            var val = (ushort)(buffer[bufferOffset++] | buffer[bufferOffset++] << 8);
            offset += 2;
            return val;
        }

        public int ReadInt24()
        {
            if (bufferOffset + 3 >= bufferSize)
                NextBuffer();

            var val = (int)(buffer[bufferOffset++] | buffer[bufferOffset++] << 8 | buffer[bufferOffset++] << 16);
            offset += 3;
            return val;
        }

        public uint ReadUInt24()
        {
            if (bufferOffset + 3 >= bufferSize)
                NextBuffer();

            var val = (uint)(buffer[bufferOffset++] | buffer[bufferOffset++] << 8 | buffer[bufferOffset++] << 16);
            offset += 3;
            return val;
        }

        public int ReadInt32()
        {
            if (bufferOffset + 4 >= bufferSize)
                NextBuffer();

            var val = (int)(buffer[bufferOffset++] | buffer[bufferOffset++] << 8 | buffer[bufferOffset++] << 16 | buffer[bufferOffset++] << 24);
            offset += 4;
            return val;
        }

        public uint ReadUInt32()
        {
            if (bufferOffset + 4 >= bufferSize)
                NextBuffer();

            var val = (uint)(buffer[bufferOffset++] | buffer[bufferOffset++] << 8 | buffer[bufferOffset++] << 16 | buffer[bufferOffset++] << 24);
            offset += 4;
            return val;
        }

        public float ReadSingle()
        {
            if (bufferOffset + 4 >= bufferSize)
                NextBuffer();

            var val = BitConverter.ToSingle(buffer, bufferOffset);
            bufferOffset += 4;
            offset += 4;
            return val;
        }

        public double ReadDouble()
        {
            if (bufferOffset + 8 >= bufferSize)
                NextBuffer();

            var val = BitConverter.ToDouble(buffer, bufferOffset);
            bufferOffset += 8;
            offset += 8;
            return val;
        }

        public long ReadInt64()
        {
            if (bufferOffset + 8 >= bufferSize)
                NextBuffer();

            var val = BitConverter.ToInt64(buffer, bufferOffset);
            bufferOffset += 8;
            offset += 8;
            return val;
        }

        public ulong ReadUInt64()
        {
            if (bufferOffset + 8 >= bufferSize)
                NextBuffer();

            var val = BitConverter.ToUInt64(buffer, bufferOffset);
            bufferOffset += 8;
            offset += 8;
            return val;
        }

        public string ReadGMString()
        {
            if (bufferOffset + 5 >= bufferSize)
                NextBuffer();
            int length = (int)(buffer[bufferOffset++] | buffer[bufferOffset++] << 8 | buffer[bufferOffset++] << 16 | buffer[bufferOffset++] << 24);
            offset += 4;
            if (bufferOffset + length + 1 >= bufferSize)
                NextBuffer();
            string res = encoding.GetString(buffer, bufferOffset, length);
            bufferOffset += length;
            if (buffer[bufferOffset++] != 0)
                throw new IOException("String not null terminated!");
            offset += length + 1;
            return res;
        }

        public void Dispose()
        {
            stream.Close();
        }
    }
}
