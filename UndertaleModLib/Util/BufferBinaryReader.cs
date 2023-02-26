using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UndertaleModLib.Util
{
    // Initial implementation was based on DogScepter's implementation
    public class BufferBinaryReader : IBinaryReader
    {

        // A faster implementation of "MemoryStream"
        private class ChunkBuffer
        {
            private readonly byte[] _buffer;

            public int Position { get; set; }
            public int Length { get; private set; }
            public int Capacity { get; set; }

            public ChunkBuffer(int capacity)
            {
                _buffer = new byte[capacity];
            }

            public int Read(byte[] buffer, int count)
            {
                int n = Length - Position;
                if (n > count)
                    n = count;
                if (n <= 0)
                    return 0;

                if (n <= 8)
                {
                    int byteCount = n;
                    while (--byteCount >= 0)
                        buffer[byteCount] = _buffer[Position + byteCount];
                }
                else
                    Buffer.BlockCopy(_buffer, Position, buffer, 0, n);
                Position += n;

                return n;
            }
            public int Read(Span<byte> buffer)
            {
                int n = Math.Min(Length - Position, buffer.Length);
                if (n <= 0)
                    return 0;

                new Span<byte>(_buffer, Position, n).CopyTo(buffer);

                Position += n;
                return n;
            }
            public int ReadByte()
            {
                if (Position >= Length)
                    return -1;

                return _buffer[Position++];
            }

            public void Write(byte[] buffer, int count)
            {
                int i = Position + count;
                if (i < 0)
                    throw new IOException("Writing out of the chunk buffer bounds.");

                // "MemoryStream" also extends the buffer if 
                // the length becomes greater than the capacity
                Length = i;

                if ((count <= 8) && (buffer != _buffer))
                {
                    int byteCount = count;
                    while (--byteCount >= 0)
                    {
                        _buffer[Position + byteCount] = buffer[byteCount];
                    }
                }
                else
                {
                    Buffer.BlockCopy(buffer, 0, _buffer, Position, count);
                }

                Position = i;
            }
        }


        private readonly byte[] buffer = new byte[16];
        private readonly ChunkBuffer chunkBuffer;
        private readonly byte[] chunkCopyBuffer = new byte[81920];

        private readonly Encoding encoding = new UTF8Encoding(false);
        public Stream Stream { get; set; }

        public long Length { get; }

        public uint Position
        {
            get => (uint)chunkBuffer.Position;
            set
            {
                if (value > chunkBuffer.Length)
                    throw new IOException("Reading out of the chunk bounds.");

                chunkBuffer.Position = (int)value;
            }
        }

        public BufferBinaryReader(Stream stream, Encoding encoding = null)
        {
            Length = stream.Length;
            Stream = stream;

            // Check data file length
            if (Length < 10 * 1024 * 1024) // 10 MB
                chunkBuffer = new(5 * 1024 * 1024);
            else
                chunkBuffer = new(10 * 1024 * 1024);

            if (stream.Position != 0)
                stream.Seek(0, SeekOrigin.Begin);

            if (encoding is not null)
                this.encoding = encoding;
        }

        public void CopyChunkToBuffer(uint length)
        {
            Stream.Position -= 8; // Chunk name + length
            chunkBuffer.Position = 0;

            // Source - https://stackoverflow.com/a/13022108/12136394
            int read;
            int remaining = (int)length + 8;
            while (remaining > 0 &&
                   (read = Stream.Read(chunkCopyBuffer, 0, Math.Min(chunkCopyBuffer.Length, remaining))) > 0)
            {
                chunkBuffer.Write(chunkCopyBuffer, read);
                remaining -= read;
            }

            Stream.Position -= length;
            chunkBuffer.Position -= (int)length;
        }
        private ReadOnlySpan<byte> ReadToBuffer(int count)
        {
            chunkBuffer.Read(buffer, count);
            return buffer;
        }

        public byte ReadByte()
        {
#if DEBUG
            if (Position + 1 > Length)
                throw new IOException("Reading out of bounds");
#endif
            return (byte)chunkBuffer.ReadByte();
        }

        public virtual bool ReadBoolean()
        {
            return ReadByte() != 0;
        }

        public string ReadChars(int count)
        {
#if DEBUG
            if (Position + count > Length)
                throw new IOException("Reading out of bounds");
#endif
            if (count > 1024)
            {
                byte[] buf = new byte[count];
                chunkBuffer.Read(buf, count);

                return encoding.GetString(buf);
            }
            else
            {
                Span<byte> buf = stackalloc byte[count];
                chunkBuffer.Read(buf);

                return encoding.GetString(buf);
            }
        }

        public byte[] ReadBytes(int count)
        {
#if DEBUG
            if (Position + count > Length)
                throw new IOException("Reading out of bounds");
#endif
            byte[] val = new byte[count];
            chunkBuffer.Read(val, count);
            return val;
        }

        public short ReadInt16()
        {
#if DEBUG
            if (Position + 2 > Length)
                throw new IOException("Reading out of bounds");
#endif
            return BinaryPrimitives.ReadInt16LittleEndian(ReadToBuffer(2));
        }

        public ushort ReadUInt16()
        {
#if DEBUG
            if (Position + 2 > Length)
                throw new IOException("Reading out of bounds");
#endif
            return BinaryPrimitives.ReadUInt16LittleEndian(ReadToBuffer(2));
        }

        public int ReadInt24()
        {
#if DEBUG
            if (Position + 3 > Length)
                throw new IOException("Reading out of bounds");
#endif
            ReadToBuffer(3);
            return buffer[0] | buffer[1] << 8 | (sbyte)buffer[2] << 16;
        }

        public uint ReadUInt24()
        {
#if DEBUG
            if (Position + 3 > Length)
                throw new IOException("Reading out of bounds");
#endif
            ReadToBuffer(3);
            return (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16);
        }

        public int ReadInt32()
        {
#if DEBUG
            if (Position + 4 > Length)
                throw new IOException("Reading out of bounds");
#endif
            return BinaryPrimitives.ReadInt32LittleEndian(ReadToBuffer(4));
        }

        public uint ReadUInt32()
        {
#if DEBUG
            if (Position + 4 > Length)
                throw new IOException("Reading out of bounds");
#endif
            return BinaryPrimitives.ReadUInt32LittleEndian(ReadToBuffer(4));
        }

        public float ReadSingle()
        {
#if DEBUG
            if (Position + 4 > Length)
                throw new IOException("Reading out of bounds");
#endif
            return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(ReadToBuffer(4)));
        }

        public double ReadDouble()
        {
#if DEBUG
            if (Position + 8 > Length)
                throw new IOException("Reading out of bounds");
#endif
            return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(ReadToBuffer(8)));
        }

        public long ReadInt64()
        {
#if DEBUG
            if (Position + 8 > Length)
                throw new IOException("Reading out of bounds");
#endif
            return BinaryPrimitives.ReadInt64LittleEndian(ReadToBuffer(8));
        }

        public ulong ReadUInt64()
        {
#if DEBUG
            if (Position + 8 > Length)
                throw new IOException("Reading out of bounds");
#endif
            return BinaryPrimitives.ReadUInt64LittleEndian(ReadToBuffer(8));
        }

        public string ReadGMString()
        {
#if DEBUG
            if (Position + 5 > Length)
                throw new IOException("Reading out of bounds");
#endif
            int length = BinaryPrimitives.ReadInt32LittleEndian(ReadToBuffer(4));
#if DEBUG
            if (Position + length + 1 >= Length)
                throw new IOException("Reading out of bounds");
#endif
            string res;
            if (length > 1024)
            {
                byte[] buf = new byte[length];
                chunkBuffer.Read(buf, length);
                res = encoding.GetString(buf);
            }
            else
            {
                Span<byte> buf = stackalloc byte[length];
                chunkBuffer.Read(buf);
                res = encoding.GetString(buf);
            }
            
#if DEBUG
            if (ReadByte() != 0)
                throw new IOException("String not null terminated!");
#else
            Position++;
#endif
            return res;
        }
        public void SkipGMString()
        {
            int length = BinaryPrimitives.ReadInt32LittleEndian(ReadToBuffer(4));
            Position += (uint)length + 1;
        }

        public void Dispose()
        {
        }
    }
}