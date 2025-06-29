using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleSpriteViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => Sprite;
    public UndertaleSprite Sprite { get; set; }

    [Notify]
    private UndertaleSprite.TextureEntry? _TexturesSelected;
    [Notify]
    private UndertaleSprite.MaskEntry? _CollisionMasksSelected;

    public UndertaleSpriteViewModel(UndertaleSprite sprite)
    {
        Sprite = sprite;
    }

    public void TexturesSelectedChanged(object? item)
    {
        TexturesSelected = (UndertaleSprite.TextureEntry?)item!;
    }
    public void CollisionMasksSelectedChanged(object? item)
    {
        CollisionMasksSelected = (UndertaleSprite.MaskEntry?)item!;
    }

    public static UndertaleSprite.TextureEntry CreateTextureEntry() => new();
    public static UndertaleSprite.MaskEntry CreateMaskEntry() => new();
}
