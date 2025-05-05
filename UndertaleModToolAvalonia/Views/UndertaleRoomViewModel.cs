using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using UndertaleModLib;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleRoomViewModel : ViewModelBase
{
    public UndertaleRoom Room { get; set; }

    [ObservableProperty]
    private object? _RoomItemsSelectedItem;

    [ObservableProperty]
    private object? _PropertiesContent;

    partial void OnRoomItemsSelectedItemChanged(object? value) {
        PropertiesContent = value switch
        {
            TreeViewItem { Name: "RoomTreeViewItem"} => Room,
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
