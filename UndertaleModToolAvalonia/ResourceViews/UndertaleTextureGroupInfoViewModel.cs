using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public class UndertaleTextureGroupInfoViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => TextureGroupInfo;
    public UndertaleTextureGroupInfo TextureGroupInfo { get; set; }

    public UndertaleTextureGroupInfoViewModel(UndertaleTextureGroupInfo textureGroupInfo)
    {
        TextureGroupInfo = textureGroupInfo;
    }

    public static UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR> CreateEmbeddedTextureItem() => new();
    public static UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> CreateSpriteItem() => new();
    public static UndertaleResourceById<UndertaleFont, UndertaleChunkFONT> CreateFontItem() => new();
    public static UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND> CreateBackgroundItem() => new();
}
