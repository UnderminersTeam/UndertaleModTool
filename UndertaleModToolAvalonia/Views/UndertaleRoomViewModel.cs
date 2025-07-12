using Avalonia.Controls;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleRoomViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => Room;
    public UndertaleRoom Room { get; set; }

    [Notify]
    private object? _RoomItemsSelectedItem;

    [Notify]
    private object? _PropertiesContent;

    private void OnRoomItemsSelectedItemChanged()
    {
        PropertiesContent = RoomItemsSelectedItem switch
        {
            TreeViewItem { Name: "RoomTreeViewItem" } => Room,
            TreeViewItem => null,
            UndertalePointerList<Tile> => null,
            UndertalePointerList<SpriteInstance> => null,
            UndertalePointerList<SequenceInstance> => null,
            UndertalePointerList<ParticleSystemInstance> => null,
            UndertalePointerList<TextItemInstance> => null,
            object o => o,
            _ => null,
        };
    }

    public UndertaleRoomViewModel(UndertaleRoom room)
    {
        Room = room;
    }
}
