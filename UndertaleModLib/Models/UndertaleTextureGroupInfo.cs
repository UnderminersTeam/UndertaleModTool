using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleTextureGroupInfo : UndertaleObject
    {
        public UndertaleString GroupName;
        public UndertaleSimpleResourcesList<UndertaleEmbeddedTexture, UndertaleChunkTXTR> TexturePages;
        public UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT> Sprites;
        public UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT> SpineSprites;
        public UndertaleSimpleResourcesList<UndertaleFont, UndertaleChunkFONT> Fonts;
        public UndertaleSimpleResourcesList<UndertaleBackground, UndertaleChunkBGND> Tilesets;

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
            writer.WriteUndertaleString(GroupName);

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
            GroupName = reader.ReadUndertaleString();

            // Read the pointers
            TexturePages = reader.ReadUndertaleObjectPointer<UndertaleSimpleResourcesList<UndertaleEmbeddedTexture, UndertaleChunkTXTR>>();
            Sprites = reader.ReadUndertaleObjectPointer<UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>>();
            SpineSprites = reader.ReadUndertaleObjectPointer<UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>>();
            Fonts = reader.ReadUndertaleObjectPointer<UndertaleSimpleResourcesList<UndertaleFont, UndertaleChunkFONT>>();
            Tilesets = reader.ReadUndertaleObjectPointer<UndertaleSimpleResourcesList<UndertaleBackground, UndertaleChunkBGND>>();

            // Read the objects, throwing an error if the pointers are invalid
            if (reader.ReadUndertaleObject<UndertaleSimpleResourcesList<UndertaleEmbeddedTexture, UndertaleChunkTXTR>>() != TexturePages ||
                reader.ReadUndertaleObject<UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>>() != Sprites ||
                reader.ReadUndertaleObject<UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>>() != SpineSprites ||
                reader.ReadUndertaleObject<UndertaleSimpleResourcesList<UndertaleFont, UndertaleChunkFONT>>() != Fonts ||
                reader.ReadUndertaleObject<UndertaleSimpleResourcesList<UndertaleBackground, UndertaleChunkBGND>>() != Tilesets)
            {
                throw new UndertaleSerializationException("Invalid pointer to SimpleResourcesList");
            }
        }
    }
}
