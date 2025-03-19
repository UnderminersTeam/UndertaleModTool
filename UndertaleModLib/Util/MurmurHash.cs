using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace UndertaleModLib.Util;

/// <summary>
/// 32-bit implementation of MurmurHash, for deterministic (but non-cryptographic) hashing.
/// </summary>
/// <remarks>
/// Based on https://en.wikipedia.org/wiki/MurmurHash.
/// </remarks>
internal static class MurmurHash
{
    /// <summary>
    /// Calculates a hash based on a span of characters.
    /// </summary>
    /// <remarks>
    /// The characters will be re-encoded as a UTF-8 buffer before hashing.
    /// </remarks>
    public static uint Hash(ReadOnlySpan<char> chars, uint seed = 0)
    {
        // Allocate (probably) enough space on stack - it's fine if there isn't enough in edge cases
        Span<byte> utf8 = stackalloc byte[chars.Length * 2];

        // Decode UTF8 into stack memory and hash it
        int numBytes = Encoding.UTF8.GetBytes(chars, utf8);
        return Hash(utf8[..numBytes], seed);
    }

    /// <summary>
    /// Calculates a hash based on a span of bytes.
    /// </summary>
    public static uint Hash(ReadOnlySpan<byte> bytes, uint seed = 0)
    {
        // Initialize hash based on seed
        uint hash = seed;
        int len = bytes.Length;

        // Process each group of 4 bytes
        int position = 0;
        for (int i = len >> 2; i > 0; i--)
        {
            // Take next 4 bytes
            uint group = BitConverter.ToUInt32(bytes[position..]);
            if (!BitConverter.IsLittleEndian)
            {
                // Swap endianness to match little endian
                group = (group >> 16) | (group << 16);
                group = ((group & 0xff00ff00) >> 8) | ((group & 0x00ff00ff) << 8);
            }

            // Apply group to current hash value
            hash ^= Scramble(group);
            hash = (hash << 13) | (hash >> 19);
            hash = (hash * 5) + 0xe6546b64;

            // Move to next 4 byte chunk
            position += 4;
        }

        // Take remaining bytes from the end (0 to 3 bytes)
        uint endGroup = 0;
        for (int i = len & 3; i > 0; i--)
        {
            endGroup <<= 8;
            endGroup |= bytes[position + (i - 1)];
        }

        // Apply ending group to current hash value
        hash ^= Scramble(endGroup);

        // Perform final operations on hash value
        hash ^= (uint)len;
        hash ^= hash >> 16;
        hash *= 0x85ebca6b;
        hash ^= hash >> 13;
        hash *= 0xc2b2ae35;
        hash ^= hash >> 16;

        return hash;
    }

    /// <summary>
    /// 32-bit Murmur hash scramble operation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Scramble(uint number)
    {
        number *= 0xcc9e2d51;
        number = (number << 15) | (number >> 17);
        return number * 0x1b873593;
    }
}
