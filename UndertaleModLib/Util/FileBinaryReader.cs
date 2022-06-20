using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UndertaleModLib.Util
{
    // Reimplemented based on DogScepter's implementation
    public class FileBinaryReader : IDisposable
    {
        private readonly byte[] buffer = new byte[16];

        private Encoding encoding = new UTF8Encoding(false);
        public Encoding Encoding { get => encoding; }
        public Stream Stream { get; set; }

        public long Length { get; private set; }

        public uint Position
        {
            get => (uint)Stream.Position;
            set
            {
                if (value > Length)
                    throw new IOException("Reading out of bounds.");

                Stream.Position = value;
            }
        }

        public FileBinaryReader(Stream stream, Encoding encoding = null)
        {
            Length = stream.Length;
            Stream = stream;
            Position = 0;

            if (stream.Position != 0)
                stream.Seek(0, SeekOrigin.Begin);

            if (encoding is not null)
                this.encoding = encoding;
        }

        private ReadOnlySpan<byte> ReadToBuffer(int count)
        {
            Stream.Read(buffer, 0, count);
            return buffer;
        }

        public byte ReadByte()
        {
#if DEBUG
            if (Position + 1 > Length)
                throw new IOException("Reading out of bounds");
#endif
            return (byte)Stream.ReadByte();
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
                Stream.Read(buf, 0, count);

                return encoding.GetString(buf);
            }
            else
            {
                Span<byte> buf = stackalloc byte[count];
                Stream.Read(buf);

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
            Stream.Read(val, 0, count);
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
            if (Position + 8 > Length)
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
                Stream.Read(buf, 0, length);
                res = encoding.GetString(buf);
            }
            else
            {
                Span<byte> buf = stackalloc byte[length];
                Stream.Read(buf);
                res = encoding.GetString(buf);
            }
            
#if DEBUG
            if (Stream.ReadByte() != 0)
                throw new IOException("String not null terminated!");
#else
            Position++;
#endif
            return res;
        }

        public void Dispose()
        {
            if (Stream is not null)
            {
                Stream.Close();
                Stream.Dispose();
            }
        }
    }
}