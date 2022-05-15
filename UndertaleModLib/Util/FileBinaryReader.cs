using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UndertaleModLib.Util
{
    // Reimplemented based on DogScepter's implementation
    public class FileBinaryReader : IDisposable
    {
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
            byte[] buf = new byte[count];
            Stream.Read(buf, 0, count);

            return encoding.GetString(buf);
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
            return (short)(Stream.ReadByte() | Stream.ReadByte() << 8);
        }

        public ushort ReadUInt16()
        {
#if DEBUG
            if (Position + 2 > Length)
                throw new IOException("Reading out of bounds");
#endif
            return (ushort)(Stream.ReadByte() | Stream.ReadByte() << 8);
        }

        public int ReadInt24()
        {
#if DEBUG
            if (Position + 3 > Length)
                throw new IOException("Reading out of bounds");
#endif
            return Stream.ReadByte() | Stream.ReadByte() << 8 | (sbyte)Stream.ReadByte() << 16;
        }

        public uint ReadUInt24()
        {
#if DEBUG
            if (Position + 3 > Length)
                throw new IOException("Reading out of bounds");
#endif
            return (uint)(Stream.ReadByte() | Stream.ReadByte() << 8 | Stream.ReadByte() << 16);
        }

        public int ReadInt32()
        {
#if DEBUG
            if (Position + 4 > Length)
                throw new IOException("Reading out of bounds");
#endif
            return Stream.ReadByte() | Stream.ReadByte() << 8 |
                   Stream.ReadByte() << 16 | (sbyte)Stream.ReadByte() << 24;
        }

        public uint ReadUInt32()
        {
#if DEBUG
            if (Position + 4 > Length)
                throw new IOException("Reading out of bounds");
#endif
            return (uint)(Stream.ReadByte() | Stream.ReadByte() << 8 |
                          Stream.ReadByte() << 16 | Stream.ReadByte() << 24);
        }

        public float ReadSingle()
        {
#if DEBUG
            if (Position + 4 > Length)
                throw new IOException("Reading out of bounds");
#endif
            byte[] buf = new byte[4];
            Stream.Read(buf, 0, 4);
            float val = BitConverter.ToSingle(buf);
            return val;
        }

        public double ReadDouble()
        {
#if DEBUG
            if (Position + 8 > Length)
                throw new IOException("Reading out of bounds");
#endif
            byte[] buf = new byte[8];
            Stream.Read(buf, 0, 8);
            double val = BitConverter.ToDouble(buf);
            return val;
        }

        public long ReadInt64()
        {
#if DEBUG
            if (Position + 8 > Length)
                throw new IOException("Reading out of bounds");
#endif
            byte[] buf = new byte[8];
            Stream.Read(buf, 0, 8);
            long val = BitConverter.ToInt64(buf);
            return val;
        }

        public ulong ReadUInt64()
        {
#if DEBUG
            if (Position + 8 > Length)
                throw new IOException("Reading out of bounds");
#endif
            byte[] buf = new byte[8];
            Stream.Read(buf, 0, 8);
            ulong val = BitConverter.ToUInt64(buf);
            return val;
        }

        public string ReadGMString()
        {
#if DEBUG
            if (Position + 8 > Length)
                throw new IOException("Reading out of bounds");
#endif
            int length = Stream.ReadByte() | Stream.ReadByte() << 8 | Stream.ReadByte() << 16 | Stream.ReadByte() << 24;
#if DEBUG
            if (Position + length + 1 >= Length)
                throw new IOException("Reading out of bounds");
#endif
            byte[] buf = new byte[length];
            Stream.Read(buf, 0, length);
            string res = encoding.GetString(buf);
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