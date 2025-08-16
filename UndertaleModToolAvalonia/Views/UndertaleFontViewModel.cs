﻿using System.Collections.Generic;
using System.Linq;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleFontViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => Font;
    public UndertaleFont Font { get; set; }

    [Notify]
    private UndertaleFont.Glyph? _GlyphsSelected;

    public UndertaleFontViewModel(UndertaleFont font)
    {
        Font = font;
    }

    public void GlyphsSelectedChanged(object? item)
    {
        GlyphsSelected = (UndertaleFont.Glyph?)item!;
    }

    public void SortGlyphs()
    {
        List<UndertaleFont.Glyph> sortedGlyphs = Font.Glyphs.OrderBy(x => x.Character).ToList();

        Font.Glyphs.Clear();
        foreach (UndertaleFont.Glyph glyph in sortedGlyphs)
            Font.Glyphs.Add(glyph);
    }

    public void UpdateRange()
    {
        IEnumerable<ushort> characters = Font.Glyphs.Select(x => x.Character);
        Font.RangeStart = characters.Min();
        Font.RangeEnd = characters.Max();
    }

    public static UndertaleFont.Glyph CreateGlyph() => new();
    public static UndertaleFont.Glyph.GlyphKerning CreateGlyphKerning() => new();
}
