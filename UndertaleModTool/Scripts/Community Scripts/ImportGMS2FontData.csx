// ImportGMS2FontData by Dobby233Liu

using System;
using System.IO;
using SkiaSharp;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib;
using UndertaleModLib.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UndertaleModLib.Util;

EnsureDataLoaded();

ScriptMessage(@"ImportGMS2FontData by Dobby233Liu
This script can import GM font asset data to your mod
(Designed for the data IDE v2023.8.2.108 generates)
Select the .yy file of the GM font asset you want to import"
);

string importFile = PromptLoadFile("yy", "GameMaker Studio 2 files (.yy)|*.yy|All files|*");
if (importFile == null)
{
    ScriptError("Import cancelled.");
    return;
}

JObject fontData = null;
using (StreamReader file = File.OpenText(importFile))
{
    using (JsonTextReader reader = new JsonTextReader(file))
    {
        fontData = JObject.Load(reader);
    }
}

string fontPath = Path.GetDirectoryName(importFile);
string yyFilename = Path.GetFileNameWithoutExtension(importFile);
string fontName = (string)fontData["name"] ?? yyFilename;
string fontTexturePath = Path.Combine(fontPath, yyFilename + ".png");
// Failsafe to use font name
if (!File.Exists(fontTexturePath))
    fontTexturePath = Path.Combine(fontPath, fontName + ".png");
// If we still can't find the texture
if (!File.Exists(fontTexturePath))
    throw new ScriptException(
$@"Could not find a texture file for the selected font.
Try renaming the correct texture file to
{yyFilename}.png
and putting it in the same directory as the .yy file."
    );

bool tginExists = Data.TextureGroupInfo is not null;
// Default to putting the font into the default texgroup
UndertaleTextureGroupInfo fontTexGroup;
if (tginExists)
    fontTexGroup = Data.TextureGroupInfo.ByName("Default");
/*
  If true, the script will attempt to add the new font (if any) and the new font glyph texture
  that it created to a texture group
  This was an attempt to get fonts that this script creates appear in a specific 2022.3 game,
  but it was proved unnecessary as the problem was caused by something else
*/
bool attemptToFixFontNotAppearing = tginExists && false; // Data.GM2022_3;

UndertaleFont font = Data.Fonts.ByName(fontName);
if (font == null)
{
    font = new UndertaleFont()
    {
        Name = Data.Strings.MakeString(fontName)
    };
    Data.Fonts.Add(font);

    if (attemptToFixFontNotAppearing)
    {
        if (fontTexGroup == null)
            throw new ScriptException("The default texture group doesn't exist??? (this shouldn't happen)");
        fontTexGroup.Fonts.Add(new UndertaleResourceById<UndertaleFont, UndertaleChunkFONT>() { Resource = font });
    }
}
else if (attemptToFixFontNotAppearing)
{
    // Try to find the texgroup that the font belongs to
    // Scariest LINQ query I've ever written (yet)
    fontTexGroup = Data.TextureGroupInfo
        .Where(t => t.Fonts.Any(f => f.Resource == font))
        .DefaultIfEmpty(fontTexGroup)
        .FirstOrDefault();
    if (fontTexGroup == null)
        throw new ScriptException("Existing font doesn't belong to any texture group AND the default texture group doesn't exist??? (this shouldn't happen)");
    // Failsafe - put it in Default if it's not in there
    if (!fontTexGroup.Fonts.Any(f => f.Resource == font))
        fontTexGroup.Fonts.Add(new UndertaleResourceById<UndertaleFont, UndertaleChunkFONT>() { Resource = font });
}

// Get texture properties
var imgSize = TextureWorker.GetImageSizeFromFile(fontTexturePath);
ushort width = (ushort)imgSize.Width;
ushort height = (ushort)imgSize.Height;

UndertaleEmbeddedTexture texture = new UndertaleEmbeddedTexture();
// ??? Why?
texture.Name = new UndertaleString("Texture " + Data.EmbeddedTextures.Count);
texture.TextureData.TextureBlob = File.ReadAllBytes(fontTexturePath);
Data.EmbeddedTextures.Add(texture);
if (attemptToFixFontNotAppearing)
    fontTexGroup.TexturePages.Add(new UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR>() { Resource = texture });

UndertaleTexturePageItem texturePageItem = new UndertaleTexturePageItem();
// ??? Same as above
texturePageItem.Name = new UndertaleString("PageItem " + Data.TexturePageItems.Count);
texturePageItem.TexturePage = texture;
texturePageItem.SourceX = 0;
texturePageItem.SourceY = 0;
texturePageItem.SourceWidth = width;
texturePageItem.SourceHeight = height;
texturePageItem.TargetX = 0;
texturePageItem.TargetY = 0;
texturePageItem.TargetWidth = width;
texturePageItem.TargetHeight = height;
texturePageItem.BoundingWidth = width;
texturePageItem.BoundingHeight = height;
Data.TexturePageItems.Add(texturePageItem);

font.DisplayName = Data.Strings.MakeString((string)fontData["fontName"]);
font.Texture = texturePageItem;
font.Bold = (bool)fontData["bold"];
font.Italic = (bool)fontData["italic"];
// FIXME: Potentially causes float precision to be lost
font.EmSize = (uint)fontData["size"];
// Save font size as a float in GMS2.3+ (shouldn't UML always save EmSize as a float for GMS2.3+ games??)
font.EmSizeIsFloat = Data.IsVersionAtLeast(2, 3);
font.Charset = (byte)fontData["charset"];
font.AntiAliasing = (byte)fontData["AntiAlias"];
// FIXME: ??? All YY files I've saw don't contain this
font.ScaleX = 1;
font.ScaleY = 1;
// Ascender is GM2022.2+
if (fontData.ContainsKey("ascender"))
    font.Ascender = (uint)fontData["ascender"];
if (fontData.ContainsKey("ascenderOffset"))
    font.AscenderOffset = (int)fontData["ascenderOffset"];
if (fontData.ContainsKey("usesSDF") && (bool)fontData["usesSDF"] && fontData.ContainsKey("sdfSpread"))
    font.SDFSpread = (uint)fontData["sdfSpread"];
if (fontData.ContainsKey("lineHeight"))
    font.LineHeight = (uint)fontData["lineHeight"];

// FIXME: Too complicated?
List<int> charRangesUppersAndLowers = new();
foreach (JObject range in fontData["ranges"].Values<JObject>())
{
    charRangesUppersAndLowers.Add((int)range["upper"]);
    charRangesUppersAndLowers.Add((int)range["lower"]);
}
charRangesUppersAndLowers.Sort();
// FIXME: Check the range by ourselves if ranges don't have it probably
font.RangeStart = (ushort)charRangesUppersAndLowers.DefaultIfEmpty(0).FirstOrDefault();
font.RangeEnd = (uint)charRangesUppersAndLowers.DefaultIfEmpty(0xFFFF).LastOrDefault();

List<UndertaleFont.Glyph> glyphs = new();
// From what I've seen, the keys of the objects in glyphs is just
// the character property of the object itself but in string form 
foreach (KeyValuePair<string, JToken> glyphKVEntry in (JObject)fontData["glyphs"])
{
    var glyphData = (JObject)glyphKVEntry.Value;
    glyphs.Add(new UndertaleFont.Glyph()
    {
        Character = (ushort)glyphData["character"],
        SourceX = (ushort)glyphData["x"],
        SourceY = (ushort)glyphData["y"],
        SourceWidth = (ushort)glyphData["w"],
        SourceHeight = (ushort)glyphData["h"],
        Shift = (short)glyphData["shift"],
        Offset = (short)glyphData["offset"],
    });
}
// Sort glyphs like UndertaleFontEditor to be safe
glyphs.Sort((x, y) => x.Character.CompareTo(y.Character));
font.Glyphs.Clear();
foreach (var glyph in glyphs)
    font.Glyphs.Add(glyph);

glyphs = font.Glyphs.ToList();
// TODO: applyKerning??
foreach (JObject kerningPair in fontData["kerningPairs"]?.Values<JObject>())
{
    // Why do I need to do this. Thanks YoYo
    var first = (ushort)kerningPair["first"];
    var glyph = glyphs.Find(x => x.Character == first);
    glyph.Kerning.Add(new UndertaleFont.Glyph.GlyphKerning()
    {
        Character = (short)kerningPair["second"],
        ShiftModifier = (short)kerningPair["amount"],
    });
}

ScriptMessage("Import complete.");