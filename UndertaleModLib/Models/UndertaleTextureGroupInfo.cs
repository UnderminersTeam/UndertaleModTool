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
        public List<UndertaleResourceById<UndertaleEmbeddedTexture>> TexturePages;
        public List<UndertaleResourceById<UndertaleSprite>> Sprites;
        public List<UndertaleResourceById<UndertaleSprite>> SpineSprites;
        public List<UndertaleResourceById<UndertaleFont>> Fonts;
        public List<UndertaleResourceById<UndertaleBackground>> Tilesets;

        public UndertaleTextureGroupInfo()
        {
            TexturePages = new List<UndertaleResourceById<UndertaleEmbeddedTexture>>();
            Sprites = new List<UndertaleResourceById<UndertaleSprite>>();
            SpineSprites = new List<UndertaleResourceById<UndertaleSprite>>();
            Fonts = new List<UndertaleResourceById<UndertaleFont>>();
            Tilesets = new List<UndertaleResourceById<UndertaleBackground>>();
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(GroupName);
            uint pointer1 = writer.Position; writer.Write(0);
            uint pointer2 = writer.Position; writer.Write(0);
            uint pointer3 = writer.Position; writer.Write(0);
            uint pointer4 = writer.Position; writer.Write(0);
            uint pointer5 = writer.Position; writer.Write(0);

            SeekWritePointer(writer, pointer1);
            foreach (UndertaleResourceById<UndertaleEmbeddedTexture> r in TexturePages)
            {
                writer.Write(r.Serialize(writer));
            }

            SeekWritePointer(writer, pointer2);
            foreach (UndertaleResourceById<UndertaleSprite> r in Sprites)
            {
                writer.Write(r.Serialize(writer));
            }

            SeekWritePointer(writer, pointer3);
            foreach (UndertaleResourceById<UndertaleSprite> r in SpineSprites)
            {
                writer.Write(r.Serialize(writer));
            }

            SeekWritePointer(writer, pointer4);
            foreach (UndertaleResourceById<UndertaleFont> r in Fonts)
            {
                writer.Write(r.Serialize(writer));
            }

            SeekWritePointer(writer, pointer5);
            foreach (UndertaleResourceById<UndertaleBackground> r in Tilesets)
            {
                writer.Write(r.Serialize(writer));
            }
        }

        private void SeekWritePointer(UndertaleWriter writer, uint pointer)
        {
            uint returnTo = writer.Position;
            writer.Position = pointer;
            writer.Write(returnTo);
            writer.Position = returnTo;
        }

        public void Unserialize(UndertaleReader reader)
        {
            GroupName = reader.ReadUndertaleString();
            uint pointer1 = reader.ReadUInt32();
            uint pointer2 = reader.ReadUInt32();
            uint pointer3 = reader.ReadUInt32();
            uint pointer4 = reader.ReadUInt32();
            uint pointer5 = reader.ReadUInt32();

            EnsurePointer(reader, pointer1);
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    UndertaleResourceById<UndertaleEmbeddedTexture> r = new UndertaleResourceById<UndertaleEmbeddedTexture>("TXTR");
                    r.Unserialize(reader, reader.ReadInt32());
                    TexturePages.Add(r);
                }
            }

            EnsurePointer(reader, pointer2);
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    UndertaleResourceById<UndertaleSprite> r = new UndertaleResourceById<UndertaleSprite>("SPRT");
                    r.Unserialize(reader, reader.ReadInt32());
                    Sprites.Add(r);
                }
            }

            EnsurePointer(reader, pointer3);
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    UndertaleResourceById<UndertaleSprite> r = new UndertaleResourceById<UndertaleSprite>("SPRT");
                    r.Unserialize(reader, reader.ReadInt32());
                    SpineSprites.Add(r);
                }
            }

            EnsurePointer(reader, pointer4);
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    UndertaleResourceById<UndertaleFont> r = new UndertaleResourceById<UndertaleFont>("FONT");
                    r.Unserialize(reader, reader.ReadInt32());
                    Fonts.Add(r);
                }
            }

            EnsurePointer(reader, pointer5);
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    UndertaleResourceById<UndertaleBackground> r = new UndertaleResourceById<UndertaleBackground>("BGND");
                    r.Unserialize(reader, reader.ReadInt32());
                    Tilesets.Add(r);
                }
            }
        }

        private void EnsurePointer(UndertaleReader reader, uint pointer)
        {
            Debug.Assert(reader.Position == pointer, "Invalid pointer in TGIN entry");
        }
    }
}
