using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public class UndertaleSpriteViewModel
{
    public UndertaleSprite Sprite { get; set; }

    public UndertaleSpriteViewModel(UndertaleSprite sprite)
    {
        Sprite = sprite;
    }

    public static UndertaleSprite.TextureEntry CreateTextureEntry() => new();
    public static UndertaleSprite.MaskEntry CreateMaskEntry() => new();
}
