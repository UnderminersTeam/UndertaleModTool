using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleTextureGroupInfo : UndertaleNamedResource
    {
        public UndertaleString Name { get; set; }
        public UndertaleSimpleResourcesList<UndertaleEmbeddedTexture, UndertaleChunkTXTR> TexturePages { get; set; }
        public UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT> Sprites { get; set; }
        public UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT> SpineSprites { get; set; }
        public UndertaleSimpleResourcesList<UndertaleFont, UndertaleChunkFONT> Fonts { get; set; }
        public UndertaleSimpleResourcesList<UndertaleBackground, UndertaleChunkBGND> Tilesets { get; set; }

        public UndertaleTextureGroupInfo()
        {
            TexturePages = new UndertaleSimpleResourcesList<UndertaleEmbeddedTexture, UndertaleChunkTXTR>();
            Sprites = new UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>();
            SpineSprites = new UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>();
            Fonts = new UndertaleSimpleResourcesList<UndertaleFont, UndertaleChunkFONT>();
            Tilesets = new UndertaleSimpleResourcesList<UndertaleBackground, UndertaleChunkBGND>();
        }

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

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
