// By YarTom
// Converts font glyphs into vector ones and creates font in TTF format
// Tested on Undertale And Deltarune

using System.Text;
using System;
using System.IO;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using System.Linq;
using ImageMagick;

EnsureDataLoaded();

if (Data.Fonts.Count == 0)
{
    ScriptError("No fonts to export");
    return;
}

string gameDisplayName = Data.GeneralInfo.DisplayName.Content;
bool undertale = (gameDisplayName.ToUpper().Contains("NXTALE") || gameDisplayName.ToUpper().Contains("UNDERTALE"));
bool deltarune = gameDisplayName.ToUpper().Contains("DELTARUNE");

bool keepFontNames = ScriptQuestion("Do you want to keep real names of fonts (Like 8bitoperatorJVE.ttf)? If there is two or more fonts with the same name, script will export only one!");

bool exportJapanFonts = true;
if (undertale || deltarune)
    exportJapanFonts = ScriptQuestion("Do you want to export japan fonts as well?");

string fntFolder = PromptChooseDirectory();
if (!Directory.Exists(fntFolder))
{
    return;
}

TextureWorker worker = new();
object fileWriteLock = new();

SetProgressBar(null, "Fonts", 0, Data.Fonts.Count);
StartProgressBarUpdater();

await DumpFonts();

async Task DumpFonts()
{
    await Task.Run(() => Parallel.ForEach(Data.Fonts, DumpFont));
}

await StopProgressBarUpdater();
HideProgressBar();

void DumpFont(UndertaleFont font)
{
    string name = font.Name.Content;
    string displayName = font.DisplayName.Content.Replace(" (DO NOT EDIT IN GAMEMAKER, Japanese characters will disappear)", "").Replace(".ttf", "");

    if (!exportJapanFonts && name.Contains("_ja_"))
        return;

    using var fontTexture = worker.GetTextureFor(font.Texture, name);

    bool bold = font.Bold;
    bool italic = font.Italic;

    string description = $"Exported from \"{Data.GeneralInfo.DisplayName.Content}\" with UndertaleModTool";

    PixelTtfBuilder builder = new(16, displayName, description: description, bold: bold, italic: italic);

    // This may cause some problems if the font doesnt have "A" glyph,
    // but where have you ever seen a font without letter "A"?
    var yOffset = font.Glyphs.Where(g => (char)g.Character == 'A').ToList()[0].SourceHeight;

    foreach (var g in font.Glyphs)
    {
        ushort chr = g.Character;
        ushort sx = g.SourceX;
        ushort sy = g.SourceY;
        ushort sw = g.SourceWidth;
        ushort sh = g.SourceHeight;
        short shift = g.Shift;

        using (var glyphImg = fontTexture.Clone())
        {
            glyphImg.Crop(new MagickGeometry(sx, sy, sw, sh));

            byte[] byteArray = glyphImg.GetPixels().ToByteArray(PixelMapping.RGBA);

            var contours = GenerateContourFromArray(byteArray, sw, sh);

            foreach (var contour in contours)
            {
                for (int i = 0; i < contour.Count; i++)
                {
                    var p = contour[i];
                    // Minus because in the image y = 0 is at the top, while in TTF its at the bottom
                    // yOffset is needed only to raise the glyph above y = 0.
                    // It works fine without it, but just in case.
                    contour[i] = new Point(p.X, (short)(-p.Y + yOffset));
                }
            }

            builder.AddGlyph((char)chr, contours, (ushort)shift);
        }
    }

    byte[] data = builder.Build();

    var path = $"{fntFolder}/{name}.ttf";
    if (keepFontNames)
        path = $"{fntFolder}/{displayName.Replace(" ", "")}.ttf";

    lock (fileWriteLock)
    {
        if (!File.Exists(path))
            File.WriteAllBytes(path, data);
    }
    
    IncrementProgressParallel();
}

#region Convert raster glyphs to vector ones

List<List<Point>> GenerateContourFromArray(byte[] array, int width, int height)
{
    List<List<Point>> result = new();

    bool IsOpaque(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return false;

        return array[(y * width + x) * 4 + 3] >= 127;
    }

    var edges = new Dictionary<Point, List<Point>>();

    void AddEdge(Point from, Point to)
    {
        if (!edges.TryGetValue(from, out var list))
        {
            list = new List<Point>();
            edges[from] = list;
        }

        list.Add(to);
    }

    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            if (!IsOpaque(x, y))
                continue;

            // Top border
            if (!IsOpaque(x, y - 1))
                AddEdge(new Point((short)x, (short)y), new Point((short)(x + 1), (short)y));

            // Right border
            if (!IsOpaque(x + 1, y))
                AddEdge(new Point((short)(x + 1), (short)y), new Point((short)(x + 1), (short)(y + 1)));

            // Bottom border
            if (!IsOpaque(x, y + 1))
                AddEdge(new Point((short)(x + 1), (short)(y + 1)), new Point((short)x, (short)(y + 1)));

            // Left border
            if (!IsOpaque(x - 1, y))
                AddEdge(new Point((short)x, (short)(y + 1)), new Point((short)x, (short)y));
        }
    }

    while (edges.Count > 0)
    {
        var first = edges.First();

        Point start = first.Key;
        Point current = start;

        List<Point> contour = new();

        Point previousDirection = new Point(1, 0);

        do
        {
            contour.Add(current);

            if (!edges.TryGetValue(current, out var nextPoints) || nextPoints.Count == 0)
                break;

            Point next;

            if (nextPoints.Count == 1)
                next = nextPoints[0];
            else
                next = ChooseNext(current, previousDirection, nextPoints);

            nextPoints.Remove(next);

            if (nextPoints.Count == 0)
                edges.Remove(current);

            previousDirection = new Point((short)(next.X - current.X), (short)(next.Y - current.Y));

            current = next;
        }
        while (current != start);

        if (contour.Count >= 3 && current == start)
        {
            result.Add(SimplifyContour(contour));
        }
    }

    return result;

    static Point ChooseNext(
        Point current,
        Point previousDirection,
        List<Point> candidates)
    {
        Point Right(Point d) => new Point((short)-d.Y, (short)d.X);
        Point Left(Point d)  => new Point((short)d.Y, (short)-d.X);
        Point Back(Point d)  => new Point((short)-d.X, (short)-d.Y);

        Point[] preferredDirections =
        {
            Right(previousDirection),
            previousDirection,
            Left(previousDirection),
            Back(previousDirection)
        };

        foreach (var direction in preferredDirections)
            foreach (var candidate in candidates)
                if (candidate.X - current.X == direction.X && candidate.Y - current.Y == direction.Y)
                    return candidate;

        return candidates[0];
    }
}

static List<Point> SimplifyContour(List<Point> contour)
{
    if (contour.Count <= 4)
        return contour;

    List<Point> simplified = new();

    for (int i = 0; i < contour.Count; i++)
    {
        Point prev = contour[(i - 1 + contour.Count) % contour.Count];

        Point current = contour[i];

        Point next = contour[(i + 1) % contour.Count];

        int dirx1 = current.X - prev.X;
        int diry1 = current.Y - prev.Y;

        int dirx2 = next.X - current.X;
        int diry2 = next.Y - current.Y;

        if (dirx1 != dirx2 || diry1 != diry2)
        {
            simplified.Add(current);
        }
    }

    return simplified;
}

#endregion

#region Write TTF binary

public readonly record struct Point(short X, short Y);

class Glyph(char character, List<List<Point>> contours, ushort advanceWidth)
{
    public char Character = character;
    public List<List<Point>> Contours = contours;
    public ushort AdvanceWidth = advanceWidth;

    public short? GetMinX() => (Contours.Count == 0) ? null : this.Contours.SelectMany(c => c.Select(p => p.X)).Min();
    public short? GetMaxX() => (Contours.Count == 0) ? null : this.Contours.SelectMany(c => c.Select(p => p.X)).Max();
    public short? GetMinY() => (Contours.Count == 0) ? null : this.Contours.SelectMany(c => c.Select(p => p.Y)).Min();
    public short? GetMaxY() => (Contours.Count == 0) ? null : this.Contours.SelectMany(c => c.Select(p => p.Y)).Max();
}

class PixelTtfBuilder
{
    readonly ushort UnitsPerEm;
    readonly string FamilyName;
    readonly bool Bold;
    readonly bool Italic;
    readonly string FontVersion;
    readonly string Description;
    readonly List<Glyph> Glyphs = new();

    public PixelTtfBuilder(
        ushort unitsPerEm,
        string familyName,
        bool bold = false,
        bool italic = false,
        string fontVersion = "1.0",
        string description = "")
    {
        UnitsPerEm = unitsPerEm;
        FamilyName = familyName;
        Bold = bold;
        Italic = italic;
        FontVersion = fontVersion;
        Description = description;
    }

    public void AddGlyph(char character, List<List<Point>> contours, ushort advanceWidth)
    {
        if (Glyphs.Any(g => g.Character == character))
            throw new ArgumentException($"Glyph \"{character}\" already exists.");

        Glyphs.Add(new Glyph(character, contours, advanceWidth));
    }

    static byte[] MakeGlyph(Glyph glyph)
    {
        using var s = new MemoryStream();

        I16(s, (short)glyph.Contours.Count); //numberOfContours
        short xMin = glyph.GetMinX() ?? 0;
        short yMin = glyph.GetMinY() ?? 0;
        short xMax = glyph.GetMaxX() ?? 0;
        short yMax = glyph.GetMaxY() ?? 0;
        I16(s, xMin);
        I16(s, yMin);
        I16(s, xMax);
        I16(s, yMax);

        //endPtsOfContours
        ushort pointCount = 0;

        foreach (var contour in glyph.Contours)
        {
            //Index of last point of every contour
            pointCount += (ushort)(contour.Count);
            U16(s, (ushort)(pointCount - 1));
        }

        U16(s, 0);

        // flags
        // 0x01 - ON_CURVE_POINT - always 1, because font is pixel
        // 0x08 - REPEAT_FLAG - the next byte specifies the number of
        // times this flag byte is to be repeate

        int remaining = pointCount;

        while (remaining > 0)
        {
            int run = Math.Min(remaining, 256);

            if (run == 1)
                s.WriteByte(0x01); // ON_CURVE_POINT
            else
            {
                s.WriteByte(0x09); // ON_CURVE_POINT and REPEAT_FLAG
                s.WriteByte((byte)(run - 1));
            }

            remaining -= run;
        }

        //xCoordinates
        short prevX = 0;

        foreach (var contour in glyph.Contours)
        {
            foreach (var point in contour)
            {
                I16(s, (short)(point.X - prevX));
                prevX = point.X;
            }
        }

        //yCoordinates
        short prevY = 0;

        foreach (var contour in glyph.Contours)
        {
            foreach (var point in contour)
            {
                I16(s, (short)(point.Y - prevY));
                prevY = point.Y;
            }
        }

        return s.ToArray();
    }

    static (byte[] glyf, byte[] loca) MakeGlyfAndLoca(List<Glyph> glyphs)
    {
        using var glyf = new MemoryStream();
        using var loca = new MemoryStream();

        // gid 0 - .notdef
        U32(loca, 0);
        U32(loca, 0);

        foreach (var glyph in glyphs)
        {
            glyf.Write(MakeGlyph(glyph));
            U32(loca, (uint)glyf.Position);
        }

        return (glyf.ToArray(), loca.ToArray());
    }

    static byte[] MakeMaxp(List<Glyph> glyphs)
    {
        using var s = new MemoryStream();

        U32(s, 0x00010000); //version
        U16(s, (ushort)(glyphs.Count + 1)); //numGlyphs +1 - .notdef

        ushort maxPoints = (ushort)glyphs.Max(g => g.Contours.Sum(c => c.Count));
        ushort maxContours = (ushort)glyphs.Max(g => g.Contours.Count);

        U16(s, maxPoints);
        U16(s, maxContours);

        U16(s, 0); // maxCompositePoints
        U16(s, 0); // maxCompositeContours
        U16(s, 1); // maxZones
        U16(s, 0); // maxTwilightPoints
        U16(s, 0); // maxStorage
        U16(s, 0); // maxFunctionDefs
        U16(s, 0); // maxInstructionDefs
        U16(s, 0); // maxStackElements
        U16(s, 0); // maxSizeOfInstructions
        U16(s, 0); // maxComponentElements
        U16(s, 0); // maxComponentDepth

        return s.ToArray();
    }

    static byte[] MakeHmtx(List<Glyph> glyphs)
    {
        using var s = new MemoryStream();

        // .notdef
        U16(s, 0); // advanceWidth
        I16(s, 0); // lsb

        foreach (var glyph in glyphs)
        {
            U16(s, glyph.AdvanceWidth);
            I16(s, glyph.GetMinX() ?? 0);
        }

        return s.ToArray();
    }

    static byte[] MakeHhea(List<Glyph> glyphs)
    {
        using var s = new MemoryStream();

        U16(s, 1); // majorVersion
        U16(s, 0); // minorVersion

        short ascender = glyphs.Where(g => g.GetMaxY() != null).Max(g => g.GetMaxY().Value);
        short descender = glyphs.Where(g => g.GetMinY() != null).Min(g => g.GetMinY().Value);
        ushort advanceWidthMax = glyphs.Max(g => g.AdvanceWidth);

        short minLeftSideBearing = glyphs.Where(g => g.GetMinX() != null).Min(g => g.GetMinX().Value);
        short minRightSideBearing = glyphs.Where(g => g.GetMaxX() != null).Min(g => (short)(g.AdvanceWidth - g.GetMaxX().Value));
        short xMaxExtent = glyphs.Where(g => g.GetMaxX() != null).Max(g => g.GetMaxX().Value);

        I16(s, ascender);
        I16(s, descender);
        I16(s, 0); // lineGap

        U16(s, advanceWidthMax);

        I16(s, minLeftSideBearing);
        I16(s, minRightSideBearing);
        I16(s, xMaxExtent);

        I16(s, 1); // caretSlopeRise
        I16(s, 0); // caretSlopeRun
        I16(s, 0); // caretOffset

        I16(s, 0); // reserved
        I16(s, 0); // reserved
        I16(s, 0); // reserved
        I16(s, 0); // reserved

        I16(s, 0); // metricDataFormat

        U16(s, (ushort)(glyphs.Count + 1)); // numberOfHMetrics - +1 for .nodef

        return s.ToArray();
    }

    static byte[] MakeHead(List<Glyph> glyphs, ushort unitsPerEm, bool bold, bool italic)
    {
        using var s = new MemoryStream();

        U16(s, 1);          // version - 1
        U16(s, 0);          // minorVersion - 0

        U32(s, 0x00010000); // fontRevision = 1.0
        //(Windows doesnt use this value, so I leave it 1.0)
        
        U32(s, 0);          // checkSumAdjustment - 0 for now

        U32(s, 0x5F0F3CF5); // magicNumber

        U16(s, 0);          // flags
        U16(s, unitsPerEm);

        U64(s, 0);          // created
        U64(s, 0);          // modified

        short xMin = glyphs.Where(g => g.GetMinX() != null).Min(g => g.GetMinX().Value);
        short yMin = glyphs.Where(g => g.GetMinY() != null).Min(g => g.GetMinY().Value);
        short xMax = glyphs.Where(g => g.GetMaxX() != null).Max(g => g.GetMaxX().Value);
        short yMax = glyphs.Where(g => g.GetMaxY() != null).Max(g => g.GetMaxY().Value);

        I16(s, xMin);
        I16(s, yMin);
        I16(s, xMax);
        I16(s, yMax);

        ushort macSttyle = 0;
        if (bold)
            macSttyle += 1;
        if (italic)
            macSttyle += 2;

        U16(s, macSttyle);
        U16(s, 8);  // lowestRecPPEM

        I16(s, 2);  // fontDirectionHint
        I16(s, 1);  // indexToLocFormat
        I16(s, 0);  // glyphDataFormat

        return s.ToArray();
    }

    static byte[] MakeCmap(List<Glyph> glyphs)
    {
        using var s = new MemoryStream();

        U16(s, 0);  // version
        U16(s, 1);  // numTables - one encoding record

        U16(s, 0);  // platformID - 0 - Unicode
        U16(s, 3);  // encodingID - 3 - Unicode BMP
        U32(s, 12); // subtableOffset - (16+16+16+16+32 - 12 bytes)

        ushort segCount = (ushort)(glyphs.Count + 1);

        U16(s, 4); // format
        ushort length = (ushort)(16 + segCount * 8);
        U16(s, length);

        U16(s, 0); // language
        U16(s, (ushort)(segCount * 2)); // segCountX2

        ushort power = 1;
        ushort entrySelector = 0;

        while (power * 2 <= segCount)
        {
            power *= 2;
            entrySelector++;
        }

        ushort searchRange = (ushort)(power * 2);
        ushort rangeShift = (ushort)(segCount * 2 - searchRange);

        U16(s, searchRange);
        U16(s, entrySelector);
        U16(s, rangeShift);

        // endCode[segCount]
        // End characterCode for each segment, last=0xFFFF.
        var mappings = glyphs
            .Select((glyph, i) => (
                Code: (ushort)glyph.Character,
                GlyphId: (ushort)(i + 1)
            ))
            .OrderBy(x => x.Code)
            .ToList();

        foreach (var m in mappings)
            U16(s, m.Code);

        U16(s, 0xFFFF);


        U16(s, 0); // reservedPad

        // startCode[]
        // Start character code for each segment.
        foreach (var m in mappings)
            U16(s, m.Code);

        U16(s, 0xFFFF);

        // idDelta[]
        // Delta for all character codes in segment.
        foreach (var m in mappings)
            I16(s, unchecked((short)(m.GlyphId - m.Code)));

        I16(s, 1);

        // idRangeOffset[]
        foreach (var m in mappings)
            U16(s, 0);

        U16(s, 0); //idRangeOffset

        return s.ToArray();
    }

    static byte[] MakeName(string familyName, bool bold, bool italic, string fontVersion, string description)
    {
        using var s = new MemoryStream();

        var styleName = "Regular";
        if (bold && italic)
            styleName = "Bold Italic";
        else if (bold)
            styleName = "Bold";
        else if (italic)
            styleName = "Italic";
        
        string[] fontVersionParts = fontVersion.Split('.');
        if (!(fontVersionParts.Length == 2 && int.TryParse(fontVersionParts[0], out _) && int.TryParse(fontVersionParts[1], out _)))
            fontVersion = "1.0";

        string postScriptName = Convert.ToHexString(Encoding.UTF8.GetBytes(familyName));
        postScriptName = postScriptName[..Math.Min(postScriptName.Length, 63)];

        var names = new[]
        {
            (Id: (ushort)1, Text: familyName),
            (Id: (ushort)2, Text: styleName),
            (Id: (ushort)4, Text: familyName.Replace(" ", "") + " " + styleName.Replace(" ", "")),
            (Id: (ushort)5, Text: "Version " + fontVersion),
            (Id: (ushort)6, Text: postScriptName),
            (Id: (ushort)10, Text: description)
        };

        U16(s, 0); // version
        U16(s, (ushort)names.Length); // count
        U16(s, (ushort)(6 + names.Length * 12)); //storageOffset

        var encoded = names
            .Select(n => (
                n.Id,
                Data: Utf16(n.Text)
            ))
            .ToList();

        ushort offset = 0;

        // Name records
        foreach (var name in encoded)
        {
            U16(s, 0);                          // platformID - Unicode
            U16(s, 3);                          // encodingID - Unicode BMP
            U16(s, 0);                          // languageID
            U16(s, name.Id);                    // nameID
            U16(s, (ushort)name.Data.Length);   // length
            U16(s, offset);                     // stringOffset

            offset += (ushort)name.Data.Length;
        }

        foreach (var name in encoded)
            s.Write(name.Data);

        return s.ToArray();
    }

    static byte[] MakePost()
    {
        using var s = new MemoryStream();

        U32(s, 0x00030000); // version 3.0

        I32(s, 0);          // italicAngle = 0.0

        I16(s, 0);          // underlinePosition
        I16(s, 0);          // underlineThickness

        U32(s, 0);          // isFixedPitch

        U32(s, 0);          // minMemType42
        U32(s, 0);          // maxMemType42
        U32(s, 0);          // minMemType1
        U32(s, 0);          // maxMemType1

        return s.ToArray();
    }

    static byte[] MakeOS2(List<Glyph> glyphs, bool bold)
    {
        using var s = new MemoryStream();

        short ascender = glyphs.Where(g => g.GetMaxY() != null).Max(g => g.GetMaxY().Value);
        short descender = glyphs.Where(g => g.GetMinY() != null).Min(g => g.GetMinY().Value);

        short avgWidth = (short)glyphs
            .Where(g => g.AdvanceWidth != 0)
            .Average(g => g.AdvanceWidth);

        ushort firstChar = glyphs.Min(g => (ushort)g.Character);
        ushort lastChar = glyphs.Max(g => (ushort)g.Character);

        U16(s, 4);        // version
        I16(s, avgWidth); // xAvgCharWidth

        // usWeightClass
        U16(s, (ushort)(bold ? 700 : 400)); //bold : regular
        U16(s, 5);        // usWidthClass
        U16(s, 0);        // fsType: installable embedding

        I16(s, 0); // ySubscriptXSize
        I16(s, 0); // ySubscriptYSize
        I16(s, 0); // ySubscriptXOffset
        I16(s, 0); // ySubscriptYOffset

        I16(s, 0); // ySuperscriptXSize
        I16(s, 0); // ySuperscriptYSize
        I16(s, 0); // ySuperscriptXOffset
        I16(s, 0); // ySuperscriptYOffset

        I16(s, 0); // yStrikeoutSize
        I16(s, 0); // yStrikeoutPosition

        I16(s, 0); // sFamilyClass

        // panose[10]
        for (int i = 0; i < 10; i++)
            s.WriteByte(0);

        U32(s, 0); // ulUnicodeRange1
        U32(s, 0); // ulUnicodeRange2
        U32(s, 0); // ulUnicodeRange3
        U32(s, 0); // ulUnicodeRange4

        s.WriteByte((byte)' ');
        s.WriteByte((byte)' ');
        s.WriteByte((byte)' ');
        s.WriteByte((byte)' ');

        // fsSelection
        ushort fsSelection = 0x0080; // bit 7: USE_TYPO_METRICS
        
        if (italic)
            fsSelection |= 0x0001;   // bit 0: ITALIC
        
        if (bold)
            fsSelection |= 0x0020;   // bit 5: BOLD
        
        if (!bold && !italic)
            fsSelection |= 0x0040;   // bit 6: REGULAR
        
        U16(s, fsSelection);

        U16(s, firstChar); // usFirstCharIndex
        U16(s, lastChar);  // usLastCharIndex

        I16(s, ascender);  // sTypoAscender
        I16(s, descender); // sTypoDescender
        I16(s, 0);         // sTypoLineGap

        U16(s, (ushort)Math.Max((short)(0), ascender));   // usWinAscent
        U16(s, (ushort)Math.Max((short)(0), -descender)); // usWinDescent

        U32(s, 0); // ulCodePageRange1
        U32(s, 0); // ulCodePageRange2

        I16(s, 0); // sxHeight
        I16(s, 0); // sCapHeight

        U16(s, 0);    // usDefaultChar
        U16(s, 0x20); // usBreakChar = SPACE
        U16(s, 0);    // usMaxContext

        return s.ToArray();
    }

    static void Tag(Stream s, string tag)
    {
        if (tag.Length != 4)
            throw new ArgumentException("Tag must be 4 characters.");

        foreach (char c in tag)
            s.WriteByte((byte)c);
    }

    static uint Checksum(byte[] data)
    {
        uint sum = 0;

        for (int i = 0; i < data.Length; i += 4)
        {
            uint value = 0;

            for (int j = 0; j < 4; j++)
            {
                value <<= 8;

                if (i + j < data.Length)
                    value |= data[i + j];
            }

            unchecked
            {
                sum += value;
            }
        }

        return sum;
    }

    public byte[] Build()
    {
        if (Glyphs.Count == 0)
            throw new InvalidOperationException("Font must contain at least one glyph");

        if (UnitsPerEm < 16)
            throw new InvalidOperationException("UnitsPerEm must be at least 16");

        var (glyf, loca) = MakeGlyfAndLoca(Glyphs);

        var tables = new List<(string Tag, byte[] Data)>
        {
            ("OS/2", MakeOS2(Glyphs, Bold)),
            ("cmap", MakeCmap(Glyphs)),
            ("glyf", glyf),
            ("head", MakeHead(Glyphs, UnitsPerEm, Bold, Italic)),
            ("hhea", MakeHhea(Glyphs)),
            ("hmtx", MakeHmtx(Glyphs)),
            ("loca", loca),
            ("maxp", MakeMaxp(Glyphs)),
            ("name", MakeName(FamilyName, Bold, Italic, FontVersion, Description)),
            ("post", MakePost())
        };

        tables = tables
            .OrderBy(t => t.Tag, StringComparer.Ordinal)
            .ToList();

        ushort numTables = (ushort)tables.Count;

        uint offset = (uint)(12 + numTables * 16);

        ushort power = 1;
        ushort entrySelector = 0;

        while (power * 2 <= numTables)
        {
            power *= 2;
            entrySelector++;
        }

        ushort searchRange = (ushort)(power * 16);
        ushort rangeShift = (ushort)(numTables * 16 - searchRange);

        using var s = new MemoryStream();

        U32(s, 0x00010000); // TrueType
        U16(s, numTables);
        U16(s, searchRange);
        U16(s, entrySelector);
        U16(s, rangeShift);

        var records = new List<(string Tag, byte[] Data, uint Offset, uint Checksum)>();

        foreach (var table in tables)
        {
            records.Add((
                table.Tag,
                table.Data,
                offset,
                Checksum(table.Data)
            ));

            offset += (uint)table.Data.Length;

            offset = ((offset + 3) / 4) * 4;
        }

        foreach (var table in records)
        {
            Tag(s, table.Tag);

            U32(s, table.Checksum);
            U32(s, table.Offset);
            U32(s, (uint)table.Data.Length);
        }

        foreach (var table in records)
        {
            s.Write(table.Data);

            while (s.Position % 4 != 0)
                s.WriteByte(0);
        }

        byte[] font = s.ToArray();

        uint headOffset = records
            .First(t => t.Tag == "head")
            .Offset;

        uint adjustment = unchecked(0xB1B0AFBAu - Checksum(font));

        U32WithOffset(font, (int)headOffset + 8, adjustment);

        return font;
    }

    static void U16(Stream s, ushort v)
    {
        s.WriteByte((byte)(v >> 8));
        s.WriteByte((byte)v);
    }

    static void I16(Stream s, short v)
    {
        U16(s, unchecked((ushort)v));
    }

    static void U32(Stream s, uint v)
    {
        U16(s, (ushort)(v >> 16));
        U16(s, (ushort)(v));
    }

    static void U32WithOffset(byte[] data, int offset, uint v)
    {
        data[offset] = (byte)(v >> 24);
        data[offset + 1] = (byte)(v >> 16);
        data[offset + 2] = (byte)(v >> 8);
        data[offset + 3] = (byte)v;
    }

    static void I32(Stream s, int v)
    {
        U32(s, unchecked((uint)v));
    }

    static void U64(Stream s, ulong v)
    {
        U32(s, (uint)(v >> 32));
        U32(s, (uint)v);
    }

    static byte[] Utf16(string str)
    {
        using var s = new MemoryStream();

        foreach (char c in str)
            U16(s, c);

        return s.ToArray();
    }
}

#endregion
