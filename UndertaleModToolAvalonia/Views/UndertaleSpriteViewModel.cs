using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleSpriteViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => Sprite;
    public UndertaleSprite Sprite { get; set; }

    public UndertaleSpriteViewModel(UndertaleSprite sprite)
    {
        Sprite = sprite;
    }

    public static UndertaleSprite.TextureEntry CreateTextureEntry() => new();
    public static UndertaleSprite.MaskEntry CreateMaskEntry() => new();
}
