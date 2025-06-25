// Made with the help of QuantumV and The United Modders Of Pizza Tower Team
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UndertaleModLib.Util;
using UndertaleModLib.Models;
using System.Collections.Generic;

EnsureDataLoaded();

string fontFolder = GetFolder(FilePath) + "Decompiled" + Path.DirectorySeparatorChar + "fonts" + Path.DirectorySeparatorChar;

if (Directory.Exists(fontFolder))
{
    Directory.Delete(fontFolder, true);
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

public class AssetReference
{
    public string name { get; set; }
    public string path { get; set; }
}

public class GMFont {
    public string resourceType = "GMFont";
    public string resourceVersion = "1.0";
    public string name;
    public AssetReference parent;
    public AssetReference textureGroupId;
	public byte AntiAlias = 0;
	public int applyKerning = 0;
	public uint ascender = 0;
	public int ascenderOffset = 0;
	public bool bold = false;
	public bool canGenerateBitmap = true;
	public uint charset = 0;
	public ushort first = 0;
	public string fontName = "Inconsolata";
	public uint glyphOperations = 0;
	public Dictionary<string, GMFGlyph> glyphs = new Dictionary<string, GMFGlyph>();
	public uint hinting = 0;
	public bool includeTTF = false;
	public uint interpreter = 0;
	public bool italic = false;
	public List<object> kerningPairs = new List<object>();
	public uint last = 0;
	public uint lineHeight = 0;
	public bool maintainGms1Font = false;
	public uint pointRounding = 0;
	public List<GMFRange> ranges = new List<GMFRange>();
	public bool regenerateBitmap = false;
	public string sampleText = "abcdef ABCDEF"
		+ "\n0123456789 .,<>\"'&!?"
		+ "\nthe quick brown fox jumps over the lazy dog"
		+ "\nTHE QUICK BROWN FOX JUMPS OVER THE LAZY DOG"
		+ $"\nDefault character: ▯ (9647)";
	public uint sdfSpread = 8;
	public float size = 12.0f;
	public string styleName = "Regular";
	public string TTFName;
	public bool usesSDF = false;
}

public class GMFGlyph {
	public ushort character;
	public ushort x;
	public ushort y;
	public ushort w;
	public ushort h;
	public short offset;
	public short shift;
}

public class GMFRange {
	public ushort lower = 0;
	public uint upper = 0;
}

var defaultTexGroup = new AssetReference
{
    name = "Default",
    path = "texturegroups/Default"
};
var texGroups = new Dictionary<UndertaleFont, AssetReference>();
foreach (UndertaleTextureGroupInfo group in Data.TextureGroupInfo)
{
    var reference = new AssetReference
    {
        name = group.Name.Content,
        path = "texturegroups/" + group.Name.Content
    };
    foreach (UndertaleResourceById<UndertaleFont, UndertaleChunkFONT> fnt in group.Fonts)
    {
        texGroups.TryAdd(fnt.Resource, reference);
    }
}

TextureWorker worker = new TextureWorker();

SetProgressBar(null, "Exporting fonts...", 0, Data.Fonts.Count);
StartProgressBarUpdater();

await DumpFonts();

await StopProgressBarUpdater();
HideProgressBar();

async Task DumpFonts()
{
    await Task.Run(() => Parallel.ForEach(Data.Fonts, DumpFont));
}

void DumpFont(UndertaleFont font) {
    string folderName = Path.Combine(fontFolder, font.Name.Content);
    Directory.CreateDirectory(folderName);

    string yyPath = Path.Combine(folderName, font.Name.Content + ".yy");

	var fontData = new GMFont{
		name = font.Name.Content,
		fontName = font.DisplayName.Content,
		size = (float)font.EmSize,
		bold = font.Bold,
		italic = font.Italic,
		charset = font.Charset,
		AntiAlias = font.AntiAliasing,
		ascender = font.Ascender,
		ascenderOffset = font.AscenderOffset,
		usesSDF = font.SDFSpread != 0,
		sdfSpread = font.SDFSpread,
		lineHeight = font.LineHeight,
		TTFName = $"${{project_dir}}\\fonts\\{font.Name.Content}\\{font.Name.Content}.png",
		parent = new AssetReference() { name = "Fonts", path = "folders/Fonts.yy" },
        textureGroupId = texGroups.GetValueOrDefault(font, defaultTexGroup),
		first = font.RangeStart,
		last = font.RangeEnd
	};

	if (font.Bold && font.Italic) fontData.styleName = "Bold Italic";
	else if (font.Bold) fontData.styleName = "Bold";
	else if (font.Italic) fontData.styleName = "Italic";

	foreach (UndertaleFont.Glyph glyph in font.Glyphs) {
		fontData.glyphs.TryAdd(glyph.Character.ToString(), new GMFGlyph{
			character = glyph.Character,
			x = glyph.SourceX,
			y = glyph.SourceY,
			w = glyph.SourceWidth,
			h = glyph.SourceHeight,
			offset = glyph.Offset,
			shift = glyph.Shift,
		});
	}

	GMFRange range = null;
	for (uint i = (uint)font.RangeStart; i <= font.RangeEnd; i++) {
		string key = i.ToString();
		if (!fontData.glyphs.ContainsKey(key)) {
			if (range != null) fontData.ranges.Add(range);
			range = null;
			continue;
		}
		if (range == null) {
			range = new GMFRange{
				lower = (ushort)i
			};
		}
		range.upper = i;
	}
	if (range != null) fontData.ranges.Add(range);

    worker.ExportAsPNG(font.Texture, Path.Combine(folderName, font.Name.Content + ".png"));
	
    string exportedyy = JsonConvert.SerializeObject(fontData, Formatting.Indented);
    File.WriteAllText(yyPath, exportedyy);
	
    IncrementProgressParallel();
}