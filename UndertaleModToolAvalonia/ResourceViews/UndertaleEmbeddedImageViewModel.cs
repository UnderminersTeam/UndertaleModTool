using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleEmbeddedImageViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => EmbeddedImage;
    public UndertaleEmbeddedImage EmbeddedImage { get; set; }

    public UndertaleEmbeddedImageViewModel(UndertaleEmbeddedImage embeddedImage)
    {
        EmbeddedImage = embeddedImage;
    }
}
