using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public class UndertaleTexturePageItemViewModel
{
    public UndertaleTexturePageItem TexturePageItem { get; set; }

    public UndertaleTexturePageItemViewModel(UndertaleTexturePageItem texturePageItem)
    {
        TexturePageItem = texturePageItem;
    }
}
