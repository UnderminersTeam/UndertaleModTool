//Adapted from original script by Grossley
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

// Initialization Start

var DataEmbeddedTexturesCount = Data.EmbeddedTextures.Count;
List<int> tex_TargetX = new();
List<int> tex_TargetY = new();
List<int> tex_SourceX = new();
List<int> tex_SourceY = new();
List<int> tex_SourceWidth = new();
List<int> tex_SourceHeight = new();
List<int> tex_TargetWidth = new();
List<int> tex_TargetHeight = new();
List<int> tex_BoundingWidth = new();
List<int> tex_BoundingHeight = new();
List<int> tex_Frame = new();
List<int> tex_EmbeddedTextureID = new();
List<string> tex_Name = new();
List<string> tex_Type = new();
List<bool> tex_IsNull = new();
List<bool> TexturePageItemsUsed = new();

// Initialization End

int copiedSpritesCount = 0;
int copiedBackgroundsCount = 0;
int copiedFontsCount = 0;
int copiedAssetsCount = 0;
List<string> splitStringsList = GetSplitStringsList("sprite(s)/background(s)/font");
bool[] SpriteSheetsCopyNeeded = new bool[DataEmbeddedTexturesCount];
bool[] SpriteSheetsUsed = new bool[(DataEmbeddedTexturesCount + DataEmbeddedTexturesCount)];

int lastTextPage = Data.EmbeddedTextures.Count - 1;

SetProgressBar(null, "Textures Exported", 0, Data.TexturePageItems.Count);
StartProgressBarUpdater();

SyncBinding("EmbeddedTextures, Strings, Backgrounds, Sprites, Fonts, TexturePageItems", true);
await Task.Run(() => 
{
    for (int i = 0; i < SpriteSheetsCopyNeeded.Length; i++)
    {
        SpriteSheetsCopyNeeded[i] = false;
    }
    for (int i = 0; i < SpriteSheetsUsed.Length; i++)
    {
        SpriteSheetsUsed[i] = false;
    }
    for (var i = 0; i < DataEmbeddedTexturesCount; i++)
    {
        UndertaleEmbeddedTexture texture = new UndertaleEmbeddedTexture();
        texture.Name = new UndertaleString("Texture " + ++lastTextPage);
        texture.TextureData.Image = Data.EmbeddedTextures[i].TextureData.Image;
        Data.EmbeddedTextures.Add(texture);
    }
    for (var j = 0; j < splitStringsList.Count; j++)
    {
        int DataBackgroundsLength = Data.Backgrounds.Count;
        for (var i = 0; i < DataBackgroundsLength; i++)
        {
            UndertaleBackground bg = Data.Backgrounds[i];
            if (splitStringsList[j].ToLower() == bg.Name.Content.ToLower())
            {
                UndertaleBackground donorBG = Data.Backgrounds.ByName(bg.Name.Content);
                UndertaleBackground nativeBG = new UndertaleBackground();
                nativeBG.Name = Data.Strings.MakeString(bg.Name.Content + "_Copy");
                Data.Backgrounds.Add(nativeBG);
                nativeBG.Transparent = donorBG.Transparent;
                nativeBG.Smooth = donorBG.Smooth;
                nativeBG.Preload = donorBG.Preload;
                nativeBG.GMS2VersionNumber = donorBG.GMS2VersionNumber;
                nativeBG.GMS2TileWidth = donorBG.GMS2TileWidth;
                nativeBG.GMS2TileHeight = donorBG.GMS2TileHeight;
                nativeBG.GMS2OutputBorderX = donorBG.GMS2OutputBorderX;
                nativeBG.GMS2OutputBorderY = donorBG.GMS2OutputBorderY;
                nativeBG.GMS2TileColumns = donorBG.GMS2TileColumns;
                nativeBG.GMS2ItemsPerTileCount = donorBG.GMS2ItemsPerTileCount;
                nativeBG.GMS2TileCount = donorBG.GMS2TileCount;
                // FIXME: this doesn't properly work due to the cross-asset references required, but usually this doesn't matter...
                nativeBG.GMS2ExportedSprite = null;
                nativeBG.GMS2FrameLength = donorBG.GMS2FrameLength;
                nativeBG.GMS2TileIds = donorBG.GMS2TileIds;
                DumpBackground(donorBG);
                copiedBackgroundsCount += 1;
            }
        }
        int DataSpritesLength = Data.Sprites.Count;
        for (var k = 0; k < DataSpritesLength; k++)
        {
            UndertaleSprite sprite = Data.Sprites[k];
            if (splitStringsList[j].ToLower() == sprite.Name.Content.ToLower())
            {
                UndertaleSprite donorSPR = Data.Sprites.ByName(sprite.Name.Content);
                UndertaleSprite nativeSPR = new UndertaleSprite();
                nativeSPR.Name = Data.Strings.MakeString(sprite.Name.Content + "_Copy");
                Data.Sprites.Add(nativeSPR);
                for (var i = 0; i < donorSPR.CollisionMasks.Count; i++)
                    nativeSPR.CollisionMasks.Add(new UndertaleSprite.MaskEntry(
                        donorSPR.CollisionMasks[i].Data,
                        donorSPR.CollisionMasks[i].Width,
                        donorSPR.CollisionMasks[i].Height));
                nativeSPR.Width = donorSPR.Width;
                nativeSPR.Height = donorSPR.Height;
                nativeSPR.MarginLeft = donorSPR.MarginLeft;
                nativeSPR.MarginRight = donorSPR.MarginRight;
                nativeSPR.MarginBottom = donorSPR.MarginBottom;
                nativeSPR.MarginTop = donorSPR.MarginTop;
                nativeSPR.Transparent = donorSPR.Transparent;
                nativeSPR.Smooth = donorSPR.Smooth;
                nativeSPR.Preload = donorSPR.Preload;
                nativeSPR.BBoxMode = donorSPR.BBoxMode;
                nativeSPR.OriginX = donorSPR.OriginX;
                nativeSPR.OriginY = donorSPR.OriginY;

                // Special sprite types (always used in GMS2)
                nativeSPR.SVersion = donorSPR.SVersion;
                nativeSPR.GMS2PlaybackSpeed = donorSPR.GMS2PlaybackSpeed;
                nativeSPR.IsSpecialType = donorSPR.IsSpecialType;
                nativeSPR.SpineVersion = donorSPR.SpineVersion;
                nativeSPR.SpineJSON = donorSPR.SpineJSON;
                nativeSPR.SpineAtlas = donorSPR.SpineAtlas;
                nativeSPR.SWFVersion = donorSPR.SWFVersion;

                //Possibly will break
                nativeSPR.SepMasks = donorSPR.SepMasks;
                nativeSPR.SSpriteType = donorSPR.SSpriteType;
                nativeSPR.GMS2PlaybackSpeedType = donorSPR.GMS2PlaybackSpeedType;
                nativeSPR.SpineTextures = donorSPR.SpineTextures;
                nativeSPR.YYSWF = donorSPR.YYSWF;
                nativeSPR.V2Sequence = donorSPR.V2Sequence;
                nativeSPR.V3NineSlice = donorSPR.V3NineSlice;

                DumpSprite(donorSPR);
                copiedSpritesCount += 1;
            }
        }
        int DataFontsLength = Data.Fonts.Count;
        for (var i = 0; i < DataFontsLength; i++)
        {
            UndertaleFont fnt = Data.Fonts[i];
            if (splitStringsList[j].ToLower() == fnt.Name.Content.ToLower())
            {
                UndertaleFont donorFNT = Data.Fonts.ByName(fnt.Name.Content);
                UndertaleFont nativeFNT = new UndertaleFont();
                nativeFNT.Name = Data.Strings.MakeString(fnt.Name.Content + "_Copy");
                Data.Fonts.Add(nativeFNT);
                nativeFNT.Glyphs.Clear();
                nativeFNT.RangeStart = donorFNT.RangeStart;
                nativeFNT.DisplayName = Data.Strings.MakeString(donorFNT.DisplayName.Content + "_Copy");
                nativeFNT.EmSize = donorFNT.EmSize;
                nativeFNT.Bold = donorFNT.Bold;
                nativeFNT.Italic = donorFNT.Italic;
                nativeFNT.Charset = donorFNT.Charset;
                nativeFNT.AntiAliasing = donorFNT.AntiAliasing;
                nativeFNT.ScaleX = donorFNT.ScaleX;
                nativeFNT.ScaleY = donorFNT.ScaleY;
                foreach (UndertaleFont.Glyph glyph in donorFNT.Glyphs)
                {
                    UndertaleFont.Glyph glyph_new = new UndertaleFont.Glyph();
                    glyph_new.Character = glyph.Character;
                    glyph_new.SourceX = glyph.SourceX;
                    glyph_new.SourceY = glyph.SourceY;
                    glyph_new.SourceWidth = glyph.SourceWidth;
                    glyph_new.SourceHeight = glyph.SourceHeight;
                    glyph_new.Shift = glyph.Shift;
                    glyph_new.Offset = glyph.Offset;
                    nativeFNT.Glyphs.Add(glyph_new);
                }
                nativeFNT.RangeEnd = donorFNT.RangeEnd;
                DumpFont(donorFNT);
                copiedFontsCount += 1;
            }
        }
    }
    for (var i = 0; i < tex_IsNull.Count; i++)
    {
        if (tex_IsNull[i] == false)
        {
            UndertaleTexturePageItem texturePageItem = new UndertaleTexturePageItem();
            texturePageItem.TargetX = (ushort)tex_TargetX[i];
            texturePageItem.TargetY = (ushort)tex_TargetY[i];
            texturePageItem.TargetWidth = (ushort)tex_TargetWidth[i];
            texturePageItem.TargetHeight = (ushort)tex_TargetHeight[i];
            texturePageItem.SourceX = (ushort)tex_SourceX[i];
            texturePageItem.SourceY = (ushort)tex_SourceY[i];
            texturePageItem.SourceWidth = (ushort)tex_SourceWidth[i];
            texturePageItem.SourceHeight = (ushort)tex_SourceHeight[i];
            texturePageItem.BoundingWidth = (ushort)tex_BoundingWidth[i];
            texturePageItem.BoundingHeight = (ushort)tex_BoundingHeight[i];
            texturePageItem.TexturePage = Data.EmbeddedTextures[(DataEmbeddedTexturesCount + tex_EmbeddedTextureID[i])];
            Data.TexturePageItems.Add(texturePageItem);
            texturePageItem.Name = new UndertaleString("PageItem " + Data.TexturePageItems.IndexOf(texturePageItem).ToString());
            if (tex_Type[i].Equals("bg"))
            {
                UndertaleBackground background = Data.Backgrounds.ByName(tex_Name[i]);
                background.Texture = texturePageItem;
            }
            else if (tex_Type[i].Equals("fnt"))
            {
                UndertaleFont font = Data.Fonts.ByName(tex_Name[i]);
                font.Texture = texturePageItem;
            }
            else
            {
                int frame = tex_Frame[i];
                UndertaleSprite sprite = Data.Sprites.ByName(tex_Name[i]);
                UndertaleSprite.TextureEntry texentry = new UndertaleSprite.TextureEntry();
                texentry.Texture = texturePageItem;
                if (frame > sprite.Textures.Count - 1)
                {
                    while (frame > sprite.Textures.Count - 1)
                    {
                        sprite.Textures.Add(texentry);
                    }
                }
                else
                {
                    sprite.Textures[frame] = texentry;
                }
            }
        }
        else
        {
            if (tex_Type[i] == "spr")
            {
                UndertaleSprite.TextureEntry texentry = new UndertaleSprite.TextureEntry();
                texentry.Texture = null;
                Data.Sprites.ByName(tex_Name[i]).Textures.Add(texentry);
            }
            if (tex_Type[i] == "bg")
            {
                Data.Backgrounds.ByName(tex_Name[i]).Texture = null;
            }
            if (tex_Type[i] == "fnt")
            {
                Data.Fonts.ByName(tex_Name[i]).Texture = null;
            }
        }
    }
    SpriteSheetsUsedUpdate();
    RemoveUnusedSpriteSheets();
    TexturePageItemsUsedUpdate();
    RemoveUnusedTexturePageItems();
});
DisableAllSyncBindings();

await StopProgressBarUpdater();
HideProgressBar();
copiedAssetsCount = (copiedFontsCount + copiedBackgroundsCount + copiedSpritesCount);
ScriptMessage($"{copiedAssetsCount} assets were copied ({copiedSpritesCount} Sprites, {copiedBackgroundsCount} Backgrounds, and {copiedFontsCount} Fonts)");

// Functions

void RemoveUnusedTexturePageItems()
{
    for (int i = (TexturePageItemsUsed.Count - 1); i > -1; i--)
    {
        if (TexturePageItemsUsed[i] == false)
        {
            Data.TexturePageItems.Remove(Data.TexturePageItems[i]);
        }
    }
    foreach (UndertaleTexturePageItem texture in Data.TexturePageItems)
    {
        texture.Name = new UndertaleString("PageItem " + Data.TexturePageItems.IndexOf(texture).ToString());
    }
}
void RemoveUnusedSpriteSheets()
{
    for (int i = (SpriteSheetsUsed.Length - 1); i > -1; i--)
    {
        if (SpriteSheetsUsed[i] == false)
        {
            Data.EmbeddedTextures.Remove(Data.EmbeddedTextures[i]);
        }
    }
    foreach (UndertaleEmbeddedTexture texture in Data.EmbeddedTextures)
    {
        texture.Name = new UndertaleString("Texture " + Data.EmbeddedTextures.IndexOf(texture).ToString());
    }
}
void DumpSprite(UndertaleSprite sprite)
{
    for (int i = 0; i < sprite.Textures.Count; i++)
    {
        if (sprite.Textures[i]?.Texture != null)
            NotNullHandler(sprite.Textures[i].Texture);
        else
            NullHandler();
        tex_Frame.Add(i);
        tex_Name.Add(sprite.Name.Content + "_Copy");
        tex_Type.Add("spr");
    }
    AddProgress(sprite.Textures.Count);
}
void DumpFont(UndertaleFont font)
{
    if (font.Texture != null)
        NotNullHandler(font.Texture);
    else
        NullHandler();
    tex_Frame.Add(0);
    tex_Name.Add(font.Name.Content + "_Copy");
    tex_Type.Add("fnt");
    IncrementProgress();
}
void DumpBackground(UndertaleBackground background)
{
    if (background.Texture != null)
        NotNullHandler(background.Texture);
    else
        NullHandler();
    tex_Frame.Add(0);
    tex_Name.Add(background.Name.Content + "_Copy");
    tex_Type.Add("bg");
    IncrementProgress();
}
void NullHandler()
{
    tex_TargetX.Add(-16000);
    tex_TargetY.Add(-16000);
    tex_SourceWidth.Add(-16000);
    tex_SourceHeight.Add(-16000);
    tex_SourceX.Add(-16000);
    tex_SourceY.Add(-16000);
    tex_TargetWidth.Add(-16000);
    tex_TargetHeight.Add(-16000);
    tex_BoundingWidth.Add(-16000);
    tex_BoundingHeight.Add(-16000);
    tex_EmbeddedTextureID.Add(-16000);
    tex_IsNull.Add(true);
}
void NotNullHandler(UndertaleTexturePageItem tex)
{
    tex_TargetX.Add(tex.TargetX);
    tex_TargetY.Add(tex.TargetY);
    tex_SourceWidth.Add(tex.SourceWidth);
    tex_SourceHeight.Add(tex.SourceHeight);
    tex_SourceX.Add(tex.SourceX);
    tex_SourceY.Add(tex.SourceY);
    tex_TargetWidth.Add(tex.TargetWidth);
    tex_TargetHeight.Add(tex.TargetHeight);
    tex_BoundingWidth.Add(tex.BoundingWidth);
    tex_BoundingHeight.Add(tex.BoundingHeight);
    tex_EmbeddedTextureID.Add(Data.EmbeddedTextures.IndexOf(tex.TexturePage));
    tex_IsNull.Add(false);
}
void TexturePageItemsUsedUpdate()
{
    foreach (UndertaleTexturePageItem texture in Data.TexturePageItems)
    {
        TexturePageItemsUsed.Add(false);
    }
    foreach(UndertaleSprite sprite in Data.Sprites)
    {
        if (sprite is null)
            continue;
        for (int i = 0; i < sprite.Textures.Count; i++)
        {
            if (sprite.Textures[i]?.Texture != null)
            {
                TexturePageItemsUsed[Data.TexturePageItems.IndexOf(sprite.Textures[i]?.Texture)] = true;
            }
        }
    }
    foreach (UndertaleBackground bg in Data.Backgrounds)
    {
        if (bg is null)
            continue;
        if (bg.Texture != null)
        {
            TexturePageItemsUsed[Data.TexturePageItems.IndexOf(bg.Texture)] = true;
        }
    }
    foreach (UndertaleFont fnt in Data.Fonts)
    {
        if (fnt is null)
            continue;
        if (fnt.Texture != null)
        {
            TexturePageItemsUsed[Data.TexturePageItems.IndexOf(fnt.Texture)] = true;
        }
    }
}
void SpriteSheetsUsedUpdate()
{
    foreach(UndertaleSprite sprite in Data.Sprites)
    {
        if (sprite is null)
            continue;
        for (int i = 0; i < sprite.Textures.Count; i++)
        {
            if (sprite.Textures[i]?.Texture != null)
            {
                SpriteSheetsUsed[Data.EmbeddedTextures.IndexOf(sprite.Textures[i]?.Texture.TexturePage)] = true;
            }
        }
    }
    foreach (UndertaleBackground bg in Data.Backgrounds)
    {
        if (bg is null)
            continue;
        if (bg.Texture != null)
        {
            SpriteSheetsUsed[Data.EmbeddedTextures.IndexOf(bg.Texture.TexturePage)] = true;
        }
    }
    foreach (UndertaleFont fnt in Data.Fonts)
    {
        if (fnt is null)
            continue;
        if (fnt.Texture != null)
        {
            SpriteSheetsUsed[Data.EmbeddedTextures.IndexOf(fnt.Texture.TexturePage)] = true;
        }
    }
}

List<string> GetSplitStringsList(string assetType)
{
    ScriptMessage("Enter the " + assetType + "(s) to copy");
    List<string> splitStringsList = new List<string>();
    string InputtedText = "";
    InputtedText = SimpleTextInput("Menu", "Enter the name(s) of the " + assetType + "(s)", InputtedText, true);
    string[] IndividualLineArray = InputtedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    foreach (var OneLine in IndividualLineArray)
    {
        splitStringsList.Add(OneLine.Trim());
    }
    return splitStringsList;
}
