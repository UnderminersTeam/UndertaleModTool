using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;

namespace UndertaleModLib.Util;

/// <summary>
/// Utility class containing static methods for use in applying BPS file patches.
/// Specification used is in the public domain.
/// </summary>
/// <remarks>
/// Sets an arbitrary safety limit at <see cref="MaxFilesize"/> bytes for base/source and output/target files.
/// </remarks>
public static class BPS
{
    /// <summary>
    /// Header magic used for BPS patch files, represented in little endian.
    /// </summary>
    public const uint Magic = 0x31535042; // "BPS1"

    /// <summary>
    /// Safety limit for maximum filesize, including base/source and output/target files.
    /// </summary>
    /// <remarks>
    /// Limited to 4GB, which is the practical limit for GameMaker data files.
    /// </remarks>
    public const long MaxFilesize = int.MaxValue;

    // Lookup table for CRC32 calculations
    private static readonly uint[] _crcTable = new uint[256];

    // Initializes CRC32 lookup table
    static BPS()
    {
        for (uint n = 0; n < 256; n++)
        {
            uint val = n;
            for (int k = 0; k < 8; k++)
            {
                val = (val & 1) != 0 ? (0xedb88320 ^ (val >> 1)) : (val >> 1);
            }
            _crcTable[n] = val;
        }
    }

    /// <summary>
    /// Applies a BPS file patch from <paramref name="patchFile"/> to <paramref name="baseFile"/>, 
    /// with data output to <paramref name="outputFile"/>.
    /// </summary>
    /// <remarks>
    /// Will throw an exception if patching fails for any reason, in which case 
    /// <paramref name="outputFile"/> should be cleared/dealt with manually.
    /// </remarks>
    public static void ApplyPatch(Stream baseFile, Stream patchFile, Stream outputFile)
    {
        // Seek input streams to beginning, just in case
        baseFile.Seek(0, SeekOrigin.Begin);
        patchFile.Seek(0, SeekOrigin.Begin);

        // Verify magic in patch file
        Span<byte> readBuffer = stackalloc byte[4];
        patchFile.ReadExactly(readBuffer);
        if (BinaryPrimitives.ReadUInt32LittleEndian(readBuffer) != Magic)
        {
            throw new IOException("Patch file is not a valid BPS file");
        }

        // Read header of patch file
        long sourceSize = DecodeNumber(patchFile);
        long targetSize = DecodeNumber(patchFile);
        long metadataSize = DecodeNumber(patchFile);
        long metadataPosition = patchFile.Position;
        if (sourceSize > MaxFilesize)
        {
            throw new IOException("Base filesize is too large");
        }
        if (sourceSize != baseFile.Length)
        {
            throw new IOException($"Input (base) file is invalid for the patch, or has been corrupted " +
                                  $"(size is {baseFile.Length} bytes, expected {sourceSize} bytes)");
        }
        if (targetSize > MaxFilesize)
        {
            throw new IOException("Target filesize is too large");
        }
        if (metadataSize > patchFile.Length - metadataPosition - 12)
        {
            throw new IOException("Invalid metadata length (exceeds length of patch data)");
        }

        // Rent buffer for use in file I/O operations.
        // Size is based on the internal size used for Stream copies in the official .NET library implementation.
        byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            // Verify patch file CRC based on the number at the end of the patch stream.
            // We do this now to prevent most cases of erroneous patch applications due to corrupt files.
            long patchLength = patchFile.Length;
            long patchDataLength = patchLength - 12;
            patchFile.Seek(0, SeekOrigin.Begin);
            uint actualPatchCRC = CalculateCRC(patchFile, patchLength - 4, sharedBuffer);
            patchFile.ReadExactly(readBuffer);
            uint expectedPatchCRC = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer);
            if (actualPatchCRC != expectedPatchCRC)
            {
                throw new IOException("Patch file has been corrupted (CRC32 checksum mismatch)");
            }

            // Verify base file CRC based on the number at the end of the patch stream.
            // Similar to the last check, we do this now to prevent most cases of erroneous patch applications.
            patchFile.Seek(patchDataLength, SeekOrigin.Begin);
            uint actualBaseCRC = CalculateCRC(baseFile, baseFile.Length, sharedBuffer);
            patchFile.ReadExactly(readBuffer);
            uint expectedBaseCRC = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer);
            if (actualBaseCRC != expectedBaseCRC)
            {
                throw new IOException("Input (base) file is invalid for the patch, or has been corrupted (CRC32 checksum mismatch)");
            }

            // Also read the CRC for the output file while we're right here (will be checked later)
            patchFile.ReadExactly(readBuffer);
            uint expectedOutputCRC = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer);

            // Skip metadata and apply patch
            patchFile.Seek(metadataPosition + metadataSize, SeekOrigin.Begin);
            long outputOffset = 0;
            long sourceRelativeOffset = 0;
            long targetRelativeOffset = 0;
            while (patchFile.Position < patchDataLength)
            {
                // Decode next command and length
                long data = DecodeNumber(patchFile);
                long command = data & 0b11;
                long length = (data >> 2) + 1;

                // Make sure output filesize is within expectation
                if (outputOffset + length > targetSize)
                {
                    throw new IOException("Output filesize overrun");
                }

                // Execute command
                switch (command)
                {
                    case 0:
                        {
                            // Source read (copy identical data from the same exact position in the file)
                            baseFile.Seek(outputOffset, SeekOrigin.Begin);
                            outputOffset += length;

                            // Copy bytes in chunks
                            do
                            {
                                int numBytesToCopy = (int)Math.Min(sharedBuffer.Length, length);
                                baseFile.ReadExactly(sharedBuffer, 0, numBytesToCopy);
                                outputFile.Write(sharedBuffer, 0, numBytesToCopy);
                                length -= numBytesToCopy;
                            }
                            while (length > 0);
                            break;
                        }
                    case 1:
                        {
                            // Target read (copy in new data directly from the patch file)
                            outputOffset += length;

                            // Copy bytes in chunks
                            do
                            {
                                int numBytesToCopy = (int)Math.Min(sharedBuffer.Length, length);
                                patchFile.ReadExactly(sharedBuffer, 0, numBytesToCopy);
                                outputFile.Write(sharedBuffer, 0, numBytesToCopy);
                                length -= numBytesToCopy;
                            }
                            while (length > 0);
                            break;
                        }
                    case 2:
                        {
                            // Source copy (copy data from an arbitrary location in the base file)
                            long sourceOffsetData = DecodeNumber(patchFile);
                            sourceRelativeOffset += ((sourceOffsetData & 1) != 0 ? -1 : 1) * (sourceOffsetData >> 1);
                            baseFile.Seek(sourceRelativeOffset, SeekOrigin.Begin);
                            sourceRelativeOffset += length;
                            outputOffset += length;

                            // Copy bytes in chunks
                            do
                            {
                                int numBytesToCopy = (int)Math.Min(sharedBuffer.Length, length);
                                baseFile.ReadExactly(sharedBuffer, 0, numBytesToCopy);
                                outputFile.Write(sharedBuffer, 0, numBytesToCopy);
                                length -= numBytesToCopy;
                            }
                            while (length > 0);
                            break;
                        }
                    case 3:
                        {
                            // Target copy (copy data from an arbitrary location in the data already written to the output file)
                            long targetOffsetData = DecodeNumber(patchFile);
                            targetRelativeOffset += ((targetOffsetData & 1) != 0 ? -1 : 1) * (targetOffsetData >> 1);

                            // Copy bytes in chunks while possible
                            long possibleChunkedLength = Math.Min(length, outputOffset - targetRelativeOffset);
                            length -= possibleChunkedLength;

                            // Copy in chunks
                            int numBytesToCopy;
                            do
                            {
                                numBytesToCopy = (int)Math.Min(sharedBuffer.Length, possibleChunkedLength);
                                outputFile.Seek(targetRelativeOffset, SeekOrigin.Begin);
                                outputFile.ReadExactly(sharedBuffer, 0, numBytesToCopy);
                                outputFile.Seek(outputOffset, SeekOrigin.Begin);
                                outputFile.Write(sharedBuffer, 0, numBytesToCopy);
                                possibleChunkedLength -= numBytesToCopy;
                                targetRelativeOffset += numBytesToCopy;
                                outputOffset += numBytesToCopy;
                            }
                            while (possibleChunkedLength > 0);

                            // Copy the last byte while needed (in chunks)
                            if (length > 0)
                            {
                                // Fill shared buffer with the last byte
                                byte lastByte = sharedBuffer[numBytesToCopy - 1];
                                numBytesToCopy = (int)Math.Min(sharedBuffer.Length, length);
                                for (int i = 0; i < numBytesToCopy; i++)
                                {
                                    sharedBuffer[i] = lastByte;
                                }

                                // Write the shared buffer as many times as needed
                                targetRelativeOffset += length;
                                outputOffset += length;
                                do
                                {
                                    numBytesToCopy = (int)Math.Min(sharedBuffer.Length, length);
                                    outputFile.Write(sharedBuffer, 0, numBytesToCopy);
                                    length -= numBytesToCopy;
                                }
                                while (length > 0);
                            }
                            break;
                        }
                }
            }

            // Verify output filesize
            if (outputOffset != targetSize)
            {
                throw new IOException($"Output filesize mismatch (expected {targetSize} bytes, produced {outputOffset} bytes)");
            }

            // Verify output file CRC based on the number at the end of the patch stream.
            // Similar to the last check, we do this now to prevent most cases of erroneous patch applications.
            outputFile.Seek(0, SeekOrigin.Begin);
            uint actualOutputCRC = CalculateCRC(outputFile, outputFile.Length, sharedBuffer);
            if (actualOutputCRC != expectedOutputCRC)
            {
                throw new IOException("Output file was produced incorrectly (CRC32 checksum mismatch)");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sharedBuffer);
        }
    }

    /// <summary>
    /// Decodes a variable-length integer (up to 63 bits) from the current position of the stream.
    /// </summary>
    private static long DecodeNumber(Stream stream)
    {
        // Follow variable-length encoding scheme as stated in specification,
        // but read only up to 63 bits total (7 * 9 = 63).
        ulong data = 0;
        ulong shift = 1;
        Span<byte> currentByte = stackalloc byte[1];
        for (int i = 0; i < 9; i++)
        {
            stream.ReadExactly(currentByte);
            data += (ulong)(currentByte[0] & 0x7f) * shift;
            if ((currentByte[0] & 0x80) != 0)
            {
                break;
            }
            shift <<= 7;
            data += shift;
        }
        if ((currentByte[0] & 0x80) == 0 || data > long.MaxValue)
        {
            throw new IOException("Number is too large in patch file");
        }
        return (long)data;
    }

    /// <summary>
    /// Calculates CRC32 for the stream, starting at its current seek position, for the given length.
    /// </summary>
    private static uint CalculateCRC(Stream stream, long length, Span<byte> sharedBuffer)
    {
        uint crc = 0xffffffff;

        // Read the stream in chunks of up to 1024
        long position = 0;
        int bytesRead;
        do
        {
            // Read next chunk
            int maxChunkSize = (int)Math.Min(sharedBuffer.Length, length - position);
            bytesRead = stream.Read(sharedBuffer[0..maxChunkSize]);
            position += bytesRead;

            // Update CRC
            for (int i = 0; i < bytesRead; i++)
            {
                crc = (crc >> 8) ^ _crcTable[(crc ^ sharedBuffer[i]) & 0xff];
            }
        }
        while (bytesRead > 0 && position < length);

        return ~crc;
    }
}
