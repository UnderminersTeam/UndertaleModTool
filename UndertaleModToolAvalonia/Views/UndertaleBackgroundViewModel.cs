using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleBackgroundViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => Background;
    public UndertaleBackground Background { get; set; }

    public UndertaleBackgroundViewModel(UndertaleBackground background)
    {
        Background = background;
    }

    public static UndertaleBackground.TileID CreateTileID() => new();
}
