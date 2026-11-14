using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleFontView : UserControl
{
    // Takes the selected glyph's width and height; a binding (unlike a view model property) follows their changes without keeping the glyph subscribed to this view.
    public static readonly FuncMultiValueConverter<object?, string> EditRectangleButtonText = new(sizes =>
    {
        bool isEmpty = sizes.Any(size => size is ushort and 0);
        return isEmpty ? "Select a region of an empty glyph" : "Edit the selected glyph rectangle";
    });

    public UndertaleFontView()
    {
        InitializeComponent();
    }

    // Only a click that also started on the texture selects a glyph, not a drag that ends over it.
    bool isPressedOnTexture;

    void TextureImage_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        isPressedOnTexture = e.GetCurrentPoint(TextureImage).Properties.IsLeftButtonPressed;
    }

    void TextureImage_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        bool isClick = isPressedOnTexture && e.InitialPressMouseButton == MouseButton.Left;
        isPressedOnTexture = false;
        if (!isClick || DataContext is not UndertaleFontViewModel vm)
            return;

        // The image can be zoomed with Ctrl+scroll, so convert from screen to texture pixels.
        double scale = TextureImage.Scaling;
        Point position = e.GetPosition(TextureImage);
        UndertaleFont.Glyph? glyph = vm.FindGlyphAt(position.X / scale, position.Y / scale);
        if (glyph is not null)
            SelectGlyph(glyph);
    }

    void SelectGlyph(UndertaleFont.Glyph glyph)
    {
        GlyphsExpander.IsExpanded = true;
        GlyphsGrid.SelectAndScrollTo(glyph);
    }

    async void EditRectangleButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleFontViewModel vm || vm.GlyphsSelected is null)
            return;

        bool hasTexture = vm.Font.Texture is not null && vm.Font.Texture.BoundingWidth > 0 && vm.Font.Texture.BoundingHeight > 0;
        if (!hasTexture)
        {
            await vm.MainVM.View!.MessageDialog("The font has no texture.", "Error");
            return;
        }

        // A script may have removed the glyph or replaced the font's glyph list since it was selected.
        if (!vm.Font.Glyphs.Contains(vm.GlyphsSelected))
        {
            await vm.MainVM.View!.MessageDialog("Cannot find the selected glyph.", "Error");
            return;
        }

        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        // Like the WPF editor, report an editor failure instead of letting this async void handler crash the app.
        UndertaleFont.Glyph? lastSelected;
        try
        {
            EditGlyphRectangleWindow window = new(vm.Font, vm.GlyphsSelected);
            lastSelected = await window.ShowDialog<UndertaleFont.Glyph?>(owner);
        }
        catch (Exception ex)
        {
            await vm.MainVM.View!.MessageDialog($"An error occurred in the glyph rectangle editor window.\n{ex}", "Error");
            return;
        }

        if (lastSelected is not null && vm.Font.Glyphs.Contains(lastSelected))
            SelectGlyph(lastSelected);
    }

    async void CreateGlyphButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UndertaleFontViewModel vm)
            return;

        UndertaleFont.Glyph? glyph = vm.CreateEmptyGlyph();
        if (glyph is null)
        {
            await vm.MainVM.View!.MessageDialog("The last glyph has zero size.\nYou can use \"Select a region of an empty glyph\" to fix that.", "Warning");
            return;
        }

        SelectGlyph(glyph);
    }
}
