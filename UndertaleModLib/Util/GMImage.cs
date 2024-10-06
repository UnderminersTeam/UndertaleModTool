using ICSharpCode.SharpZipLib.BZip2;
using ImageMagick;
using System;
using System.Buffers.Binary;
using System.IO;

namespace UndertaleModLib.Util;

/// <summary>
/// Immutable wrapper around GameMaker texture images.
/// </summary>
public class GMImage
{
    /// <summary>
    /// Supported formats of GameMaker textures.
    /// </summary>
    public enum ImageFormat
    {
        /// <summary>
        /// Raw BGRA color format, with 8 bits per channel (32 bits per pixel).
        /// </summary>
        RawBgra,

        /// <summary>
        /// PNG file format.
        /// </summary>
        Png,

        /// <summary>
        /// GameMaker's custom variant of the QOI image file format.
        /// </summary>
        Qoi,

        /// <summary>
        /// BZip2 compression applied on top of GameMaker's custom variant of the QOI image file format.
        /// </summary>
        Bz2Qoi
    }

    /// <summary>
    /// Format of this image.
    /// </summary>
    public ImageFormat Format { get; init; }

    /// <summary>
    /// Width of this image, in pixels.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Height of this image, in pixels.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Maximum supported image width or height.
    /// </summary>
    public const int MaxImageDimension = 16384;

    /// <summary>
    /// PNG file format magic.
    /// </summary>
    public static ReadOnlySpan<byte> MagicPng => new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

    /// <summary>
    /// QOI file format magic.
    /// </summary>
    public static ReadOnlySpan<byte> MagicQoi => "fioq"u8;

    /// <summary>
    /// BZip2 + QOI file format magic.
    /// </summary>
    public static ReadOnlySpan<byte> MagicBz2Qoi => "2zoq"u8;

    /// <summary>
    /// Magic value found near the end of a BZip2 stream (square root of pi). 
    /// </summary>
    private static ReadOnlySpan<byte> MagicBz2Footer => new byte[] { 0x17, 0x72, 0x45, 0x38, 0x50, 0x90 };

    /// <summary>
    /// Backing data for the image, whether compressed or not.
    /// </summary>
    private readonly byte[] _data = null;

    /// <summary>
    /// If this is a Bz2Qoi image in GameMaker 2022.5 and above, then this is 
    /// the size of the BZip2 data when entirely uncompressed.
    /// </summary>
    private int _bz2UncompressedSize { get; init; } = -1;

    /// <summary>
    /// Initializes an image with raw format, of the desired width and height.
    /// </summary>
    /// <remarks>
    /// Creates a completely blank image (black, fully transparent).
    /// </remarks>
    public GMImage(int width, int height)
    {
        if (width is < 0 or > MaxImageDimension)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }
        if (height is < 0 or > MaxImageDimension)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Format = ImageFormat.RawBgra;
        Width = width;
        Height = height;
        _data = new byte[width * height * 4];
    }

    /// <summary>
    /// Basic private constructor for use by other creation methods; just initializes the given fields.
    /// </summary>
    private GMImage(ImageFormat format, int width, int height, byte[] data)
    {
        Format = format;
        Width = width;
        Height = height;
        _data = data;
    }

    /// <summary>
    /// Searches for the BZ2 footer magic, when around the end of a BZ2 stream, 
    /// and returns the exact end position of the stream.
    /// </summary>
    private static long FindEndOfBZ2Search(IBinaryReader reader, long endDataPosition)
    {
        // Read 16 bytes from the end of the BZ2 stream
        Span<byte> data = stackalloc byte[16];
        reader.Position = endDataPosition - data.Length;
        int numBytesRead = reader.Stream.Read(data);

        // Start searching for magic, bit by bit (it is not always byte-aligned)
        ReadOnlySpan<byte> footerMagic = MagicBz2Footer;
        int searchStartPosition = numBytesRead - 1;
        int searchStartBitPosition = 0;
        while (searchStartPosition >= 0)
        {
            // Perform search starting from the current search start position 
            bool foundMatch = false;
            int bitPosition = searchStartBitPosition;
            int searchPosition = searchStartPosition;
            int magicBitPosition = 0;
            int magicPosition = footerMagic.Length - 1;
            while (searchPosition >= 0)
            {
                // Get bits at search position and corresponding magic position
                bool currentBit = (data[searchPosition] & (1 << bitPosition)) != 0;
                bool magicCurrentBit = (footerMagic[magicPosition] & (1 << magicBitPosition)) != 0;

                // If bits mismatch, terminate the current search
                if (currentBit != magicCurrentBit)
                {
                    break;
                }

                // Found a matching bit!
                // Progress magic position to next bit
                magicBitPosition++;
                if (magicBitPosition >= 8)
                {
                    magicBitPosition = 0;
                    magicPosition--;
                }

                // If we reached the end of the magic, then we successfully found a full match!
                if (magicPosition < 0)
                {
                    foundMatch = true;
                    break;
                }

                // We didn't find a full match yet, so we also need to progress our search position to the next bit
                bitPosition++;
                if (bitPosition >= 8)
                {
                    bitPosition = 0;
                    searchPosition--;
                }
            }

            if (foundMatch)
            {
                // We found a full match, so calculate end of stream position
                const int footerByteLength = 10;
                long endOfBZ2StreamPosition = searchPosition + footerByteLength;
                if (bitPosition != 7)
                {
                    // BZip2 footer started partway through a byte, and so it will end partway through the last byte.
                    // By the BZip2 specification, the unused bits of the last byte are essentially padding.
                    endOfBZ2StreamPosition++;
                }

                // Return position relative to the start of the data we read
                return (endDataPosition - data.Length) + endOfBZ2StreamPosition;
            }

            // Current search failed to make a full match, so progress to next bit, to search starting from there
            searchStartBitPosition++;
            if (searchStartBitPosition >= 8)
            {
                searchStartBitPosition = 0;
                searchStartPosition--;
            }
        }

        throw new IOException("Failed to find BZip2 footer magic");
    }

    /// <summary>
    /// Finds the end position of a BZ2 stream exactly, given the start and end bounds of the data.
    /// </summary>
    private static long FindEndOfBZ2Stream(IBinaryReader reader, long startOfStreamPosition, long maxEndOfStreamPosition)
    {
        if (startOfStreamPosition >= maxEndOfStreamPosition)
        {
            throw new ArgumentOutOfRangeException(nameof(startOfStreamPosition));
        }

        // Read backwards from the max end of stream position, in up to 256-byte chunks.
        // We want to find the end of nonzero data.
        const int maxChunkSize = 256;
        Span<byte> chunkData = stackalloc byte[maxChunkSize];
        long chunkStartPosition = Math.Max(startOfStreamPosition, maxEndOfStreamPosition - maxChunkSize);
        int chunkSize = (int)(maxEndOfStreamPosition - chunkStartPosition);
        do
        {
            // Read chunk from stream
            reader.Position = chunkStartPosition;
            reader.Stream.Read(chunkData[..chunkSize]);

            // Find first nonzero byte at end of stream
            int position = chunkSize - 1;
            while (position >= 0 && chunkData[position] == 0)
            {
                position--;
            }

            // If we're at nonzero data, then invoke search for footer magic
            if (position >= 0 && chunkData[position] != 0)
            {
                return FindEndOfBZ2Search(reader, chunkStartPosition + position + 1);
            }

            // Move backwards to next chunk
            chunkStartPosition = Math.Max(startOfStreamPosition, chunkStartPosition - maxChunkSize);
        }
        while (chunkStartPosition > startOfStreamPosition);

        throw new IOException("Failed to find nonzero data");
    }

    /// <summary>
    /// Creates a <see cref="GMImage"/> from the image contents stored at the current position of the provided <see cref="IBinaryReader"/>.
    /// </summary>
    /// <param name="reader">Binary reader to read the image data from.</param>
    /// <param name="maxEndOfStreamPosition">
    /// Location where the image stream must end at or before, from within the <see cref="IBinaryReader"/>.
    /// There should only be 0x00 bytes (AKA padding), between the end of the image data and this position.
    /// </param>
    /// <param name="gm2022_5">Whether using GameMaker version 2022.5 or above. Relevant only for BZ2 + QOI format images.</param>
    /// <exception cref="IOException">If no supported texture format is found</exception>
    /// <exception cref="InvalidDataException">Image data fails to parse</exception>
    public static GMImage FromBinaryReader(IBinaryReader reader, long maxEndOfStreamPosition, bool gm2022_5)
    {
        ArgumentNullException.ThrowIfNull(reader);

        // Determine type of image by reading the first 8 bytes
        long startAddress = reader.Position;
        ReadOnlySpan<byte> header = reader.ReadBytes(8);

        // PNG
        if (header.SequenceEqual(MagicPng))
        {
            // There's no overall PNG image length, so we parse image chunks,
            // which do have their own length, until we find the end
            while (true)
            {
                // PNG is big endian, so swap endianness here manually
                uint len = reader.ReadUInt32();
                len = (len >> 16) | (len << 16);
                len = ((len & 0xFF00FF00) >> 8) | ((len & 0x00FF00FF) << 8);

                uint type = reader.ReadUInt32();
                reader.Position += len + 4;
                if (type == 0x444e4549) // 0x444e4549 -> "IEND"
                    break;
            }

            // Calculate length, read entire image to byte array
            long length = reader.Position - startAddress;
            reader.Position = startAddress;
            return FromPng(reader.ReadBytes((int)length));
        }

        // QOI + BZip2
        if (header.StartsWith(MagicBz2Qoi))
        {
            // Skip past (start of) header
            reader.Position = startAddress + 8;

            // Read uncompressed data size, if it exists
            int serializedUncompressedLength = -1;
            int headerSize = 8;
            if (gm2022_5)
            {
                serializedUncompressedLength = reader.ReadInt32();
                headerSize = 12;
            }

            // Find compressed data length, by finding end of BZip2 stream
            long endOfBZ2Stream = FindEndOfBZ2Stream(reader, reader.Position, maxEndOfStreamPosition);
            int compressedLength = (int)(endOfBZ2Stream - (startAddress + headerSize));

            // Get width/height of image from BZ2 header
            int width = header[4] | (header[5] << 8);
            int height = header[6] | (header[7] << 8);

            // Read entire image, *EXCLUDING BZ2 HEADER*, to byte array
            reader.Position = startAddress + headerSize;
            return FromBz2Qoi(reader.ReadBytes(compressedLength), width, height, serializedUncompressedLength);
        }

        // QOI
        if (header.StartsWith(MagicQoi))
        {
            // Read length of data
            uint compressedLength = reader.ReadUInt32();

            // Read entire image to byte array
            reader.Position = startAddress;
            return FromQoi(reader.ReadBytes(12 + (int)compressedLength));
        }

        throw new IOException("Failed to recognize any known image header");
    }

    // Either retrieves the known uncompressed data size, or makes a lowball guess as to what it could be
    private int GetInitialUncompressedBufferCapacity()
    {
        if (_bz2UncompressedSize != -1)
        {
            // We already know the uncompressed size, so use it
            return _bz2UncompressedSize;
        }
        else
        {
            // Make a guess - it's probably at LEAST 2 times larger
            return _data.Length * 2;
        }
    }

    /// <summary>
    /// Creates a <see cref="GMImage"/> of PNG format, wrapping around the provided byte array containing PNG data.
    /// </summary>
    /// <param name="data">Byte array of PNG data.</param>
    /// <param name="verifyHeader">Whether to check that the PNG magic exists or not.</param>
    /// <exception cref="InvalidDataException">Invalid PNG data, or image is too large</exception>
    public static GMImage FromPng(byte[] data, bool verifyHeader = false)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length < 24)
        {
            throw new InvalidDataException("PNG data is too short");
        }
        ReadOnlySpan<byte> span = data.AsSpan();

        // Verify header, if requested
        if (verifyHeader && !span[0..8].SequenceEqual(MagicPng))
        {
            throw new InvalidDataException("PNG header mismatch (not a PNG file)");
        }

        // Calculate width/height from data
        int width = BinaryPrimitives.ReadInt32BigEndian(span[16..20]);
        int height = BinaryPrimitives.ReadInt32BigEndian(span[20..24]);

        // Ensure dimensions are valid
        if (width is < 0 or > MaxImageDimension)
        {
            throw new InvalidDataException($"Width out of range ({width})");
        }
        if (height is < 0 or > MaxImageDimension)
        {
            throw new InvalidDataException($"Height out of range ({height})");
        }

        // Create wrapper image
        return new GMImage(ImageFormat.Png, width, height, data);
    }

    /// <summary>
    /// Creates a <see cref="GMImage"/> of BZ2 + QOI format, wrapping around the provided byte array containing BZ2-compressed data (no header).
    /// </summary>
    /// <param name="compressedData">Compressed BZ2 data, excluding the header.</param>
    /// <param name="width">Width of the image, as provided in BZ2 + QOI header.</param>
    /// <param name="height">Height of the image, as provideed in BZ2 + QOI header.</param>
    /// <param name="uncompressedLength">Length of BZ2 data when fully uncompressed.</param>
    /// <exception cref="InvalidDataException">Invalid BZ2 + QOI data, or image is too large</exception>
    public static GMImage FromBz2Qoi(byte[] compressedData, int width, int height, int uncompressedLength)
    {
        ArgumentNullException.ThrowIfNull(compressedData);

        // Ensure dimensions are valid
        if (width is < 0 or > MaxImageDimension)
        {
            throw new InvalidDataException($"Width out of range ({width})");
        }
        if (height is < 0 or > MaxImageDimension)
        {
            throw new InvalidDataException($"Height out of range ({height})");
        }

        // Create wrapper image
        return new GMImage(ImageFormat.Bz2Qoi, width, height, compressedData)
        {
            _bz2UncompressedSize = uncompressedLength
        };
    }

    /// <summary>
    /// Creates a <see cref="GMImage"/> of QOI format, wrapping around the provided byte array containing QOI data (GameMaker's custom version).
    /// </summary>
    /// <exception cref="InvalidDataException">Invalid QOI data, or image is too large</exception>
    public static GMImage FromQoi(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length < 12)
        {
            throw new InvalidDataException("QOI data is too short");
        }

        // Calculate width/height from data
        ReadOnlySpan<byte> span = data.AsSpan();
        int width = BinaryPrimitives.ReadInt16LittleEndian(span[4..6]);
        int height = BinaryPrimitives.ReadInt16LittleEndian(span[6..8]);

        // Ensure dimensions are valid
        if (width is < 0 or > MaxImageDimension)
        {
            throw new InvalidDataException($"Width out of range ({width})");
        }
        if (height is < 0 or > MaxImageDimension)
        {
            throw new InvalidDataException($"Height out of range ({height})");
        }

        // Create wrapper image
        return new GMImage(ImageFormat.Qoi, width, height, data);
    }

    // Settings to be used for raw data, and when encoding a PNG
    private MagickReadSettings GetMagickRawToPngSettings()
    {
        var settings = new MagickReadSettings()
        {
            Width = Width,
            Height = Height,
            Format = MagickFormat.Bgra,
            Compression = CompressionMethod.NoCompression
        };
        settings.SetDefine(MagickFormat.Png32, "compression-level", 4);
        settings.SetDefine(MagickFormat.Png32, "compression-filter", 5);
        settings.SetDefine(MagickFormat.Png32, "compression-strategy", 2);
        return settings;
    }

    /// <summary>
    /// Saves this image as a PNG file, writing the data to the provided <see cref="Stream"/>.
    /// </summary>
    public void SavePng(Stream stream)
    {
        switch (Format)
        {
            case ImageFormat.RawBgra:
                {
                    // Create image using ImageMagick, and save it as PNG format
                    using var image = new MagickImage(_data, GetMagickRawToPngSettings());
                    image.Alpha(AlphaOption.Set);
                    image.Format = MagickFormat.Png32;
                    image.Write(stream);
                    break;
                }
            case ImageFormat.Png:
                {
                    // Data is already encoded as PNG; just use that
                    stream.Write(_data);
                    break;
                }
            case ImageFormat.Qoi:
                {
                    // Convert to raw image data, and then save that to a PNG
                    GMImage rawImage = QoiConverter.GetImageFromSpan(_data);
                    rawImage.SavePng(stream);
                    break;
                }
            case ImageFormat.Bz2Qoi:
                {
                    GMImage rawImage;
                    
                    using (MemoryStream uncompressedData = new(GetInitialUncompressedBufferCapacity()))
                    {
                        // Decompress BZ2 data
                        using (MemoryStream compressedData = new(_data))
                        {
                            BZip2.Decompress(compressedData, uncompressedData, false);
                        }

                        // Convert to raw image data
                        uncompressedData.Seek(0, SeekOrigin.Begin);
                        rawImage = QoiConverter.GetImageFromStream(uncompressedData);
                    }

                    // Save raw image to PNG
                    rawImage.SavePng(stream);
                    break;
                }
            default:
                throw new InvalidOperationException($"Unknown format {Format}");
        }
    }

    /// <summary>
    /// Returns the same or a new <see cref="GMImage"/>; the result of converting this image to the specified <see cref="ImageFormat"/>.
    /// </summary>
    /// <param name="format">Format to convert to</param>
    /// <param name="sharedStream">Reusable shared <see cref="MemoryStream"/> to be used when compressing with BZ2, as required.</param>
    public GMImage ConvertToFormat(ImageFormat format, MemoryStream sharedStream = null)
    {
        return format switch
        {
            ImageFormat.RawBgra => ConvertToRawBgra(),
            ImageFormat.Png => ConvertToPng(),
            ImageFormat.Qoi => ConvertToQoi(),
            ImageFormat.Bz2Qoi => ConvertToBz2Qoi(sharedStream),
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
    }

    /// <summary>
    /// Returns the same or a new <see cref="GMImage"/>; the result of converting this image to <see cref="ImageFormat.RawBgra"/> format.
    /// </summary>
    public GMImage ConvertToRawBgra()
    {
        switch (Format)
        {
            case ImageFormat.RawBgra:
                {
                    // Already in correct format; no conversion to be done
                    return this;
                }
            case ImageFormat.Png:
                {
                    // Convert image to raw byte array
                    var image = new MagickImage(_data);
                    image.Alpha(AlphaOption.Set);
                    image.Format = MagickFormat.Bgra;
                    image.SetCompression(CompressionMethod.NoCompression);
                    return new GMImage(ImageFormat.RawBgra, Width, Height, image.ToByteArray());
                }
            case ImageFormat.Qoi:
                {
                    // Convert to raw image data
                    return QoiConverter.GetImageFromSpan(_data);
                }
            case ImageFormat.Bz2Qoi:
                {
                    using (MemoryStream uncompressedData = new(GetInitialUncompressedBufferCapacity()))
                    {
                        // Decompress BZ2 data
                        using (MemoryStream compressedData = new(_data))
                        {
                            BZip2.Decompress(compressedData, uncompressedData, false);
                        }

                        // Convert to raw image data
                        uncompressedData.Seek(0, SeekOrigin.Begin);
                        return QoiConverter.GetImageFromStream(uncompressedData);
                    }
                }
        }

        throw new InvalidOperationException($"Unknown source format {Format}");
    }

    /// <summary>
    /// Returns the same or a new <see cref="GMImage"/>; the result of converting this image to <see cref="ImageFormat.Png"/> format.
    /// </summary>
    public GMImage ConvertToPng()
    {
        switch (Format)
        {
            case ImageFormat.RawBgra:
                {
                    // Create image using ImageMagick, and convert it to PNG format
                    using var image = new MagickImage(_data, GetMagickRawToPngSettings());
                    image.Alpha(AlphaOption.Set);
                    image.Format = MagickFormat.Png32;
                    return new GMImage(ImageFormat.Png, Width, Height, image.ToByteArray());
                }
            case ImageFormat.Png:
                {
                    // Already in correct format; no conversion to be done
                    return this;
                }
            case ImageFormat.Qoi:
                {
                    // Convert to raw image data, and then convert that to a PNG
                    GMImage rawImage = QoiConverter.GetImageFromSpan(_data);
                    return rawImage.ConvertToPng();
                }
            case ImageFormat.Bz2Qoi:
                {
                    GMImage rawImage;

                    using (MemoryStream uncompressedData = new(GetInitialUncompressedBufferCapacity()))
                    {
                        // Decompress BZ2 data
                        using (MemoryStream compressedData = new(_data))
                        {
                            BZip2.Decompress(compressedData, uncompressedData, false);
                        }

                        // Convert to raw image data
                        uncompressedData.Seek(0, SeekOrigin.Begin);
                        rawImage = QoiConverter.GetImageFromStream(uncompressedData);
                    }

                    // Convert raw image to PNG
                    return rawImage.ConvertToPng();
                }
        }

        throw new InvalidOperationException($"Unknown source format {Format}");
    }

    /// <summary>
    /// Returns the same or a new <see cref="GMImage"/>; the result of converting this image to <see cref="ImageFormat.Qoi"/> format.
    /// </summary>
    public GMImage ConvertToQoi()
    {
        switch (Format)
        {
            case ImageFormat.RawBgra:
            case ImageFormat.Png:
            case ImageFormat.Bz2Qoi:
                {
                    // Encode image as QOI
                    return new GMImage(ImageFormat.Qoi, Width, Height, QoiConverter.GetArrayFromImage(this, false));
                }
            case ImageFormat.Qoi:
                {
                    // Already in correct format; no conversion to be done
                    return this;
                }
        }

        throw new InvalidOperationException($"Unknown source format {Format}");
    }

    /// <summary>
    /// Compresses the provided QOI data using BZ2, and using the shared <see cref="MemoryStream"/>, if not null.
    /// </summary>
    /// <returns>A new BZ2 + QOI image with the compressed data.</returns>
    private static GMImage CompressQoiData(int width, int height, byte[] qoiData, MemoryStream sharedStream)
    {
        // Compress into new byte array
        byte[] compressed;
        if (sharedStream is not null)
        {
            // Use existing shared stream to compress the data
            using var input = new MemoryStream(qoiData);
            if (sharedStream.Length != 0)
            {
                // Ensure shared stream is at the beginning
                sharedStream.Seek(0, SeekOrigin.Begin);
            }
            BZip2.Compress(input, sharedStream, false, 9);
            compressed = sharedStream.GetBuffer().AsSpan()[..(int)sharedStream.Position].ToArray();
        }
        else
        {
            // Use a new memory stream to compress the data
            using var input = new MemoryStream(qoiData);
            using var output = new MemoryStream();
            BZip2.Compress(input, output, false, 9);
            compressed = output.GetBuffer().AsSpan()[..(int)output.Position].ToArray();
        }

        return new GMImage(ImageFormat.Bz2Qoi, width, height, compressed)
        {
            _bz2UncompressedSize = qoiData.Length
        };
    }

    /// <summary>
    /// Returns the same or a new <see cref="GMImage"/>; the result of converting this image to <see cref="ImageFormat.Bz2Qoi"/> format.
    /// </summary>
    /// <param name="sharedStream">Shared <see cref="MemoryStream"/> to be reused for BZ2 compression, if required.</param>
    public GMImage ConvertToBz2Qoi(MemoryStream sharedStream = null)
    {
        switch (Format)
        {
            case ImageFormat.RawBgra:
            case ImageFormat.Png:
                {
                    // Encode image as QOI, first
                    byte[] data = QoiConverter.GetArrayFromImage(this, false);
                    return CompressQoiData(Width, Height, data, sharedStream);
                }
            case ImageFormat.Qoi:
                {
                    // Already have QOI data, so just compress it
                    return CompressQoiData(Width, Height, _data, sharedStream);
                }
            case ImageFormat.Bz2Qoi:
                {
                    // Already in correct format; no conversion to be done
                    return this;
                }
        }

        throw new InvalidOperationException($"Unknown source format {Format}");
    }

    /// <summary>
    /// Returns the raw BGRA32 pixel data of this image, which can be modified.
    /// </summary>
    /// <remarks>
    /// Only works if the image format is <see cref="ImageFormat.RawBgra"/>; otherwise, you must first convert to that format using <see cref="ConvertToRawBgra"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Image format is not <see cref="ImageFormat.RawBgra"/>.</exception>
    public Span<byte> GetRawImageData()
    {
        if (Format != ImageFormat.RawBgra)
        {
            throw new InvalidOperationException("Image is not in raw format");
        }

        return _data.AsSpan();
    }

    /// <summary>
    /// Writes this image, in its current format (as seen on disk), to the current position of the specified <see cref="BinaryWriter"/>.
    /// </summary>
    /// <remarks>The gm2022_5 parameter is only relevant for images of BZ2 + QOI format.</remarks>
    /// <param name="writer"><see cref="BinaryWriter"/> instance to write to.</param>
    /// <param name="gm2022_5">True if using GameMaker 2022.5 format or above; false otherwise.</param>
    public void WriteToBinaryWriter(BinaryWriter writer, bool gm2022_5)
    {
        switch (Format)
        {
            case ImageFormat.RawBgra:
            case ImageFormat.Png:
            case ImageFormat.Qoi:
                // Data is stored identically to file format, so write it verbatim
                writer.Write(_data);
                break;
            case ImageFormat.Bz2Qoi:
                // Header is missing in this case, so we need to generate it first
                writer.Write(MagicBz2Qoi);
                writer.Write((short)Width);
                writer.Write((short)Height);
                if (gm2022_5)
                {
                    if (_bz2UncompressedSize == -1)
                    {
                        throw new InvalidOperationException("BZ2 uncompressed data size was not set");
                    }
                    writer.Write(_bz2UncompressedSize);
                }
                writer.Write(_data);
                break;
            default:
                throw new InvalidOperationException($"Unknown format {Format}");
        }
    }

    /// <summary>
    /// Converts the image to its byte array/span representation (as seen on disk).
    /// </summary>
    /// <param name="gm2022_5">True if using GameMaker 2022.5 format or above; false otherwise.</param>
    /// <remarks>The gm2022_5 parameter is only relevant for images of BZ2 + QOI format.</remarks>
    public ReadOnlySpan<byte> ToSpan(bool gm2022_5 = false)
    {
        if (Format != ImageFormat.Bz2Qoi)
        {
            // All formats except BZ2 + QOI are stored verbatim, so just return them
            return _data.AsSpan();
        }

        // We need to perform a full write with a BinaryWriter
        using (MemoryStream ms = new(_data.Length + 16))
        {
            using (BinaryWriter bw = new(ms))
            {
                WriteToBinaryWriter(bw, gm2022_5);
            }

            return ms.GetBuffer()[..(int)ms.Position].AsSpan();
        }
    }

    /// <summary>
    /// Returns a new <see cref="MagickImage"/> with the contents of this image.
    /// </summary>
    public MagickImage GetMagickImage()
    {
        switch (Format)
        {
            case ImageFormat.Png:
                {
                    // Parse the PNG data
                    MagickReadSettings settings = new()
                    {
                        ColorSpace = ColorSpace.sRGB,
                        Format = MagickFormat.Png
                    };
                    MagickImage image = new(_data, settings);
                    image.Alpha(AlphaOption.Set);
                    image.Format = MagickFormat.Bgra;
                    image.SetCompression(CompressionMethod.NoCompression);
                    return image;
                }
            case ImageFormat.RawBgra:
                {
                    // Parse the raw data
                    MagickReadSettings settings = new()
                    {
                        Width = Width,
                        Height = Height,
                        Format = MagickFormat.Bgra,
                        Compression = CompressionMethod.NoCompression
                    };
                    MagickImage image = new(_data, settings);
                    image.Alpha(AlphaOption.Set);
                    return image;
                }
            case ImageFormat.Qoi:
            case ImageFormat.Bz2Qoi:
                // Convert to raw data, then parse that
                return ConvertToRawBgra().GetMagickImage();
        }

        throw new InvalidOperationException($"Unknown format {Format}");
    }

    /// <summary>
    /// Creates a new raw format <see cref="GMImage"/> with the contents of the provided <see cref="IMagickImage"/>.
    /// </summary>
    /// <remarks>
    /// This modifies the image format of the provided <see cref="IMagickImage"/> to avoid unnecessary copies.
    /// </remarks>
    public static GMImage FromMagickImage(IMagickImage<byte> image)
    {
        image.Format = MagickFormat.Bgra;
        image.SetCompression(CompressionMethod.NoCompression);
        return new GMImage(ImageFormat.RawBgra, image.Width, image.Height, image.ToByteArray());
    }
}
