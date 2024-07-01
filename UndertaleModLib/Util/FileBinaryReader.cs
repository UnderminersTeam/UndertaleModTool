using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace UndertaleModLib.Util
{
    // Reimplemented based on DogScepter's implementation
    public class FileBinaryReader : IBinaryReader
    {
        private readonly byte[] buffer = new byte[16];

        private readonly Encoding encoding = new UTF8Encoding(false);
        public Stream Stream { get; set; }

        private readonly long _length;
        public long Length { get => _length; }

        public long Position
        {
            get => Stream.Position;
            set
            {
#if DEBUG
                if (value > Length)
                    throw new IOException("Reading out of bounds.");
#endif
                Stream.Position = value;
            }
        }

        public FileBinaryReader(Stream stream, Encoding encoding = null)
        {
            _length = stream.Length;
            Stream = stream;

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
            if (Stream.Position + 1 > _length)
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
            if (Stream.Position + count > _length)
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
            if (Stream.Position + count > _length)
                throw new IOException("Reading out of bounds");
#endif
            byte[] val = new byte[count];
            Stream.Read(val, 0, count);
            return val;
        }

        public short ReadInt16()
        {
#if DEBUG
            if (Stream.Position + 2 > _length)
                throw new IOException("Reading out of bounds");
#endif
            return BinaryPrimitives.ReadInt16LittleEndian(ReadToBuffer(2));
        }

        public ushort ReadUInt16()
        {
#if DEBUG
            if (Stream.Position + 2 > _length)
                throw new IOException("Reading out of bounds");
#endif
            return BinaryPrimitives.ReadUInt16LittleEndian(ReadToBuffer(2));
        }

        public int ReadInt24()
        {
#if DEBUG
            if (Stream.Position + 3 > _length)
                throw new IOException("Reading out of bounds");
#endif
            ReadToBuffer(3);
            return buffer[0] | buffer[1] << 8 | (sbyte)buffer[2] << 16;
        }

        public uint ReadUInt24()
        {
#if DEBUG
            if (Stream.Position + 3 > _length)
                throw new IOException("Reading out of bounds");
#endif
            ReadToBuffer(3);
            return (uint)(buffer[0] | buffer[1] << 8 | buffer[2] << 16);
        }

        public int ReadInt32()
        {
#if DEBUG
            if (Stream.Position + 4 > _length)
                throw new IOException("Reading out of bounds");
#endif
            return BinaryPrimitives.ReadInt32LittleEndian(ReadToBuffer(4));
        }

        public uint ReadUInt32()
        {
#if DEBUG
            if (Stream.Position + 4 > _length)
                throw new IOException("Reading out of bounds");
#endif
            return BinaryPrimitives.ReadUInt32LittleEndian(ReadToBuffer(4));
        }

        public float ReadSingle()
        {
#if DEBUG
            if (Stream.Position + 4 > _length)
                throw new IOException("Reading out of bounds");
#endif
            return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(ReadToBuffer(4)));
        }

        public double ReadDouble()
        {
#if DEBUG
            if (Stream.Position + 8 > _length)
                throw new IOException("Reading out of bounds");
#endif
            return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(ReadToBuffer(8)));
        }

        public long ReadInt64()
        {
#if DEBUG
            if (Stream.Position + 8 > _length)
                throw new IOException("Reading out of bounds");
#endif
            return BinaryPrimitives.ReadInt64LittleEndian(ReadToBuffer(8));
        }

        public ulong ReadUInt64()
        {
#if DEBUG
            if (Stream.Position + 8 > _length)
                throw new IOException("Reading out of bounds");
#endif
            return BinaryPrimitives.ReadUInt64LittleEndian(ReadToBuffer(8));
        }

        public string ReadGMString()
        {
#if DEBUG
            if (Stream.Position + 5 > _length)
                throw new IOException("Reading out of bounds");
#endif
            int length = BinaryPrimitives.ReadInt32LittleEndian(ReadToBuffer(4));
#if DEBUG
            if (Stream.Position + length + 1 >= _length)
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
        public void SkipGMString()
        {
            int length = BinaryPrimitives.ReadInt32LittleEndian(ReadToBuffer(4));
            Position += (uint)length + 1;
        }

        public void Dispose()
        {
            if (Stream?.CanRead == true)
            {
                Stream.Close();
                Stream.Dispose();
            }
        }
    }
}