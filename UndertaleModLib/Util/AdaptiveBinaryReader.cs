using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Util
{
    public interface IBinaryReader : IDisposable
    {
        public abstract Stream Stream { get; set; }
        public abstract long Length { get; }
        public abstract long Position { get; set; }

        public abstract byte ReadByte();
        public virtual bool ReadBoolean() => false;
        public abstract string ReadChars(int count);
        public abstract byte[] ReadBytes(int count);
        public abstract short ReadInt16();
        public abstract ushort ReadUInt16();
        public abstract int ReadInt24();
        public abstract uint ReadUInt24();
        public abstract int ReadInt32();
        public abstract uint ReadUInt32();
        public abstract float ReadSingle();
        public abstract double ReadDouble();
        public abstract long ReadInt64();
        public abstract ulong ReadUInt64();
        public abstract string ReadGMString();
        public abstract void SkipGMString();
    }

    public class AdaptiveBinaryReader : IBinaryReader
    {
        private readonly FileBinaryReader fileBinaryReader;
        private readonly BufferBinaryReader bufferBinaryReader;
        private IBinaryReader _currentReader;
        private bool isUsingBufferReader = false;
        private bool isCurrChunkTooLarge = false;
        private IBinaryReader CurrentReader
        {
            get => _currentReader;
            set
            {
                _currentReader = value;
                isUsingBufferReader = value is BufferBinaryReader;
            }
        }

        private readonly Encoding encoding = new UTF8Encoding(false);
        public Encoding Encoding { get => encoding; }
        public Stream Stream { get; set; }
        public long Length { get; private set; }

        // I've done some benchmarks, and they show that
        // "if..else" is faster than using interfaces here.
        // (at least in C# 10)
        public long Position
        {
            get
            {
                if (isUsingBufferReader)
                    return bufferBinaryReader.Position;
                else
                    return Stream.Position;
            }
            set
            {
                if (isUsingBufferReader)
                    bufferBinaryReader.Position = value;
                else
                    fileBinaryReader.Position = value;
            }
        }
        public long AbsPosition
        {
            get
            {
                if (isUsingBufferReader)
                    return bufferBinaryReader.ChunkStartPosition + bufferBinaryReader.Position - 8;
                else
                    return Stream.Position;
            }
            set
            {
                if (isUsingBufferReader)
                {
                    if (value < 0 || value > Length
                        throw new IOException("Reading out of bounds.");
                    bufferBinaryReader.Position = value - bufferBinaryReader.ChunkStartPosition + 8;
                }
                else
                    fileBinaryReader.Position = value;
            }
        }

        public AdaptiveBinaryReader(Stream stream, Encoding encoding = null)
        {
            fileBinaryReader = new(stream, encoding);
            bufferBinaryReader = new(stream, encoding);
            CurrentReader = fileBinaryReader;

            Length = stream.Length;
            Stream = stream;

            if (stream.Position != 0)
                stream.Seek(0, SeekOrigin.Begin);

            if (encoding is not null)
                this.encoding = encoding;
        }

        public void CopyChunkToBuffer(uint length)
        {
            if (length <= 12 * 1024 * 1024)
            {
                isCurrChunkTooLarge = false;
                CurrentReader = bufferBinaryReader;
                bufferBinaryReader.CopyChunkToBuffer(length);
            }
            else
            {
                isCurrChunkTooLarge = true;
                CurrentReader = fileBinaryReader;
            }
        }

        public void SwitchReaderType(bool isBufferBinaryReader)
        {
            if (!isBufferBinaryReader && CurrentReader == bufferBinaryReader)
            {
                fileBinaryReader.Position = AbsPosition;
                CurrentReader = fileBinaryReader;
            }
            else if (isBufferBinaryReader && !isCurrChunkTooLarge
                     && CurrentReader == fileBinaryReader)
            {
                CurrentReader = bufferBinaryReader;
                AbsPosition = fileBinaryReader.Position;
            }
        }

        public byte ReadByte()
        {
            if (isUsingBufferReader)
                return bufferBinaryReader.ReadByte();
            else
                return fileBinaryReader.ReadByte();
        }
        public virtual bool ReadBoolean()
        {
            if (isUsingBufferReader)
                return bufferBinaryReader.ReadBoolean();
            else
                return fileBinaryReader.ReadBoolean();
        }
        public string ReadChars(int count)
        {
            if (isUsingBufferReader)
                return bufferBinaryReader.ReadChars(count);
            else
                return fileBinaryReader.ReadChars(count);
        }
        public byte[] ReadBytes(int count)
        {
            if (isUsingBufferReader)
                return bufferBinaryReader.ReadBytes(count);
            else
                return fileBinaryReader.ReadBytes(count);
        }
        public short ReadInt16()
        {
            if (isUsingBufferReader)
                return bufferBinaryReader.ReadInt16();
            else
                return fileBinaryReader.ReadInt16();
        }
        public ushort ReadUInt16()
        {
            if (isUsingBufferReader)
                return bufferBinaryReader.ReadUInt16();
            else
                return fileBinaryReader.ReadUInt16();
        }
        public int ReadInt24()
        {
            if (isUsingBufferReader)
                return bufferBinaryReader.ReadInt24();
            else
                return fileBinaryReader.ReadInt24();
        }
        public uint ReadUInt24()
        {
            if (isUsingBufferReader)
                return bufferBinaryReader.ReadUInt24();
            else
                return fileBinaryReader.ReadUInt24();
        }
        public int ReadInt32()
        {
            if (isUsingBufferReader)
                return bufferBinaryReader.ReadInt32();
            else
                return fileBinaryReader.ReadInt32();
        }
        public uint ReadUInt32()
        {
            if (isUsingBufferReader)
                return bufferBinaryReader.ReadUInt32();
            else
                return fileBinaryReader.ReadUInt32();
        }
        public float ReadSingle()
        {
            if (isUsingBufferReader)
                return bufferBinaryReader.ReadSingle();
            else
                return fileBinaryReader.ReadSingle();
        }
        public double ReadDouble()
        {
            if (isUsingBufferReader)
                return bufferBinaryReader.ReadDouble();
            else
                return fileBinaryReader.ReadDouble();
        }
        public long ReadInt64()
        {
            if (isUsingBufferReader)
                return bufferBinaryReader.ReadInt64();
            else
                return fileBinaryReader.ReadInt64();
        }
        public ulong ReadUInt64()
        {
            if (isUsingBufferReader)
                return bufferBinaryReader.ReadUInt64();
            else
                return fileBinaryReader.ReadUInt64();
        }
        public string ReadGMString()
        {
            if (isUsingBufferReader)
                return bufferBinaryReader.ReadGMString();
            else
                return fileBinaryReader.ReadGMString();
        }
        public void SkipGMString()
        {
            if (isUsingBufferReader)
                bufferBinaryReader.SkipGMString();
            else
                fileBinaryReader.SkipGMString();
        }

        public void Dispose()
        {
            if (Stream is not null)
            {
                Stream.Close();
                Stream.Dispose();
            }
            bufferBinaryReader.Dispose();
        }
    }
}
