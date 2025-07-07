using System;
using System.IO;

namespace UndertaleModLib.Util
{
    /// <summary>
    /// A class that converts to and from the GM-custom QOI format.
    /// </summary>
    /// <remarks>Ported over from DogScepter's QOI converter at <see href="https://github.com/colinator27/dog-scepter/"/>.</remarks>
    public static class QoiConverter
    {
        public const int MaxChunkSize = 5; // according to the QOI spec: https://qoiformat.org/qoi-specification.pdf
        public const int HeaderSize = 12;

        private const byte QOI_INDEX = 0x00;
        private const byte QOI_RUN_8 = 0x40;
        private const byte QOI_RUN_16 = 0x60;
        private const byte QOI_DIFF_8 = 0x80;
        private const byte QOI_DIFF_16 = 0xc0;
        private const byte QOI_DIFF_24 = 0xe0;

        private const byte QOI_COLOR = 0xf0;
        private const byte QOI_MASK_2 = 0xc0;
        private const byte QOI_MASK_3 = 0xe0;
        private const byte QOI_MASK_4 = 0xf0;

        /// <summary>
        /// Creates a raw format <see cref="GMImage"/> from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="s">The stream to create the PNG image from.</param>
        /// <returns>The QOI image as a raw format image.</returns>
        /// <exception cref="Exception">If there is an invalid QOIF magic header or there was an error with stride width.</exception>
        public static GMImage GetImageFromStream(Stream s)
        {
            Span<byte> header = stackalloc byte[12];
            s.Read(header);
            int length = header[8] + (header[9] << 8) + (header[10] << 16) + (header[11] << 24);
            byte[] bytes = new byte[12 + length];
            s.Position -= 12;
            s.Read(bytes, 0, bytes.Length);
            return GetImageFromSpan(bytes);
        }

        /// <summary>
        /// Creates a raw format <see cref="GMImage"/> from a <see cref="ReadOnlySpan{TKey}"/> of <see cref="byte"/>s.
        /// </summary>
        /// <param name="bytes">The <see cref="Span{TKey}"/> of <see cref="byte"/>s to create the raw image from.</param>
        /// <returns>The QOI image as a raw format image.</returns>
        /// <exception cref="Exception">If there is an invalid QOIF magic header or there was an error with stride width.</exception>
        public static GMImage GetImageFromSpan(ReadOnlySpan<byte> bytes) => GetImageFromSpan(bytes, out _);

        /// <summary><inheritdoc cref="GetImageFromSpan(System.ReadOnlySpan{byte})"/></summary>
        /// <param name="bytes"><inheritdoc cref="GetImageFromSpan(System.ReadOnlySpan{byte})"/></param>
        /// <param name="length">The total amount of data read from the <see cref="Span{TKey}"/>.</param>
        /// <returns><inheritdoc cref="GetImageFromSpan(System.ReadOnlySpan{byte})"/></returns>
        /// <exception cref="Exception"><inheritdoc cref="GetImageFromSpan(System.ReadOnlySpan{byte})"/></exception>
        public static GMImage GetImageFromSpan(ReadOnlySpan<byte> bytes, out int length)
        {
            ReadOnlySpan<byte> header = bytes[..12];
            if (header[0] != (byte)'f' || header[1] != (byte)'i' || header[2] != (byte)'o' || header[3] != (byte)'q')
                throw new Exception("Invalid little-endian QOIF image magic");

            int width = header[4] + (header[5] << 8);
            int height = header[6] + (header[7] << 8);
            length = header[8] + (header[9] << 8) + (header[10] << 16) + (header[11] << 24);

            ReadOnlySpan<byte> pixelData = bytes.Slice(12, length);

            int pos = 0;
            int run = 0;
            byte r = 0, g = 0, b = 0, a = 255;
            Span<byte> index = stackalloc byte[64 * 4];

            GMImage img = new(width, height);
            Span<byte> rawData = img.GetRawImageData();
            int rawDataLength = rawData.Length;
            for (int rawDataPos = 0; rawDataPos < rawDataLength; rawDataPos += 4)
            {
                if (run > 0)
                {
                    run--;
                }
                else if (pos < pixelData.Length)
                {
                    int b1 = pixelData[pos++];

                    if ((b1 & QOI_MASK_2) == QOI_INDEX)
                    {
                        int indexPos = (b1 ^ QOI_INDEX) << 2;
                        r = index[indexPos];
                        g = index[indexPos + 1];
                        b = index[indexPos + 2];
                        a = index[indexPos + 3];
                    }
                    else if ((b1 & QOI_MASK_3) == QOI_RUN_8)
                    {
                        run = b1 & 0x1f;
                    }
                    else if ((b1 & QOI_MASK_3) == QOI_RUN_16)
                    {
                        int b2 = pixelData[pos++];
                        run = (((b1 & 0x1f) << 8) | b2) + 32;
                    }
                    else if ((b1 & QOI_MASK_2) == QOI_DIFF_8)
                    {
                        r += (byte)(((b1 & 48) << 26 >> 30) & 0xff);
                        g += (byte)(((b1 & 12) << 28 >> 22 >> 8) & 0xff);
                        b += (byte)(((b1 & 3) << 30 >> 14 >> 16) & 0xff);
                    }
                    else if ((b1 & QOI_MASK_3) == QOI_DIFF_16)
                    {
                        int b2 = pixelData[pos++];
                        int merged = b1 << 8 | b2;
                        r += (byte)(((merged & 7936) << 19 >> 27) & 0xff);
                        g += (byte)(((merged & 240) << 24 >> 20 >> 8) & 0xff);
                        b += (byte)(((merged & 15) << 28 >> 12 >> 16) & 0xff);
                    }
                    else if ((b1 & QOI_MASK_4) == QOI_DIFF_24)
                    {
                        int b2 = pixelData[pos++];
                        int b3 = pixelData[pos++];
                        int merged = b1 << 16 | b2 << 8 | b3;
                        r += (byte)(((merged & 1015808) << 12 >> 27) & 0xff);
                        g += (byte)(((merged & 31744) << 17 >> 19 >> 8) & 0xff);
                        b += (byte)(((merged & 992) << 22 >> 11 >> 16) & 0xff);
                        a += (byte)(((merged & 31) << 27 >> 3 >> 24) & 0xff);
                    }
                    else if ((b1 & QOI_MASK_4) == QOI_COLOR)
                    {
                        if ((b1 & 8) != 0)
                            r = pixelData[pos++];
                        if ((b1 & 4) != 0)
                            g = pixelData[pos++];
                        if ((b1 & 2) != 0)
                            b = pixelData[pos++];
                        if ((b1 & 1) != 0)
                            a = pixelData[pos++];
                    }

                    int indexPos2 = ((r ^ g ^ b ^ a) & 63) << 2;
                    index[indexPos2] = r;
                    index[indexPos2 + 1] = g;
                    index[indexPos2 + 2] = b;
                    index[indexPos2 + 3] = a;
                }

                rawData[rawDataPos] = b;
                rawData[rawDataPos + 1] = g;
                rawData[rawDataPos + 2] = r;
                rawData[rawDataPos + 3] = a;
            }

            length += header.Length;
            return img;
        }

        /// <summary>
        /// Creates a QOI image as a byte array from a <see cref="GMImage"/>.
        /// </summary>
        /// <param name="img">The <see cref="GMImage"/> to create the QOI image from.</param>
        /// <returns>A QOI Image as a byte array.</returns>
        /// <exception cref="Exception">If there was an error with stride width.</exception>
        public static byte[] GetArrayFromImage(GMImage img) => GetSpanFromImage(img).ToArray();

        /// <summary>
        /// Creates a QOI image as a <see cref="Span{TKey}"/> from a <see cref="GMImage"/>.
        /// </summary>
        /// <param name="img">The <see cref="GMImage"/> to create the QOI image from.</param>
        /// <returns>A QOI image as a byte array.</returns>
        public static Span<byte> GetSpanFromImage(GMImage img)
        {
            ArgumentNullException.ThrowIfNull(img);

            // Prepare buffer
            int requiredSize = (img.Width * img.Height * MaxChunkSize) + HeaderSize;
            byte[] buffer = new byte[requiredSize];

            // Little-endian QOIF image magic
            buffer[0] = (byte)'f';
            buffer[1] = (byte)'i';
            buffer[2] = (byte)'o';
            buffer[3] = (byte)'q';
            buffer[4] = (byte)(img.Width & 0xff);
            buffer[5] = (byte)((img.Width >> 8) & 0xff);
            buffer[6] = (byte)(img.Height & 0xff);
            buffer[7] = (byte)((img.Height >> 8) & 0xff);

            // Get raw image data, and encode the compressed data as per custom GameMaker format
            GMImage rawImage = img.ConvertToRawBgra();
            Span<byte> rawData = rawImage.GetRawImageData();
            int rawDataLength = rawData.Length;
            int resPos = HeaderSize;
            DebugUtil.Assert(rawDataLength == (img.Width * img.Height * 4), "Unexpected raw image data length");

            byte r = 0, g = 0, b = 0, a = 255;
            int run = 0;
            int v = 0, vPrev = 0xff;
            Span<int> index = stackalloc int[64];
            for (int rawDataPos = 0; rawDataPos < rawDataLength; rawDataPos += 4)
            {
                b = rawData[rawDataPos];
                g = rawData[rawDataPos + 1];
                r = rawData[rawDataPos + 2];
                a = rawData[rawDataPos + 3];

                v = (r << 24) | (g << 16) | (b << 8) | a;
                if (v == vPrev)
                    run++;
                if (run > 0 && (run == 0x2020 || v != vPrev || rawDataPos == rawDataLength - 4))
                {
                    if (run < 33)
                    {
                        run -= 1;
                        buffer[resPos++] = (byte)(QOI_RUN_8 | run);
                    }
                    else
                    {
                        run -= 33;
                        buffer[resPos++] = (byte)(QOI_RUN_16 | (run >> 8));
                        buffer[resPos++] = (byte)run;
                    }
                    run = 0;
                }
                if (v != vPrev)
                {
                    int indexPos = (r ^ g ^ b ^ a) & 63;
                    if (index[indexPos] == v)
                    {
                        buffer[resPos++] = (byte)(QOI_INDEX | indexPos);
                    }
                    else
                    {
                        index[indexPos] = v;

                        int vr = r - ((vPrev >> 24) & 0xff);
                        int vg = g - ((vPrev >> 16) & 0xff);
                        int vb = b - ((vPrev >> 8) & 0xff);
                        int va = a - (vPrev & 0xff);
                        if (vr > -17 && vr < 16 &&
                            vg > -17 && vg < 16 &&
                            vb > -17 && vb < 16 &&
                            va > -17 && va < 16)
                        {
                            if (va == 0 &&
                                vr > -3 && vr < 2 &&
                                vg > -3 && vg < 2 &&
                                vb > -3 && vb < 2)
                            {
                                buffer[resPos++] = (byte)(QOI_DIFF_8 | (vr << 4 & 48) | (vg << 2 & 12) | (vb & 3));
                            }
                            else if (va == 0 &&
                                        vg > -9 && vg < 8 &&
                                        vb > -9 && vb < 8)
                            {
                                buffer[resPos++] = (byte)(QOI_DIFF_16 | (vr & 31));
                                buffer[resPos++] = (byte)((vg << 4 & 240) | (vb & 15));
                            }
                            else
                            {
                                buffer[resPos++] = (byte)(QOI_DIFF_24 | (vr >> 1 & 15));
                                buffer[resPos++] = (byte)((vr << 7 & 128) | (vg << 2 & 124) | (vb >> 3 & 3));
                                buffer[resPos++] = (byte)((vb << 5 & 224) | (va & 31));
                            }
                        }
                        else
                        {
                            buffer[resPos++] = (byte)(QOI_COLOR | (vr != 0 ? 8 : 0) | (vg != 0 ? 4 : 0) | (vb != 0 ? 2 : 0) | (va != 0 ? 1 : 0));
                            if (vr != 0)
                                buffer[resPos++] = r;
                            if (vg != 0)
                                buffer[resPos++] = g;
                            if (vb != 0)
                                buffer[resPos++] = b;
                            if (va != 0)
                                buffer[resPos++] = a;
                        }
                    }
                }

                vPrev = v;
            }

            // Write final length
            int length = resPos - HeaderSize;
            buffer[8] = (byte)(length & 0xff);
            buffer[9] = (byte)((length >> 8) & 0xff);
            buffer[10] = (byte)((length >> 16) & 0xff);
            buffer[11] = (byte)((length >> 24) & 0xff);

            return buffer.AsSpan()[..resPos];
        }
    }
}