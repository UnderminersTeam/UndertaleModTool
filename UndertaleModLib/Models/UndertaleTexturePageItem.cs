using System;
using System.ComponentModel;
using System.Drawing;
using UndertaleModLib.Util;

namespace UndertaleModLib.Models
{
    /**
     * The way this works is:
     * It renders in a box of size BoundingWidth x BoundingHeight at some position.
     * TargetX/Y/W/H is relative to the bounding box, anything outside of that is just transparent.
     * SourceX/Y/W/H is part of TexturePage that is drawn over TargetX/Y/W/H
     */
    public class UndertaleTexturePageItem : UndertaleNamedResource, INotifyPropertyChanged
    {
        public UndertaleString Name { get; set; }
        public ushort SourceX { get; set; } // X/Y of item on the texture page
        public ushort SourceY { get; set; }
        public ushort SourceWidth { get; set; } // Width/height of item on the texture page
        public ushort SourceHeight { get; set; }
        public ushort TargetX { get; set; } // X/Y of where to place inside of bound width/height
        public ushort TargetY { get; set; }
        public ushort TargetWidth { get; set; } // Dimensions of where to scale/place inside of bound width/height
        public ushort TargetHeight { get; set; }
        public ushort BoundingWidth { get; set; } // Source sprite/asset dimensions
        public ushort BoundingHeight { get; set; }
        private UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR> _TexturePage = new UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR>();
        public UndertaleEmbeddedTexture TexturePage { get => _TexturePage.Resource; set { _TexturePage.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TexturePage))); } }

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
            try
            {
                return Name.Content + " (" + GetType().Name + ")";
            }
            catch
            {
                Name = new UndertaleString("PageItem Unknown Index");
            }
            return Name.Content + " (" + GetType().Name + ")";
        }

        public void ReplaceTexture(Image replaceImage, bool disposeImage = true)
        {
            Image finalImage = TextureWorker.ResizeImage(replaceImage, SourceWidth, SourceHeight);
            
            // Apply the image to the TexturePage.
            lock (TexturePage.TextureData)
            {
                TextureWorker worker = new TextureWorker();
                Bitmap embImage = worker.GetEmbeddedTexture(TexturePage); // Use SetPixel if needed.

                Graphics g = Graphics.FromImage(embImage);
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.DrawImage(finalImage, SourceX, SourceY);
                g.Dispose();

                TexturePage.TextureData.TextureBlob = TextureWorker.GetImageBytes(embImage);
                worker.Cleanup();
            }

            TargetWidth = (ushort)replaceImage.Width;
            TargetHeight = (ushort)replaceImage.Height;

            // Cleanup.
            finalImage.Dispose();
            if (disposeImage)
                replaceImage.Dispose();
        }
    }
}
