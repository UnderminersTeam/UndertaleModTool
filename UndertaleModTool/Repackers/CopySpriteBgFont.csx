//Adapted from original script by Grossley
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

DoLongErrorMessages(false);
EnsureDataLoaded();

// Initialization Start
int progress = 0;

var DataEmbeddedTexturesCount = Data.EmbeddedTextures.Count;
List<int> tex_TargetX = new List<int>();
List<int> tex_TargetY = new List<int>();
List<int> tex_SourceX = new List<int>();
List<int> tex_SourceY = new List<int>();
List<int> tex_SourceWidth = new List<int>();
List<int> tex_SourceHeight = new List<int>();
List<int> tex_TargetWidth = new List<int>();
List<int> tex_TargetHeight = new List<int>();
List<int> tex_BoundingWidth = new List<int>();
List<int> tex_BoundingHeight = new List<int>();
List<int> tex_Frame = new List<int>();
List<int> tex_EmbeddedTextureID = new List<int>();
List<String> tex_Name = new List<String>();
List<String> tex_Type = new List<String>();
List<bool> tex_IsNull = new List<bool>();
List<bool> TexturePageItemsUsed = new List<bool>();

// Initialization End

ScriptMessage("Select the file to copy from");

UndertaleData DonorData;
string DonorDataPath = PromptLoadFile(null, null);
if (DonorDataPath == null)
    throw new System.Exception("The donor data path was not set.");

using (var stream = new FileStream(DonorDataPath, FileMode.Open, FileAccess.Read))
    DonorData = UndertaleIO.Read(stream, warning => ScriptMessage("A warning occured while trying to load " + DonorDataPath + ":\n" + warning));
var DonorDataEmbeddedTexturesCount = DonorData.EmbeddedTextures.Count;

ScriptMessage("Enter the sprite(s)/background(s)/font(s) to copy");

int copiedSpritesCount = 0;
int copiedBackgroundsCount = 0;
int copiedFontsCount = 0;
int copiedAssetsCount = 0;
List<String> splitStringsList = new List<String>();
string abc123 = "";
abc123 = SimpleTextInput("Menu", "Enter names of sprites/backgrounds/fonts", abc123, true);
string[] subs = abc123.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
bool[] SpriteSheetsCopyNeeded = new bool[DonorDataEmbeddedTexturesCount];
bool[] SpriteSheetsUsed = new bool[(DataEmbeddedTexturesCount + DonorDataEmbeddedTexturesCount)];
foreach (var sub in subs)
{
    splitStringsList.Add(sub.Trim());
}
for (int i = 0; i < SpriteSheetsCopyNeeded.Length; i++)
{
    SpriteSheetsCopyNeeded[i] = false;
}
for (int i = 0; i < SpriteSheetsUsed.Length; i++)
{
    SpriteSheetsUsed[i] = false;
}
for (var i = 0; i < DonorDataEmbeddedTexturesCount; i++)
{
    UndertaleEmbeddedTexture texture = new UndertaleEmbeddedTexture();
    texture.TextureData.TextureBlob = DonorData.EmbeddedTextures[i].TextureData.TextureBlob;
    Data.EmbeddedTextures.Add(texture);
    texture.Name = new UndertaleString("Texture " + Data.EmbeddedTextures.IndexOf(texture).ToString());
}
for (var j = 0; j < splitStringsList.Count; j++)
{
    foreach (UndertaleBackground bg in DonorData.Backgrounds)
    {
        if (splitStringsList[j].ToLower() == bg.Name.Content.ToLower())
        {
            UndertaleBackground nativeBG = Data.Backgrounds.ByName(bg.Name.Content);
            UndertaleBackground donorBG = DonorData.Backgrounds.ByName(bg.Name.Content);
            if (nativeBG == null)
            {
                nativeBG = new UndertaleBackground();
                nativeBG.Name = Data.Strings.MakeString(bg.Name.Content);
                Data.Backgrounds.Add(nativeBG);
            }
            nativeBG.Transparent = donorBG.Transparent;
            nativeBG.Smooth = donorBG.Smooth;
            nativeBG.Preload = donorBG.Preload;
            nativeBG.GMS2UnknownAlways2 = donorBG.GMS2UnknownAlways2;
            nativeBG.GMS2TileWidth = donorBG.GMS2TileWidth;
            nativeBG.GMS2TileHeight = donorBG.GMS2TileHeight;
            nativeBG.GMS2OutputBorderX = donorBG.GMS2OutputBorderX;
            nativeBG.GMS2OutputBorderY = donorBG.GMS2OutputBorderY;
            nativeBG.GMS2TileColumns = donorBG.GMS2TileColumns;
            nativeBG.GMS2ItemsPerTileCount = donorBG.GMS2ItemsPerTileCount;
            nativeBG.GMS2TileCount = donorBG.GMS2TileCount;
            nativeBG.GMS2UnknownAlwaysZero = donorBG.GMS2UnknownAlwaysZero;
            nativeBG.GMS2FrameLength = donorBG.GMS2FrameLength;
            nativeBG.GMS2TileIds = donorBG.GMS2TileIds;
            DumpBackground(donorBG);
            copiedBackgroundsCount += 1;
        }
    }
    foreach (UndertaleSprite sprite in DonorData.Sprites)
    {
        if (splitStringsList[j].ToLower() == sprite.Name.Content.ToLower())
        {
            UndertaleSprite nativeSPR = Data.Sprites.ByName(sprite.Name.Content);
            UndertaleSprite donorSPR = DonorData.Sprites.ByName(sprite.Name.Content);
            if (nativeSPR != null)
            {
                nativeSPR.CollisionMasks.Clear();
            }
            else
            {
                nativeSPR = new UndertaleSprite();
                nativeSPR.Name = Data.Strings.MakeString(sprite.Name.Content);
                Data.Sprites.Add(nativeSPR);
            }
            for (var i = 0; i < donorSPR.CollisionMasks.Count; i++)
                nativeSPR.CollisionMasks.Add(new UndertaleSprite.MaskEntry(donorSPR.CollisionMasks[i].Data));
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
    foreach (UndertaleFont fnt in DonorData.Fonts)
    {
        if (splitStringsList[j].ToLower() == fnt.Name.Content.ToLower())
        {
            UndertaleFont nativeFNT = Data.Fonts.ByName(fnt.Name.Content);
            UndertaleFont donorFNT = DonorData.Fonts.ByName(fnt.Name.Content);
            if (nativeFNT == null)
            {
                nativeFNT = new UndertaleFont();
                nativeFNT.Name = Data.Strings.MakeString(fnt.Name.Content);
                Data.Fonts.Add(nativeFNT);
            }
            nativeFNT.Glyphs.Clear();
            nativeFNT.RangeStart = donorFNT.RangeStart;
            nativeFNT.DisplayName = Data.Strings.MakeString(donorFNT.DisplayName.Content);
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
copiedAssetsCount = (copiedFontsCount + copiedBackgroundsCount + copiedSpritesCount);
ScriptMessage(copiedAssetsCount.ToString() + " assets were copied (" + copiedSpritesCount.ToString() + " Sprites, " + copiedBackgroundsCount.ToString() + " Backgrounds, and " + copiedFontsCount.ToString() + " Fonts)");




// Functions

void UpdateProgress(int updateAmount = 0)
{
    UpdateProgressBar(null, "Textures Exported", progress += updateAmount, DonorData.TexturePageItems.Count);
}
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
        tex_Name.Add(sprite.Name.Content);
        tex_Type.Add("spr");
    }
    UpdateProgress(sprite.Textures.Count);
}
void DumpFont(UndertaleFont font)
{
    if (font.Texture != null)
        NotNullHandler(font.Texture);
    else
        NullHandler();
    tex_Frame.Add(0);
    tex_Name.Add(font.Name.Content);
    tex_Type.Add("fnt");
    UpdateProgress(1);
}
void DumpBackground(UndertaleBackground background)
{
    if (background.Texture != null)
        NotNullHandler(background.Texture);
    else
        NullHandler();
    tex_Frame.Add(0);
    tex_Name.Add(background.Name.Content);
    tex_Type.Add("bg");
    UpdateProgress(1);
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
    tex_EmbeddedTextureID.Add(DonorData.EmbeddedTextures.IndexOf(tex.TexturePage));
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
        for (int i = 0; i < sprite.Textures.Count; i++)
        {
            if (sprite.Textures[i]?.Texture != null)
            {
                TexturePageItemsUsed[Data.TexturePageItems.IndexOf(sprite.Textures[i]?.Texture)] = true;
            }
        }
        UpdateProgress();
    }
    foreach (UndertaleBackground bg in Data.Backgrounds)
    {
        if (bg.Texture != null)
        {
            TexturePageItemsUsed[Data.TexturePageItems.IndexOf(bg.Texture)] = true;
        }
        UpdateProgress();
    }
    foreach (UndertaleFont fnt in Data.Fonts)
    {
        if (fnt.Texture != null)
        {
            TexturePageItemsUsed[Data.TexturePageItems.IndexOf(fnt.Texture)] = true;
        }
        UpdateProgress();
    }
}
void SpriteSheetsUsedUpdate()
{
    foreach(UndertaleSprite sprite in Data.Sprites)
    {
        for (int i = 0; i < sprite.Textures.Count; i++)
        {
            if (sprite.Textures[i]?.Texture != null)
            {
                SpriteSheetsUsed[Data.EmbeddedTextures.IndexOf(sprite.Textures[i]?.Texture.TexturePage)] = true;
            }
        }
        UpdateProgress();
    }
    foreach (UndertaleBackground bg in Data.Backgrounds)
    {
        if (bg.Texture != null)
        {
            SpriteSheetsUsed[Data.EmbeddedTextures.IndexOf(bg.Texture.TexturePage)] = true;
        }
        UpdateProgress();
    }
    foreach (UndertaleFont fnt in Data.Fonts)
    {
        if (fnt.Texture != null)
        {
            SpriteSheetsUsed[Data.EmbeddedTextures.IndexOf(fnt.Texture.TexturePage)] = true;
        }
        UpdateProgress();
    }
}
//Unused
/*
void UpdateSpriteSheetsCopyNeeded()
{
    for (var j = 0; j < sprCandidates.Count; j++)
    {
        UndertaleSprite sprite = Data.Sprites.ByName(sprCandidates[j]);
        for (int i = 0; i < sprite.Textures.Count; i++)
        {
            if (sprite.Textures[i]?.Texture != null)
            {
                SpriteSheetsCopyNeeded[Data.EmbeddedTextures.IndexOf(sprite.Textures[i]?.Texture.TexturePage)] = true;
            }
        }
        UpdateProgress();
    }
    for (var j = 0; j < bgCandidates.Count; j++)
    {
        UndertaleBackground bg = Data.Backgrounds.ByName(bgCandidates[j]);
        if (bg.Texture != null)
        {
            SpriteSheetsCopyNeeded[Data.EmbeddedTextures.IndexOf(bg.Texture.TexturePage)] = true;
        }
        UpdateProgress();
    }
    for (var j = 0; j < fntCandidates.Count; j++)
    {
        UndertaleFont fnt = Data.Fonts.ByName(fntCandidates[j]);
        if (fnt.Texture != null)
        {
            SpriteSheetsCopyNeeded[Data.EmbeddedTextures.IndexOf(fnt.Texture.TexturePage)] = true;
        }
        UpdateProgress();
    }
}
*/