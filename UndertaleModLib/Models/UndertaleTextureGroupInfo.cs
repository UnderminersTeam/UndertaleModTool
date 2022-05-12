namespace UndertaleModLib.Models
{
    /// <summary>
    /// A texture group info entry in a data file.
    /// </summary>
    /// <remarks>This is a new chunk since Bytecode 17. It's probably related to performance improvements mentioned in the release notes for that runtime.
    /// Here is the specification:
    /// <code>
    /// TGIN structure - likely stands for Texture Group Info
    /// Chunk introduced in 2.2.1 with new texture functions
    /// ---
    ///
    /// Int32 - probably chunk format version number, always 1
    ///
    /// PointerList&lt;T&gt; structure. Each item represents a texture group:
    ///     32-bit string pointer - Name
    ///     32-bit pointer #1
    ///     32-bit pointer #2
    ///     32-bit pointer #3
    ///     32-bit pointer #4
    ///     32-bit pointer #5
    ///
    ///     #1 leads here:
    ///     SimpleList&lt;int&gt; of texture page IDs the group has
    ///
    ///     #2 leads here:
    ///     SimpleList&lt;int&gt; of sprite IDs the group has
    ///
    ///     #3 leads here:
    ///     SimpleList&lt;int&gt; of Spine sprite IDs (normal sprite ID, just this has Spine sprites separated) the group has
    ///
    ///     #4 leads here:
    ///     SimpleList&lt;int&gt; of font IDs the group has
    ///
    ///     #5 leads here:
    ///     SimpleList&lt;int&gt; of tileset IDs the group has
    /// </code>
    /// <see href="https://github.com/krzys-h/UndertaleModTool/wiki/Bytecode-17#tgin-new-chunk"/>.</remarks>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleTextureGroupInfo : UndertaleNamedResource
    {
        /// <summary>
        /// The name of the texture group info entry.
        /// </summary>
        public UndertaleString Name { get; set; }

        /// <summary>
        /// A list of texture pages the group has.
        /// </summary>
        public UndertaleSimpleResourcesList<UndertaleEmbeddedTexture, UndertaleChunkTXTR> TexturePages { get; set; }

        /// <summary>
        /// A list of sprites the group has.
        /// </summary>
        public UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT> Sprites { get; set; }

        /// <summary>
        /// A list of spine sprites the group has.
        /// </summary>
        public UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT> SpineSprites { get; set; }

        /// <summary>
        /// A list of fonts the group has.
        /// </summary>
        public UndertaleSimpleResourcesList<UndertaleFont, UndertaleChunkFONT> Fonts { get; set; }

        /// <summary>
        /// A list of tilesets this group has.
        /// </summary>
        public UndertaleSimpleResourcesList<UndertaleBackground, UndertaleChunkBGND> Tilesets { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="UndertaleTextureGroupInfo"/>.
        /// </summary>
        public UndertaleTextureGroupInfo()
        {
            TexturePages = new UndertaleSimpleResourcesList<UndertaleEmbeddedTexture, UndertaleChunkTXTR>();
            Sprites = new UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>();
            SpineSprites = new UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>();
            Fonts = new UndertaleSimpleResourcesList<UndertaleFont, UndertaleChunkFONT>();
            Tilesets = new UndertaleSimpleResourcesList<UndertaleBackground, UndertaleChunkBGND>();
        }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);

            writer.WriteUndertaleObjectPointer(TexturePages);
            writer.WriteUndertaleObjectPointer(Sprites);
            writer.WriteUndertaleObjectPointer(SpineSprites);
            writer.WriteUndertaleObjectPointer(Fonts);
            writer.WriteUndertaleObjectPointer(Tilesets);

            writer.WriteUndertaleObject(TexturePages);
            writer.WriteUndertaleObject(Sprites);
            writer.WriteUndertaleObject(SpineSprites);
            writer.WriteUndertaleObject(Fonts);
            writer.WriteUndertaleObject(Tilesets);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();

            // Read the pointers
            TexturePages = reader.ReadUndertaleObjectPointer<UndertaleSimpleResourcesList<UndertaleEmbeddedTexture, UndertaleChunkTXTR>>();
            Sprites = reader.ReadUndertaleObjectPointer<UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>>();
            SpineSprites = reader.ReadUndertaleObjectPointer<UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>>();
            Fonts = reader.ReadUndertaleObjectPointer<UndertaleSimpleResourcesList<UndertaleFont, UndertaleChunkFONT>>();
            Tilesets = reader.ReadUndertaleObjectPointer<UndertaleSimpleResourcesList<UndertaleBackground, UndertaleChunkBGND>>();

            // Read the objects, throwing an error if the pointers are invalid
            reader.ReadUndertaleObject(TexturePages);
            reader.ReadUndertaleObject(Sprites);
            reader.ReadUndertaleObject(SpineSprites);
            reader.ReadUndertaleObject(Fonts);
            reader.ReadUndertaleObject(Tilesets);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
