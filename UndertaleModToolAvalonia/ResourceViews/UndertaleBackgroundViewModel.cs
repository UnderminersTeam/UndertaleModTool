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

    public void AutoTileIDs()
    {
        Background.GMS2TileIds.Clear();

        for (uint i = 0; i < Background.GMS2TileCount; i++)
            for (uint j = 0; j < Background.GMS2ItemsPerTileCount; j++)
                Background.GMS2TileIds.Add(new UndertaleBackground.TileID() { ID = i });
    }
}
