using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

/// <summary>
/// Draws font glyph rectangles over the font's texture. Place it over an <see cref="SKImageViewer"/> showing
/// the font texture, at the same size: it follows the viewer's zoom by comparing its size to the texture's.
/// </summary>
public class FontGlyphsOverlay : Control
{
    public static readonly StyledProperty<UndertaleFont?> FontProperty =
        AvaloniaProperty.Register<FontGlyphsOverlay, UndertaleFont?>(nameof(Font));

    public UndertaleFont? Font
    {
        get => GetValue(FontProperty);
        set => SetValue(FontProperty, value);
    }

    public static readonly StyledProperty<UndertaleFont.Glyph?> SelectedGlyphProperty =
        AvaloniaProperty.Register<FontGlyphsOverlay, UndertaleFont.Glyph?>(nameof(SelectedGlyph));

    public UndertaleFont.Glyph? SelectedGlyph
    {
        get => GetValue(SelectedGlyphProperty);
        set => SetValue(SelectedGlyphProperty, value);
    }

    /// <summary>
    /// The glyphs to outline besides the selected one, or null for none.
    /// </summary>
    public static readonly StyledProperty<UndertaleFont.Glyph[]?> OtherGlyphsProperty =
        AvaloniaProperty.Register<FontGlyphsOverlay, UndertaleFont.Glyph[]?>(nameof(OtherGlyphs));

    public UndertaleFont.Glyph[]? OtherGlyphs
    {
        get => GetValue(OtherGlyphsProperty);
        set => SetValue(OtherGlyphsProperty, value);
    }

    /// <summary>
    /// Draws a line across the whole texture at the selected glyph's top, to line it up with other glyphs.
    /// </summary>
    public static readonly StyledProperty<bool> ShowTopLineProperty =
        AvaloniaProperty.Register<FontGlyphsOverlay, bool>(nameof(ShowTopLine));

    public bool ShowTopLine
    {
        get => GetValue(ShowTopLineProperty);
        set => SetValue(ShowTopLineProperty, value);
    }

    // Same colors as the WPF editor.
    static readonly IPen SelectedPen = new Pen(new SolidColorBrush(Color.FromRgb(0xB2, 0x31, 0x31)), 1);
    static readonly IBrush SelectedFill = new SolidColorBrush(Colors.Red, 0.2);
    static readonly IPen OtherPen = new Pen(new SolidColorBrush(Colors.Yellow, 0.25), 1);
    static readonly IBrush OtherFill = new SolidColorBrush(Colors.Yellow, 0.05);
    static readonly IBrush OffsetBrush = Brushes.Blue;
    static readonly IBrush ShiftBrush = Brushes.LawnGreen;
    static readonly IBrush OriginBrush = new SolidColorBrush(Color.FromRgb(0x3E, 0x80, 0x80));
    static readonly IBrush TopLineBrush = new SolidColorBrush(Colors.Gold, 0.6);

    static FontGlyphsOverlay()
    {
        AffectsRender<FontGlyphsOverlay>(FontProperty, SelectedGlyphProperty, OtherGlyphsProperty, ShowTopLineProperty);
    }

    public FontGlyphsOverlay()
    {
        IsHitTestVisible = false;
    }

    // The font and glyphs outlive this view, so only listen to them while it's shown; otherwise they would keep it alive.
    // These are what it currently listens to, so it can stop even after the font replaced its glyph list.
    UndertaleFont? listenedFont;
    INotifyCollectionChanged? listenedGlyphs;
    INotifyPropertyChanged? listenedGlyph;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        StartListening();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopListening();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        bool changesWhatToListenTo = change.Property == FontProperty || change.Property == SelectedGlyphProperty;
        if (changesWhatToListenTo && this.IsAttachedToVisualTree())
        {
            StopListening();
            StartListening();
        }
    }

    void StartListening()
    {
        listenedFont = Font;
        if (listenedFont is INotifyPropertyChanged notifyingFont)
            notifyingFont.PropertyChanged += OnFontChanged;

        listenedGlyphs = listenedFont?.Glyphs;
        if (listenedGlyphs is not null)
            listenedGlyphs.CollectionChanged += OnGlyphsChanged;

        listenedGlyph = SelectedGlyph as INotifyPropertyChanged;
        if (listenedGlyph is not null)
            listenedGlyph.PropertyChanged += OnGlyphChanged;
    }

    void StopListening()
    {
        if (listenedFont is INotifyPropertyChanged notifyingFont)
            notifyingFont.PropertyChanged -= OnFontChanged;
        if (listenedGlyphs is not null)
            listenedGlyphs.CollectionChanged -= OnGlyphsChanged;
        if (listenedGlyph is not null)
            listenedGlyph.PropertyChanged -= OnGlyphChanged;

        listenedFont = null;
        listenedGlyphs = null;
        listenedGlyph = null;
    }

    void OnFontChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Only the texture and the glyph list affect what's drawn; the list may have been replaced entirely.
        if (e.PropertyName == nameof(UndertaleFont.Glyphs))
        {
            StopListening();
            StartListening();
        }
        if (e.PropertyName is nameof(UndertaleFont.Glyphs) or nameof(UndertaleFont.Texture))
            InvalidateVisual();
    }

    void OnGlyphChanged(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();
    void OnGlyphsChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        int textureWidth = Font?.Texture?.BoundingWidth ?? 0;
        if (textureWidth == 0)
            return;

        // Glyph coordinates are texture pixels; the viewer may be zoomed.
        double scale = Bounds.Width / textureWidth;
        Rect ToScreen(double x, double y, double width, double height) =>
            new(x * scale, y * scale, width * scale, height * scale);

        if (OtherGlyphs is not null)
        {
            foreach (UndertaleFont.Glyph glyph in OtherGlyphs)
            {
                if (glyph != SelectedGlyph && !glyph.IsEmpty())
                    context.DrawRectangle(OtherFill, OtherPen, ToScreen(glyph.SourceX, glyph.SourceY, glyph.SourceWidth, glyph.SourceHeight));
            }
        }

        UndertaleFont.Glyph? selected = SelectedGlyph;
        if (selected is null)
            return;

        if (ShowTopLine)
            context.FillRectangle(TopLineBrush, ToScreen(0, selected.SourceY, textureWidth, 1));

        if (selected.IsEmpty())
            return;

        context.DrawRectangle(SelectedFill, SelectedPen, ToScreen(selected.SourceX, selected.SourceY, selected.SourceWidth, selected.SourceHeight));

        // Where the glyph is drawn (offset), how far the next one starts (shift), and the origin, on the bottom row.
        int bottomRow = selected.SourceY + selected.SourceHeight - 1;
        context.FillRectangle(OffsetBrush, ToScreen(selected.SourceX + selected.Offset, selected.SourceY, 1, selected.SourceHeight));
        if (selected.Shift > 0)
            context.FillRectangle(ShiftBrush, ToScreen(selected.SourceX, bottomRow, selected.Shift, 1));
        context.FillRectangle(OriginBrush, ToScreen(selected.SourceX, bottomRow, 1, 1));
    }
}
