using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public class UndertaleEmbeddedTextureViewModel
{
    public UndertaleEmbeddedTexture EmbeddedTexture { get; set; }

    public UndertaleEmbeddedTextureViewModel(UndertaleEmbeddedTexture embeddedTexture)
    {
        EmbeddedTexture = embeddedTexture;
    }
}
