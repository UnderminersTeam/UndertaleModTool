using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UndertaleModLib.Models
{
    /**
     * The way this works is:
     * It renders in a box of size BoundingWidth x BoundingHeight at some position.
     * TargetX/Y/W/H is relative to the bounding box, anything outside of that is just transparent.
     * SourceX/Y/W/H is part of TexturePage that is drawn over TargetX/Y/W/H
     */
    public class UndertaleTexturePageItem : UndertaleResource, INotifyPropertyChanged
    {
        private ushort _SourceX;
        private ushort _SourceY;
        private ushort _SourceWidth;
        private ushort _SourceHeight;
        private ushort _TargetX;
        private ushort _TargetY;
        private ushort _TargetWidth;
        private ushort _TargetHeight;
        private ushort _BoundingWidth;
        private ushort _BoundingHeight;
        private UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR> _TexturePage = new UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR>();

        private static int maxSpriteWidth = 0;
        private static int maxSpriteHeight = 0;
        private static UInt32[] pixelBuffer = null; // a memory buffer for the largest possible sprite.
        private static UndertaleEmbeddedTexture embedded = null;
        private static PngBitmapDecoder decoder = null;
        private static WriteableBitmap renderTarget = null;

        public ushort SourceX { get => _SourceX; set { _SourceX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceX")); } }
        public ushort SourceY { get => _SourceY; set { _SourceY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceY")); } }
        public ushort SourceWidth { get => _SourceWidth; set { _SourceWidth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceWidth")); } }
        public ushort SourceHeight { get => _SourceHeight; set { _SourceHeight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceHeight")); } }
        public ushort TargetX { get => _TargetX; set { _TargetX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TargetX")); } }
        public ushort TargetY { get => _TargetY; set { _TargetY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TargetY")); } }
        public ushort TargetWidth { get => _TargetWidth; set { _TargetWidth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TargetWidth")); } }
        public ushort TargetHeight { get => _TargetHeight; set { _TargetHeight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TargetHeight")); } }
        public ushort BoundingWidth { get => _BoundingWidth; set { _BoundingWidth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BoundingWidth")); } }
        public ushort BoundingHeight { get => _BoundingHeight; set { _BoundingHeight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BoundingHeight")); } }
        public UndertaleEmbeddedTexture TexturePage { get => _TexturePage.Resource; set { _TexturePage.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TexturePage")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(SourceX);
            writer.Write(SourceY);
            writer.Write(SourceWidth);
            writer.Write(SourceHeight);
            writer.Write(TargetX);
            writer.Write(TargetY);
            writer.Write(TargetWidth);
            writer.Write(TargetHeight);
            writer.Write(BoundingWidth);
            writer.Write(BoundingHeight);
            writer.Write((short)_TexturePage.SerializeById(writer));
        }

        public void Unserialize(UndertaleReader reader)
        {
            SourceX = reader.ReadUInt16();
            SourceY = reader.ReadUInt16();
            SourceWidth = reader.ReadUInt16();
            SourceHeight = reader.ReadUInt16();
            TargetX = reader.ReadUInt16();
            TargetY = reader.ReadUInt16();
            TargetWidth = reader.ReadUInt16();
            TargetHeight = reader.ReadUInt16();
            BoundingWidth = reader.ReadUInt16();
            BoundingHeight = reader.ReadUInt16();
            _TexturePage = new UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR>();
            _TexturePage.UnserializeById(reader, reader.ReadInt16()); // This one is special as it uses a short instead of int
        }

        public override string ToString()
        {
            return "(" + GetType().Name + ")";
        }

        public void ExportAsPNG(UndertaleData data, string FullPath, string imageName, bool newTarget)
        {
            int exportWidth = BoundingWidth; // sprite.Width
            int exportHeight = BoundingHeight; // sprite.Height
            if (newTarget)
                renderTarget = new WriteableBitmap(exportWidth, exportHeight, 96, 96, PixelFormats.Pbgra32, null);

            // Clear the render target.
            if (pixelBuffer == null)
            {
                foreach (var texPageItem in data.TexturePageItems)
                {
                    if ((int)texPageItem.BoundingWidth > maxSpriteWidth)
                        maxSpriteWidth = texPageItem.BoundingWidth;
                    if ((int)texPageItem.BoundingHeight > maxSpriteHeight)
                        maxSpriteHeight = texPageItem.BoundingHeight;
                }
                pixelBuffer = new UInt32[maxSpriteWidth * maxSpriteHeight]; // Allocate the memory buffer.
            }

            for (int x = 0; x < pixelBuffer.Length; x++)
                pixelBuffer[x] = (UInt32)0U;

            System.Windows.Int32Rect rect = default;
            rect.X = 0;
            rect.Y = 0;
            rect.Width = exportWidth;
            rect.Height = exportHeight;
            renderTarget.WritePixels(rect, pixelBuffer, maxSpriteWidth * 4, 0, 0);

            // Create a Windows bitmap object for the backing texture.
            if (embedded != TexturePage)
            {
                embedded = TexturePage;
                decoder = new PngBitmapDecoder(new MemoryStream(embedded.TextureData.TextureBlob), BitmapCreateOptions.None, BitmapCacheOption.Default);
            }

            // Sanity checks.
            if ((TargetWidth > exportWidth) || (TargetHeight > exportHeight))
                throw new InvalidDataException(imageName + " has too large a texture");

            // Create a bitmap representing that part of the texture page.
            rect.X = SourceX;
            rect.Y = SourceY;
            rect.Width = SourceWidth;
            rect.Height = SourceHeight;
            CroppedBitmap cropped = new CroppedBitmap(decoder.Frames[0], rect);

            // Do we need to scale?
            if ((SourceWidth != TargetWidth) || (SourceHeight != TargetHeight))
            {
                // Yes.
                double scaleX = (double)TargetWidth / SourceWidth;
                double scaleY = (double)TargetHeight / SourceHeight;
                ScaleTransform scale = new ScaleTransform(scaleX, scaleY);
                TransformedBitmap transformed = new TransformedBitmap(cropped, scale);

                // Sanity check, since we're using floating point.
                if ((transformed.PixelWidth != TargetWidth) || (transformed.PixelHeight != TargetHeight))
                {
                    throw new InvalidDataException($"{imageName} has mismatched scaling size: " +
                        $"{SourceWidth} {TargetWidth} {SourceHeight} {TargetHeight} " +
                        $"{exportWidth} {exportHeight} {transformed.PixelWidth} {transformed.PixelHeight} {scaleX} {scaleY}");
                }

                // Copy the transformed pixels.
                rect.X = 0;
                rect.Y = 0;
                rect.Width = TargetWidth;
                rect.Height = TargetHeight;
                transformed.CopyPixels(pixelBuffer, TargetWidth * 4, 0);
            }
            else
            {
                // Copy the cropped bitmap to the buffer.
                cropped.CopyPixels(pixelBuffer, TargetWidth * 4, 0);
            }

            // Overwrite the render target with this.
            rect.X = 0;
            rect.Y = 0;
            rect.Width = TargetWidth;
            rect.Height = TargetHeight;
            renderTarget.WritePixels(rect, pixelBuffer, TargetWidth * 4, TargetX, TargetY);

            // Output the image data.
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTarget));

            var stream = new FileStream(FullPath, FileMode.Create);
            encoder.Save(stream);
            stream.Close();
        }
    }
}
