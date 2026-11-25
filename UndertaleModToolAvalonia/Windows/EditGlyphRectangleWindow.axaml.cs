using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

/// <summary>
/// Lets the user move and resize a font glyph's rectangle on the font texture with the mouse or keyboard,
/// or draw one for an empty glyph. Works on copies of the glyphs; "Save changes" copies them back.
/// Returns the font's glyph that was selected last, or null when cancelled.
/// </summary>
public partial class EditGlyphRectangleWindow : Window
{
    // Which part of the selected rectangle the mouse is over: its body, an edge or a corner.
    enum HitType { None, Body, Left, Right, Top, Bottom, TopLeft, TopRight, BottomRight, BottomLeft }

    // How close to an edge, in screen pixels, still counts as grabbing it.
    const double EdgeGrabPixels = 4;

    static readonly Cursor ArrowCursor = new(StandardCursorType.Arrow);
    static readonly Cursor CrossCursor = new(StandardCursorType.Cross);
    static readonly Cursor MoveCursor = new(StandardCursorType.SizeAll);
    static readonly Cursor TopLeftCursor = new(StandardCursorType.TopLeftCorner);
    static readonly Cursor TopRightCursor = new(StandardCursorType.TopRightCorner);
    static readonly Cursor BottomLeftCursor = new(StandardCursorType.BottomLeftCorner);
    static readonly Cursor BottomRightCursor = new(StandardCursorType.BottomRightCorner);
    static readonly Cursor HorizontalCursor = new(StandardCursorType.SizeWestEast);
    static readonly Cursor VerticalCursor = new(StandardCursorType.SizeNorthSouth);

    public UndertaleFont Font { get; } = null!;
    public UndertaleFont.Glyph[] Glyphs { get; } = [];

    // The font's glyphs that Glyphs are copies of, in the same order; Save writes into these objects,
    // so the edits reach the right glyphs even if the font's list changes while the window is open.
    readonly UndertaleFont.Glyph[] originalGlyphs = [];

    UndertaleFont.Glyph selectedGlyph = null!;
    bool isDrawingNewRectangle;
    HitType lastHoveredType = HitType.None;
    UndertaleFont.Glyph? lastTooltipGlyph;
    readonly DispatcherTimer tooltipTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };

    // State of the current mouse drag, in texture pixels.
    HitType dragType = HitType.None;
    Point dragStart;
    PixelRect startRect;
    short startShift;

    int TextureWidth => Font.Texture!.BoundingWidth;
    int TextureHeight => Font.Texture!.BoundingHeight;

    public EditGlyphRectangleWindow() { }

    public EditGlyphRectangleWindow(UndertaleFont font, UndertaleFont.Glyph glyph)
    {
        Font = font;
        originalGlyphs = font.Glyphs.ToArray();
        Glyphs = originalGlyphs.Select(x => x.Clone()).ToArray();

        InitializeComponent();

        SelectGlyph(Glyphs[Array.IndexOf(originalGlyphs, glyph)]);

        // Shift shows the top line until it's released, but the release is missed if focus moves away first.
        TexturePanel.LostFocus += (_, _) => Overlay.ShowTopLine = false;
        Deactivated += (_, _) => Overlay.ShowTopLine = false;

        // The mouse can be taken away mid-drag (e.g. by switching windows) without a release reaching the texture.
        TexturePanel.PointerCaptureLost += (_, _) => EndDrag();

        tooltipTimer.Tick += (_, _) =>
        {
            tooltipTimer.Stop();
            ToolTip.SetIsOpen(TexturePanel, true);
        };
        TexturePanel.PointerExited += (_, _) =>
        {
            HideCharacterTooltip();
            lastTooltipGlyph = null;
        };
        Closed += (_, _) => tooltipTimer.Stop();

        Opened += (_, _) =>
        {
            // Start zoomed in on small textures, like the WPF version: the largest power of 2 that fits the width.
            double fitScale = TextureScroll.Viewport.Width / TextureWidth;
            if (fitScale > 1)
                TextureImage.Scaling = Math.Pow(2, Math.Floor(Math.Log2(fitScale)));
            TexturePanel.Focus();
        };
    }

    void SelectGlyph(UndertaleFont.Glyph glyph)
    {
        selectedGlyph = glyph;
        Overlay.SelectedGlyph = glyph;

        isDrawingNewRectangle = glyph.IsEmpty();
        TexturePanel.Cursor = isDrawingNewRectangle ? CrossCursor : ArrowCursor;
        lastHoveredType = HitType.None;

        HelpTextBlock.Text = isDrawingNewRectangle
            ? "This glyph is empty: drag over its region on the texture to create its rectangle. Ctrl + scroll to zoom."
            : "Drag the red rectangle or its edges to move or resize it, or use the arrow keys (Shift + arrows to resize). " +
              "Double-click another rectangle to select it. Ctrl + scroll to zoom.";
    }

    double Scale => TextureImage.Scaling;

    Point TexturePosition(PointerEventArgs e)
    {
        Point position = e.GetPosition(TextureImage);
        return new Point(position.X / Scale, position.Y / Scale);
    }

    static PixelRect RectOf(UndertaleFont.Glyph glyph) =>
        new(glyph.SourceX, glyph.SourceY, glyph.SourceWidth, glyph.SourceHeight);

    void Texture_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(TexturePanel).Properties.IsLeftButtonPressed)
            return;

        TexturePanel.Focus();
        Point position = TexturePosition(e);

        // Like the WPF editor, other rectangles can't be selected while drawing a new one.
        if (e.ClickCount >= 2 && !isDrawingNewRectangle)
        {
            UndertaleFont.Glyph? clickedGlyph = Glyphs.FirstOrDefault(glyph => glyph.Contains(position.X, position.Y));
            if (clickedGlyph is not null && clickedGlyph != selectedGlyph)
            {
                SelectGlyph(clickedGlyph);
                return;
            }
        }

        dragType = isDrawingNewRectangle ? HitType.BottomRight : GetHitType(position);
        if (dragType == HitType.None)
            return;

        // The tooltip would cover the rectangle being dragged.
        HideCharacterTooltip();
        lastTooltipGlyph = null;

        dragStart = position;
        startShift = selectedGlyph.Shift;
        startRect = FitInTexture(RectOf(selectedGlyph));
        if (isDrawingNewRectangle)
        {
            // The new rectangle starts at the pixel under the mouse and grows from nothing,
            // so its whole width is added to the shift. It stays empty until the mouse moves.
            PixelPoint anchor = ClampToTexture(PixelPointAt(position));
            startRect = new PixelRect(anchor.X, anchor.Y, 0, 0);
        }
        Overlay.ShowTopLine = dragType is HitType.Top or HitType.TopLeft or HitType.TopRight;
        e.Pointer.Capture(TexturePanel);
    }

    void Texture_PointerMoved(object? sender, PointerEventArgs e)
    {
        Point position = TexturePosition(e);

        if (dragType == HitType.None)
        {
            ShowCharacterTooltip(position);
            if (isDrawingNewRectangle)
                return;

            HitType hoveredType = GetHitType(position);
            if (hoveredType != lastHoveredType)
            {
                TexturePanel.Cursor = CursorFor(hoveredType);
                lastHoveredType = hoveredType;
            }
            return;
        }

        if (isDrawingNewRectangle)
        {
            ApplyRect(DrawnRect(position));
            return;
        }

        int dx = (int)Math.Round(position.X - dragStart.X);
        int dy = (int)Math.Round(position.Y - dragStart.Y);
        ApplyRect(DraggedRect(dx, dy));
    }

    /// <summary>
    /// Like the WPF editor, hovering a glyph's rectangle shows its character.
    /// </summary>
    void ShowCharacterTooltip(Point position)
    {
        UndertaleFont.Glyph? hoveredGlyph = Glyphs.FirstOrDefault(glyph => glyph.Contains(position.X, position.Y));
        if (hoveredGlyph == lastTooltipGlyph)
            return;
        lastTooltipGlyph = hoveredGlyph;

        // One tooltip serves the whole texture. Avalonia only opens it when the pointer enters the texture,
        // so reopen it ourselves after the usual delay, next to the newly hovered glyph.
        HideCharacterTooltip();
        bool hasCharacter = hoveredGlyph is not null && hoveredGlyph.Character != 0;
        if (!hasCharacter)
            return;

        ToolTip.SetTip(TexturePanel, ((char)hoveredGlyph!.Character).ToString());
        tooltipTimer.Start();
    }

    void HideCharacterTooltip()
    {
        tooltipTimer.Stop();
        ToolTip.SetIsOpen(TexturePanel, false);
        ToolTip.SetTip(TexturePanel, null);
    }

    /// <summary>
    /// The new rectangle from the pixel where the drag started to the pixel under the mouse, both included, in any direction.
    /// </summary>
    PixelRect DrawnRect(Point position)
    {
        PixelPoint current = ClampToTexture(PixelPointAt(position));
        int left = Math.Min(startRect.X, current.X);
        int top = Math.Min(startRect.Y, current.Y);
        int right = Math.Max(startRect.X, current.X) + 1;
        int bottom = Math.Max(startRect.Y, current.Y) + 1;
        return new PixelRect(left, top, right - left, bottom - top);
    }

    void Texture_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (dragType == HitType.None)
            return;

        e.Pointer.Capture(null);
        EndDrag();
    }

    void EndDrag()
    {
        if (dragType == HitType.None)
            return;

        dragType = HitType.None;
        Overlay.ShowTopLine = false;

        if (isDrawingNewRectangle)
            SelectGlyph(selectedGlyph);
    }

    /// <summary>
    /// A glyph from odd data may stick out of the texture or be larger than it. Bring it back inside before
    /// dragging, so every edge has room to move and the drag math (and shift) compares like with like.
    /// </summary>
    PixelRect FitInTexture(PixelRect rect)
    {
        int width = Math.Min(rect.Width, TextureWidth);
        int height = Math.Min(rect.Height, TextureHeight);
        int left = Math.Clamp(rect.X, 0, TextureWidth - width);
        int top = Math.Clamp(rect.Y, 0, TextureHeight - height);
        return new PixelRect(left, top, width, height);
    }

    /// <summary>
    /// The rectangle after dragging the grabbed part by (dx, dy) texture pixels, kept inside the texture and at least 1x1.
    /// </summary>
    PixelRect DraggedRect(int dx, int dy)
    {
        int left = startRect.X, top = startRect.Y, right = startRect.Right, bottom = startRect.Bottom;
        int width = startRect.Width, height = startRect.Height;

        if (dragType == HitType.Body)
        {
            left = Math.Clamp(left + dx, 0, TextureWidth - width);
            top = Math.Clamp(top + dy, 0, TextureHeight - height);
            return new PixelRect(left, top, width, height);
        }

        bool movesLeft = dragType is HitType.Left or HitType.TopLeft or HitType.BottomLeft;
        bool movesRight = dragType is HitType.Right or HitType.TopRight or HitType.BottomRight;
        bool movesTop = dragType is HitType.Top or HitType.TopLeft or HitType.TopRight;
        bool movesBottom = dragType is HitType.Bottom or HitType.BottomLeft or HitType.BottomRight;

        if (movesLeft)
            left = Math.Clamp(left + dx, 0, right - 1);
        if (movesRight)
            right = Math.Clamp(right + dx, left + 1, TextureWidth);
        if (movesTop)
            top = Math.Clamp(top + dy, 0, bottom - 1);
        if (movesBottom)
            bottom = Math.Clamp(bottom + dy, top + 1, TextureHeight);

        return new PixelRect(left, top, right - left, bottom - top);
    }

    void ApplyRect(PixelRect rect, bool adjustShift = true)
    {
        selectedGlyph.SourceX = (ushort)rect.X;
        selectedGlyph.SourceY = (ushort)rect.Y;
        selectedGlyph.SourceWidth = (ushort)rect.Width;
        selectedGlyph.SourceHeight = (ushort)rect.Height;

        // Like the WPF editor: when resizing with the mouse, the shift (distance to the next character) grows and shrinks with the width.
        if (adjustShift)
            selectedGlyph.Shift = (short)(startShift + rect.Width - startRect.Width);
    }

    HitType GetHitType(Point position)
    {
        PixelRect rect = RectOf(selectedGlyph);

        // At most a third of the rectangle counts as an edge, so narrow glyphs can still be grabbed in the middle to move them.
        double edgeX = Math.Min(EdgeGrabPixels / Scale, rect.Width / 3.0);
        double edgeY = Math.Min(EdgeGrabPixels / Scale, rect.Height / 3.0);

        bool isInside = position.X >= rect.X - edgeX && position.X <= rect.Right + edgeX
                        && position.Y >= rect.Y - edgeY && position.Y <= rect.Bottom + edgeY;
        if (!isInside)
            return HitType.None;

        bool nearLeft = Math.Abs(position.X - rect.X) <= edgeX;
        bool nearRight = Math.Abs(position.X - rect.Right) <= edgeX;
        bool nearTop = Math.Abs(position.Y - rect.Y) <= edgeY;
        bool nearBottom = Math.Abs(position.Y - rect.Bottom) <= edgeY;

        return (nearLeft, nearRight, nearTop, nearBottom) switch
        {
            (true, _, true, _) => HitType.TopLeft,
            (_, true, true, _) => HitType.TopRight,
            (true, _, _, true) => HitType.BottomLeft,
            (_, true, _, true) => HitType.BottomRight,
            (true, _, _, _) => HitType.Left,
            (_, true, _, _) => HitType.Right,
            (_, _, true, _) => HitType.Top,
            (_, _, _, true) => HitType.Bottom,
            _ => HitType.Body,
        };
    }

    static Cursor CursorFor(HitType hitType) => hitType switch
    {
        HitType.Body => MoveCursor,
        HitType.TopLeft => TopLeftCursor,
        HitType.TopRight => TopRightCursor,
        HitType.BottomLeft => BottomLeftCursor,
        HitType.BottomRight => BottomRightCursor,
        HitType.Left or HitType.Right => HorizontalCursor,
        HitType.Top or HitType.Bottom => VerticalCursor,
        _ => ArrowCursor,
    };

    static PixelPoint PixelPointAt(Point position) => new((int)Math.Floor(position.X), (int)Math.Floor(position.Y));

    PixelPoint ClampToTexture(PixelPoint point) =>
        new(Math.Clamp(point.X, 0, TextureWidth - 1), Math.Clamp(point.Y, 0, TextureHeight - 1));

    void Texture_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.LeftShift or Key.RightShift)
        {
            Overlay.ShowTopLine = true;
            return;
        }

        bool isResize = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        (int dx, int dy) = e.Key switch
        {
            Key.Left => (-1, 0),
            Key.Right => (1, 0),
            Key.Up => (0, -1),
            Key.Down => (0, 1),
            _ => (0, 0),
        };
        bool isArrowKey = dx != 0 || dy != 0;
        if (!isArrowKey)
            return;

        // Arrow keys always belong to the rectangle, even when ignored, so they don't scroll the texture instead.
        e.Handled = true;
        bool isMouseDragging = dragType != HitType.None;
        if (isDrawingNewRectangle || isMouseDragging)
            return;

        // A glyph from odd data that sticks out of the texture is left alone, like in the WPF editor,
        // rather than being silently pulled inside by a key press.
        PixelRect rect = RectOf(selectedGlyph);
        bool fitsInTexture = FitInTexture(rect) == rect;
        if (!fitsInTexture)
            return;

        // Arrow keys move the rectangle; with Shift they move its right and bottom edges.
        dragType = isResize ? HitType.BottomRight : HitType.Body;
        startRect = rect;
        ApplyRect(DraggedRect(dx, dy), adjustShift: false);
        dragType = HitType.None;
    }

    void Texture_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.LeftShift or Key.RightShift)
            Overlay.ShowTopLine = false;
    }

    void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        for (int i = 0; i < Glyphs.Length; i++)
        {
            UndertaleFont.Glyph original = originalGlyphs[i];
            UndertaleFont.Glyph edited = Glyphs[i];
            original.SourceX = edited.SourceX;
            original.SourceY = edited.SourceY;
            original.SourceWidth = edited.SourceWidth;
            original.SourceHeight = edited.SourceHeight;
            original.Shift = edited.Shift;
        }

        Close(originalGlyphs[Array.IndexOf(Glyphs, selectedGlyph)]);
    }

    void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
