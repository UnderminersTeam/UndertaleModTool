using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia;

public partial class UndertaleRoomViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => Room;
    public UndertaleRoom Room { get; set; }

    public ObservableCollection<RoomItem> RoomItems { get; set; } = [];

    [Notify]
    private object? _RoomItemsSelectedItem;

    [Notify]
    private object? _PropertiesContent;

    [Notify]
    private object? _CategorySelected;

    [Notify]
    private double _Zoom = 1;

    [Notify]
    private uint _SelectedTileData = 0;

    public UndertaleRoomViewModel(UndertaleRoom room, IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();

        Room = room;

        bool isGMS2 = MainVM.Data!.IsVersionAtLeast(2);

        if (!isGMS2)
            RoomItems.Add(new("Backgrounds", "Backgrounds", Room.Backgrounds));

        RoomItems.Add(new("Views", "Views", Room.Views));

        if (!isGMS2)
        {
            RoomItems.Add(new("Game objects", "GameObjects", Room.GameObjects));
            RoomItems.Add(new("Tiles", "Tiles", Room.Tiles));
        }

        if (isGMS2)
            RoomItems.Add(new("Layers", "Layers", Room.Layers));

    }

    public void AddLayer(UndertaleRoom.LayerType type)
    {
        // TODO: Move this to library
        string name = $"New {type switch
        {
            LayerType.Background => "background",
            LayerType.Instances => "instances",
            LayerType.Assets => "assets",
            LayerType.Tiles => "tiles",
            LayerType.Effect => "effect",
            LayerType.Path2 => "path",
            _ => "unknown",
        }} layer";

        uint layerId = 0;
        foreach (UndertaleRoom? room in MainVM.Data!.Rooms)
        {
            if (room is null)
                continue;
            foreach (Layer roomLayer in room.Layers)
            {
                if (roomLayer.LayerId > layerId)
                    layerId = roomLayer.LayerId;
            }
        }
        layerId += 1;

        int layerDepth = 0;
        if (Room.Layers.Count > 0)
            layerDepth = Room.Layers.Select(layer => layer.LayerDepth).Max() + 1;

        Layer layer = new()
        {
            LayerName = MainVM.Data!.Strings.MakeString(name, createNew: true),
            LayerId = layerId,
            LayerDepth = (int)layerDepth,
            LayerType = type,
            Data = type switch
            {
                LayerType.Background => new Layer.LayerBackgroundData(),
                LayerType.Instances => new Layer.LayerInstancesData(),
                LayerType.Assets => new Layer.LayerAssetsData(),
                LayerType.Tiles => new Layer.LayerTilesData(),
                LayerType.Effect => new Layer.LayerEffectData(),
                _ => null,
            },
            ParentRoom = Room,
        };

        if (layer.LayerType == LayerType.Assets)
        {
            layer.AssetsData.LegacyTiles ??= new UndertalePointerList<Tile>();
            layer.AssetsData.Sprites ??= new UndertalePointerList<SpriteInstance>();
            layer.AssetsData.Sequences ??= new UndertalePointerList<SequenceInstance>();

            if (!MainVM.Data.IsVersionAtLeast(2, 3, 2))
                layer.AssetsData.NineSlices ??= new UndertalePointerList<SpriteInstance>();

            if (MainVM.Data.IsNonLTSVersionAtLeast(2023, 2))
                layer.AssetsData.ParticleSystems ??= new UndertalePointerList<ParticleSystemInstance>();

            if (MainVM.Data.IsVersionAtLeast(2024, 6))
                layer.AssetsData.TextItems ??= new UndertalePointerList<TextItemInstance>();

            layer.AssetsData.InitializeAllAssets();
        }
        else if (layer.LayerType == LayerType.Tiles)
        {
            layer.TilesData.TileData ??= [];
        }

        Room.Layers.Add(layer);
    }

    public object? FindItemCategory(object? item)
    {
        // NOTE: This sucks. Ideally we'd have this information from the DataContext of the item directly.
        if (item is null)
            return null;

        bool isGMS2 = MainVM.Data!.IsVersionAtLeast(2);

        object? category = item switch
        {
            RoomItem { Tag: "GameObjects" } => RoomItems.First(x => x.Tag == "GameObjects"),
            GameObject => !isGMS2 ? RoomItems.First(x => x.Tag == "GameObjects") : null,
            RoomItem { Tag: "Tiles" } => RoomItems.First(x => x.Tag == "Tiles"),
            Tile => !isGMS2 ? RoomItems.First(x => x.Tag == "Tiles") : null,
            RoomItem => null,
            _ => null,
        };

        if (category is not null)
            return category;

        foreach (var layer in Room.Layers)
        {
            if (layer.LayerType == LayerType.Instances)
            {
                if (item == layer)
                    return layer;

                var instance = layer.InstancesData.Instances.FirstOrDefault(x => x == item);
                if (instance is not null)
                    return layer;
            }
            else if (layer.LayerType == LayerType.Assets)
            {
                if (item == layer)
                    return layer;

                foreach (IEnumerable<object> assetTypeList in layer.AssetsData.AllAssets.Cast<IEnumerable<object>>())
                {
                    if (item == assetTypeList)
                        return layer;

                    var instance = assetTypeList.FirstOrDefault(x => x == item);
                    if (instance is not null)
                        return layer;
                }
            }
            else if (layer.LayerType == LayerType.Tiles)
            {
                if (item == layer)
                    return layer;
            }
        }

        return null;
    }

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
            RoomItem => null,
            object o => o,
            _ => null,
        };

        CategorySelected = FindItemCategory(RoomItemsSelectedItem);
    }

    public class RoomItem(string header, string tag, IEnumerable itemsSource)
    {
        public string Header { get; set; } = header;
        public string Tag { get; set; } = tag;
        public IEnumerable ItemsSource { get; set; } = itemsSource;
    }
}
