using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleFontViewModel : ObservableObject, IUndertaleResourceViewModel
{
    public MainViewModel MainVM;

    public UndertaleResource Resource => Font;
    public UndertaleFont Font { get; }

    [ObservableProperty]
    public partial UndertaleFont.Glyph? GlyphsSelected { get; set; }

    public UndertaleFontViewModel(UndertaleFont font, IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();
        Font = font;
    }

    public void GlyphsSelectedChanged(object? item)
    {
        GlyphsSelected = (UndertaleFont.Glyph?)item!;
    }

    /// <summary>
    /// Returns the glyph drawn at this pixel of the font texture, if any.
    /// </summary>
    public UndertaleFont.Glyph? FindGlyphAt(double x, double y)
    {
        return Font.Glyphs.FirstOrDefault(glyph => glyph.Contains(x, y));
    }

    /// <summary>
    /// Adds a glyph with no rectangle at the end, to be drawn with the rectangle editor.
    /// Returns null without adding one if the last glyph is still empty.
    /// </summary>
    public UndertaleFont.Glyph? CreateEmptyGlyph()
    {
        UndertaleFont.Glyph? lastGlyph = Font.Glyphs.LastOrDefault();
        if (lastGlyph is not null && lastGlyph.IsEmpty())
            return null;

        UndertaleFont.Glyph glyph = new();
        Font.Glyphs.Add(glyph);
        return glyph;
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
