// ImportGMS2FontData by Dobby233Liu

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib;
using UndertaleModLib.Util;
using System.Text.Json;
using System.Text.Json.Nodes;

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

// GameMaker writes .yy files with trailing commas, which strict JSON doesn't allow.
JsonDocumentOptions yyJsonOptions = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
JsonObject fontData = JsonNode.Parse(File.ReadAllText(importFile), documentOptions: yyJsonOptions).AsObject();

// Newtonsoft's JToken casts accepted numbers written as floats (12.0); System.Text.Json's integer casts throw on them.
double ReadNumber(JsonNode node) => (double)node;

string fontPath = Path.GetDirectoryName(importFile);
string yyFilename = Path.GetFileNameWithoutExtension(importFile);
string fontName = (string)fontData["name"] ?? yyFilename;
string fontTexturePath = Paths.JoinVerifyWithinDirectory(fontPath, yyFilename + ".png");
// Failsafe to use font name
if (!File.Exists(fontTexturePath))
    fontTexturePath = Paths.JoinVerifyWithinDirectory(fontPath, fontName + ".png");
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
(int parsedWidth, int parsedHeight) = TextureWorker.GetImageSizeFromFile(fontTexturePath);
if (parsedWidth == -1 || parsedHeight == -1)
    throw new ScriptException("Invalid font texture image");
ushort width = (ushort)parsedWidth;
ushort height = (ushort)parsedHeight;

UndertaleEmbeddedTexture texture = new UndertaleEmbeddedTexture();
texture.Name = new UndertaleString($"Texture {Data.EmbeddedTextures.Count}");
texture.TextureData.Image = GMImage.FromPng(File.ReadAllBytes(fontTexturePath)); // TODO: generate other formats
Data.EmbeddedTextures.Add(texture);
if (attemptToFixFontNotAppearing)
    fontTexGroup.TexturePages.Add(new UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR>() { Resource = texture });

UndertaleTexturePageItem texturePageItem = new UndertaleTexturePageItem()
{
    Name = new UndertaleString($"PageItem {Data.TexturePageItems.Count}"),
    TexturePage = texture,
    SourceX = 0,
    SourceY = 0,
    SourceWidth = width,
    SourceHeight = height,
    TargetX = 0,
    TargetY = 0,
    TargetWidth = width,
    TargetHeight = height,
    BoundingWidth = width,
    BoundingHeight = height
};
Data.TexturePageItems.Add(texturePageItem);

font.DisplayName = Data.Strings.MakeString((string)fontData["fontName"]);
font.Texture = texturePageItem;
font.Bold = (bool)fontData["bold"];
font.Italic = (bool)fontData["italic"];
// FIXME: Potentially causes float precision to be lost
font.EmSize = (uint)ReadNumber(fontData["size"]);
// Save font size as a float in GMS2.3+ (shouldn't UML always save EmSize as a float for GMS2.3+ games??)
font.EmSizeIsFloat = Data.IsVersionAtLeast(2, 3);
font.Charset = (byte)ReadNumber(fontData["charset"]);
font.AntiAliasing = (byte)ReadNumber(fontData["AntiAlias"]);
// FIXME: ??? All YY files I've saw don't contain this
font.ScaleX = 1;
font.ScaleY = 1;
// Ascender is GM2022.2+
if (fontData.ContainsKey("ascender"))
    font.Ascender = (uint)ReadNumber(fontData["ascender"]);
if (fontData.ContainsKey("ascenderOffset"))
    font.AscenderOffset = (int)ReadNumber(fontData["ascenderOffset"]);
if (fontData.ContainsKey("usesSDF") && (bool)fontData["usesSDF"] && fontData.ContainsKey("sdfSpread"))
    font.SDFSpread = (uint)ReadNumber(fontData["sdfSpread"]);
if (fontData.ContainsKey("lineHeight"))
    font.LineHeight = (uint)ReadNumber(fontData["lineHeight"]);

// FIXME: Too complicated?
List<int> charRangesUppersAndLowers = new();
foreach (JsonNode range in fontData["ranges"].AsArray())
{
    charRangesUppersAndLowers.Add((int)ReadNumber(range["upper"]));
    charRangesUppersAndLowers.Add((int)ReadNumber(range["lower"]));
}
charRangesUppersAndLowers.Sort();
// FIXME: Check the range by ourselves if ranges don't have it probably
font.RangeStart = (ushort)charRangesUppersAndLowers.DefaultIfEmpty(0).FirstOrDefault();
font.RangeEnd = (uint)charRangesUppersAndLowers.DefaultIfEmpty(0xFFFF).LastOrDefault();

List<UndertaleFont.Glyph> glyphs = new();
// From what I've seen, the keys of the objects in glyphs is just
// the character property of the object itself but in string form 
foreach (KeyValuePair<string, JsonNode> glyphKVEntry in fontData["glyphs"].AsObject())
{
    var glyphData = glyphKVEntry.Value;
    glyphs.Add(new UndertaleFont.Glyph()
    {
        Character = (ushort)ReadNumber(glyphData["character"]),
        SourceX = (ushort)ReadNumber(glyphData["x"]),
        SourceY = (ushort)ReadNumber(glyphData["y"]),
        SourceWidth = (ushort)ReadNumber(glyphData["w"]),
        SourceHeight = (ushort)ReadNumber(glyphData["h"]),
        Shift = (short)ReadNumber(glyphData["shift"]),
        Offset = (short)ReadNumber(glyphData["offset"]),
    });
}
// Sort glyphs like UndertaleFontEditor to be safe
glyphs.Sort((x, y) => x.Character.CompareTo(y.Character));
font.Glyphs.Clear();
foreach (var glyph in glyphs)
    font.Glyphs.Add(glyph);

glyphs = font.Glyphs.ToList();
// TODO: applyKerning??
foreach (JsonNode kerningPair in fontData["kerningPairs"]?.AsArray() ?? new JsonArray())
{
    // Why do I need to do this. Thanks YoYo
    var first = (ushort)ReadNumber(kerningPair["first"]);
    var glyph = glyphs.Find(x => x.Character == first);
    glyph.Kerning.Add(new UndertaleFont.Glyph.GlyphKerning()
    {
        Character = (short)ReadNumber(kerningPair["second"]),
        ShiftModifier = (short)ReadNumber(kerningPair["amount"]),
    });
}

ScriptMessage("Import complete.");