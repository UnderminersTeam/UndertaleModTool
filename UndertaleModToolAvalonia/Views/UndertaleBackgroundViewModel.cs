using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public class UndertaleBackgroundViewModel
{
    public UndertaleBackground Background { get; set; }

    public UndertaleBackgroundViewModel(UndertaleBackground background)
    {
        Background = background;
    }

    public static UndertaleBackground.TileID CreateTileID() => new();
}
