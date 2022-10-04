// ImportGMS2FontData by Dobby233Liu

using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

EnsureDataLoaded();

ScriptMessage(@"ImportGMS2FontData by Dobby233Liu
This script imports the .yy file the GM IDE generates for font assets to your game
Designed for the data IDE v2022.6.0.23 generates
Select a .yy file in the fonts directory of your project
");

string importFile = PromptLoadFile("yy", "GameMaker Studio 2 files (.yy)|*.yy|All files|*");
if (importFile == null)
    throw new ScriptException("The import folder was not set.");

JObject fontData = null;
using (StreamReader file = File.OpenText(importFile))
{
    using (JsonTextReader reader = new JsonTextReader(file))
    {
        fontData = (JObject)JToken.ReadFrom(reader);
    }
}

string fontPath = Path.GetFullPath(importFile);
string fontName = (string)fontData["name"]; // im gonna trust the yy file lmao
string textureSourcePath = Path.Combine(Path.GetDirectoryName(importFile), fontName + ".png");
if (!File.Exists(textureSourcePath))
    throw new ScriptException("The font texture file " + textureSourcePath + " doesn't exist.");

UndertaleFont font = Data.Fonts.ByName(fontName);
if (font == null)
{
    font = new UndertaleFont() {
        Name = Data.Strings.MakeString(fontName)
    };
    Data.Fonts.Add(font);
}

Bitmap textureBitmap = new Bitmap(textureSourcePath);
UndertaleEmbeddedTexture texture = new UndertaleEmbeddedTexture();
texture.Name = Data.Strings.MakeString("Texture " + Data.EmbeddedTextures.Count); // ???
texture.TextureData.TextureBlob = File.ReadAllBytes(textureSourcePath);
Data.EmbeddedTextures.Add(texture);

UndertaleTexturePageItem texturePageItem = new UndertaleTexturePageItem();
texturePageItem.Name = Data.Strings.MakeString("PageItem " + Data.TexturePageItems.Count); // ???
texturePageItem.TexturePage = texture;
texturePageItem.SourceX = 0;
texturePageItem.SourceY = 0;
texturePageItem.SourceWidth = (ushort)textureBitmap.Width;
texturePageItem.SourceHeight = (ushort)textureBitmap.Height;
texturePageItem.TargetX = 0;
texturePageItem.TargetY = 0;
texturePageItem.TargetWidth = (ushort)textureBitmap.Width;
texturePageItem.TargetHeight = (ushort)textureBitmap.Height;
texturePageItem.BoundingWidth = (ushort)textureBitmap.Width;
texturePageItem.BoundingHeight = (ushort)textureBitmap.Height;
Data.TexturePageItems.Add(texturePageItem);

font.Texture = texturePageItem;

if (Data.GMS2_3)
    font.EmSizeIsFloat = true; // forcbliy save as float lmao

font.Glyphs.Clear();

font.DisplayName = Data.Strings.MakeString((string)fontData["fontName"]);
font.EmSize = (uint)fontData["size"];
font.Bold = (bool)fontData["bold"];
font.Italic = (bool)fontData["italic"];
font.Charset = (byte)fontData["charset"];
font.AntiAliasing = (byte)fontData["AntiAlias"];
// ???
font.ScaleX = 1;
font.ScaleY = 1;
if (fontData.ContainsKey("ascender"))
    font.Ascender = (uint)fontData["ascender"];
if (fontData.ContainsKey("ascenderOffset"))
    font.AscenderOffset = (int)fontData["ascenderOffset"];

font.RangeStart = 0;
font.RangeEnd = 0;
foreach (JObject range in fontData["ranges"].Values<JObject>())
{
    var rangeStart = (ushort)range["lower"];
    var rangeEnd = (uint)range["upper"];
    if (font.RangeStart > rangeStart)
        font.RangeStart = rangeStart;
    if (font.RangeEnd > rangeEnd)
        font.RangeEnd = rangeEnd;
}

foreach (KeyValuePair<string, JToken> glyphMeta in (JObject)fontData["glyphs"])
{
    var glyph = (JObject)glyphMeta.Value;
    font.Glyphs.Add(new UndertaleFont.Glyph()
    {
        Character = (ushort)glyph["character"],
        SourceX = (ushort)glyph["x"],
        SourceY = (ushort)glyph["y"],
        SourceWidth = (ushort)glyph["w"],
        SourceHeight = (ushort)glyph["h"],
        Shift = (short)glyph["shift"],
        Offset = (short)glyph["offset"],
    });
}

List<UndertaleFont.Glyph> glyphs = font.Glyphs.ToList();

// I'm literally going to LINQ 100000 times
// and you can't stop me
foreach (JObject kerningPair in fontData["kerningPairs"].Values<JObject>())
{
    var first = (ushort)kerningPair["first"];
    var glyph = glyphs.Find(x => x.Character == first);
    glyph.Kerning.Add(new UndertaleFont.Glyph.GlyphKerning()
    {
        Other = (short)kerningPair["second"],
        Amount = (short)kerningPair["amount"],
    });
}

// Sort glyphs like in UndertaleFontEditor to be safe
glyphs.Sort((x, y) => x.Character.CompareTo(y.Character));
font.Glyphs.Clear();
foreach (UndertaleFont.Glyph glyph in glyphs)
    font.Glyphs.Add(glyph);

ScriptMessage("Import complete.");