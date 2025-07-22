using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Views;

namespace UndertaleModToolAvalonia.Helpers;

public class RoomItemToContextMenuConverter : IValueConverter
{
    public ContextMenu? GameObjectListMenu { get; set; }
    public ContextMenu? GameObjectMenu { get; set; }
    public ContextMenu? TileListMenu { get; set; }
    public ContextMenu? TileMenu { get; set; }
    public ContextMenu? LayersListMenu { get; set; }
    public ContextMenu? GenericLayerMenu { get; set; }
    public ContextMenu? InstancesLayerMenu { get; set; }
    public ContextMenu? AssetsLayerMenu { get; set; }
    public ContextMenu? SpriteInstanceMenu { get; set; }
    public ContextMenu? SequenceInstanceMenu { get; set; }
    public ContextMenu? ParticleSystemInstanceMenu { get; set; }
    public ContextMenu? TextItemInstanceMenu { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value)
        {
            case UndertaleRoomViewModel.RoomItem { Tag: "GameObjects" }:
                return GameObjectListMenu;
            case UndertaleRoom.GameObject:
                return GameObjectMenu;
            case UndertaleRoomViewModel.RoomItem { Tag: "Tiles" }:
                return TileListMenu;
            case UndertaleRoom.Tile:
                return TileMenu;
            case UndertaleRoomViewModel.RoomItem { Tag: "Layers" }:
                return LayersListMenu;
            case UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Instances }:
                return InstancesLayerMenu;
            case UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Assets }:
                return AssetsLayerMenu;
            case UndertaleRoom.Layer:
                return GenericLayerMenu;
            case UndertaleRoom.SpriteInstance:
                return SpriteInstanceMenu;
            case UndertaleRoom.SequenceInstance:
                return SequenceInstanceMenu;
            case UndertaleRoom.ParticleSystemInstance:
                return ParticleSystemInstanceMenu;
            case UndertaleRoom.TextItemInstance:
                return TextItemInstanceMenu;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
