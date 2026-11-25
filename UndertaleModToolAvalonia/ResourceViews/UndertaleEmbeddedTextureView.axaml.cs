using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleEmbeddedTextureView : UserControl
{
    UndertaleTexturePageItem? hoveredItem;

    // Only a click that also started on the texture opens an item, not a drag (e.g. from the scrollbar) that ends over it.
    bool isPressedOnTexture;

    public UndertaleEmbeddedTextureView()
    {
        InitializeComponent();
    }

    private void TextureImage_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (DataContext is not UndertaleEmbeddedTextureViewModel vm)
            return;

        // Width is -1 when the texture has no image.
        int textureWidth = vm.EmbeddedTexture.TextureData.Width;
        if (textureWidth <= 0)
            return;

        // The image can be zoomed with Ctrl+scroll, so convert between screen and texture pixels.
        double scale = TextureImage.Bounds.Width / textureWidth;
        Point position = e.GetPosition(TextureImage);
        hoveredItem = vm.FindItemAt(position.X / scale, position.Y / scale);

        if (hoveredItem is null)
        {
            HoveredItemBorder.IsVisible = false;
            return;
        }

        Canvas.SetLeft(HoveredItemBorder, hoveredItem.SourceX * scale);
        Canvas.SetTop(HoveredItemBorder, hoveredItem.SourceY * scale);
        HoveredItemBorder.Width = hoveredItem.SourceWidth * scale;
        HoveredItemBorder.Height = hoveredItem.SourceHeight * scale;
        HoveredItemBorder.IsVisible = true;
    }

    private void TextureImage_PointerExited(object? sender, PointerEventArgs e)
    {
        hoveredItem = null;
        HoveredItemBorder.IsVisible = false;
    }

    private void TextureImage_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPointProperties properties = e.GetCurrentPoint(TextureImage).Properties;
        isPressedOnTexture = properties.IsLeftButtonPressed || properties.IsMiddleButtonPressed;
    }

    private void TextureImage_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        bool isClick = isPressedOnTexture;
        isPressedOnTexture = false;
        if (!isClick || hoveredItem is null || DataContext is not UndertaleEmbeddedTextureViewModel vm)
            return;

        if (e.InitialPressMouseButton == MouseButton.Left)
            vm.OpenItem(hoveredItem, inNewTab: false);
        else if (e.InitialPressMouseButton == MouseButton.Middle)
            vm.OpenItem(hoveredItem, inNewTab: true);
    }
}
