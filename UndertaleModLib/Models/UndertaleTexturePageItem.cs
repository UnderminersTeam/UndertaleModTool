using System.ComponentModel;
using System.Drawing;
using UndertaleModLib.Util;

namespace UndertaleModLib.Models
{
    /// <summary>
    /// A texture page item in a data file.
    /// </summary>
    /// <remarks>The way a texture page item works is: <br/>
    /// It renders in a box of size <see cref="BoundingWidth"/> x <see cref="BoundingHeight"/> at some position. <br/>
    /// <see cref="TargetX"/>, <see cref="TargetY"/>, <see cref="TargetWidth"/> and <see cref="TargetHeight"/> are relative to the bounding box,
    /// anything outside of that is just transparent. <br/>
    /// <see cref="SourceX"/>, <see cref="SourceY"/>, <see cref="SourceWidth"/> and <see cref="SourceHeight"/> are part of the texture page which
    /// are drawn over <see cref="TargetX"/>, <see cref="TargetY"/>, <see cref="TargetWidth"/>, <see cref="TargetHeight"/>.</remarks>
    public class UndertaleTexturePageItem : UndertaleNamedResource, INotifyPropertyChanged
    {
        /// <summary>
        /// The name of the texture page item.
        /// </summary>
        //TODO: is not used by game maker, should get repurposed
        public UndertaleString Name { get; set; }

        /// <summary>
        /// The x coordinate of the item on the texture page.
        /// </summary>
        public ushort SourceX { get; set; }

        /// <summary>
        /// The y coordinate of the item on the texture page.
        /// </summary>
        public ushort SourceY { get; set; }

        /// <summary>
        /// The width of the item on the texture page.
        /// </summary>
        public ushort SourceWidth { get; set; }

        /// <summary>
        /// The height of the item on the texture page.
        /// </summary>
        public ushort SourceHeight { get; set; }

        /// <summary>
        /// The x coordinate of the item in the bounding rectangle.
        /// </summary>
        public ushort TargetX { get; set; }

        /// <summary>
        /// The y coordinate of the item in the bounding rectangle.
        /// </summary>
        public ushort TargetY { get; set; }

        /// <summary>
        /// The width of the item in the bounding rectangle.
        /// </summary>
        public ushort TargetWidth { get; set; }

        /// <summary>
        /// The height of the item in the bounding rectangle.
        /// </summary>
        public ushort TargetHeight { get; set; }

        /// <summary>
        /// The width of the bounding rectangle.
        /// </summary>
        public ushort BoundingWidth { get; set; }

        /// <summary>
        /// The height of the bounding rectangle.
        /// </summary>
        public ushort BoundingHeight { get; set; }

        private UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR> _TexturePage = new UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR>();

        /// <summary>
        /// The texture page this item is referencing
        /// </summary>
        public UndertaleEmbeddedTexture TexturePage { get => _TexturePage.Resource; set { _TexturePage.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TexturePage))); } }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <summary>
        /// Replaces the current image of this texture page item to hold a new image.
        /// </summary>
        /// <param name="replaceImage">The new image that shall be applied to this texture page item.</param>
        /// <param name="disposeImage">Whether to dispose <paramref name="replaceImage"/> afterwards.</param>
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
