using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using SkiaSharp;
using UndertaleModLib;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModToolAvalonia;

public partial class UndertaleRoomViewModel : IUndertaleResourceViewModel
{
    public const uint TILE_ID =     0b00000000000001111111111111111111;
    public const uint TILE_FLIP_H = 0b00010000000000000000000000000000;
    public const uint TILE_FLIP_V = 0b00100000000000000000000000000000;
    public const uint TILE_ROTATE = 0b01000000000000000000000000000000;

    public MainViewModel MainVM;
    public UndertaleResource Resource => Room;
    public UndertaleRoom Room { get; set; }

    public ObservableCollection<RoomTreeItem> RoomTreeItems { get; set; } = [];

    [Notify]
    private object? _RoomItemsSelectedItem;

    [Notify]
    private object? _PropertiesContent;

    [Notify]
    private object? _CategorySelected;

    [Notify]
    private string _StatusText = "";

    [Notify]
    private bool _IsSelectAnyLayerEnabled = false;

    [Notify]
    private bool _IsGridEnabled = false;
    [Notify]
    private uint _GridWidth = 20;
    [Notify]
    private uint _GridHeight = 20;
    [Notify]
    private double _Zoom = 1;

    [Notify]
    private uint _SelectedTileData = 0;
    [Notify]
    private uint _TileSetColumns = 0;

    public UndertaleRoomViewModel(UndertaleRoom room, IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();

        Room = room;

        IsSelectAnyLayerEnabled = MainVM.Settings!.EnableSelectAnyLayerByDefault;
        IsGridEnabled = MainVM.Settings!.EnableRoomGridByDefault;
        GridWidth = MainVM.Settings!.DefaultRoomGridWidth;
        GridHeight = MainVM.Settings!.DefaultRoomGridHeight;

        bool isGMS2 = MainVM.Data!.IsVersionAtLeast(2);

        if (!isGMS2)
            RoomTreeItems.Add(new("Backgrounds", "Backgrounds", Room.Backgrounds));

        RoomTreeItems.Add(new("Views", "Views", Room.Views));

        if (!isGMS2)
        {
            RoomTreeItems.Add(new("Game objects", "GameObjects", Room.GameObjects));
            RoomTreeItems.Add(new("Tiles", "Tiles", Room.Tiles));
        }

        if (isGMS2)
            RoomTreeItems.Add(new("Layers", "Layers", Room.Layers));

    }

    public void AddLayer(LayerType type)
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

    public void RemoveLayer(Layer layer)
    {
        if (layer.LayerType == LayerType.Instances)
        {
            // TODO: Remove from InstanceCreationOrderIDs
            foreach (UndertaleRoom.GameObject? instance in layer.InstancesData.Instances)
            {
                Room.GameObjects.Remove(instance);
            }
        }

        Room.Layers.Remove(layer);
    }

    public void AddGameObjectInstance(Layer? layer = null)
    {
        GameObject instance = new()
        {
            InstanceID = MainVM.Data!.GeneralInfo.LastObj++,
        };
        Room.GameObjects.Add(instance);

        layer?.InstancesData.Instances.Add(instance);
    }

    public void RemoveGameObjectInstance(GameObject instance, Layer? layer = null)
    {
        // TODO: Remove from InstanceCreationOrderIDs
        layer?.InstancesData.Instances.Remove(instance);
        Room.GameObjects.Remove(instance);
    }

    public void AddTile()
    {
        Tile tile = new()
        {
            InstanceID = MainVM.Data!.GeneralInfo.LastTile++,
        };
        Room.Tiles.Add(tile);
    }

    public void RemoveTile(Tile tile, UndertalePointerList<Tile>? legacyTilesList = null)
    {
        if (legacyTilesList is not null)
        {
            legacyTilesList.Remove(tile);
        }
        else
        {
            Room.Tiles.Remove(tile);
        }
    }

    public void AddLegacyTileInstance(Layer layer)
    {
        Tile tile = new()
        {
            InstanceID = MainVM.Data!.GeneralInfo.LastTile++,
            spriteMode = true,
        };

        layer.AssetsData.LegacyTiles.Add(tile);
    }

    public void AddSpriteInstance(Layer layer)
    {
        SpriteInstance spriteInstance = new()
        {
            Name = SpriteInstance.GenerateRandomName(MainVM.Data),
        };

        layer.AssetsData.Sprites.Add(spriteInstance);
    }

    public void AddSequenceInstance(Layer layer)
    {
        SequenceInstance sequenceInstance = new()
        {
            // Uses the same naming scheme as a sprite
            Name = SpriteInstance.GenerateRandomName(MainVM.Data),
        };

        layer.AssetsData.Sequences?.Add(sequenceInstance);
    }

    public void AddParticleSystemInstance(Layer layer)
    {
        ParticleSystemInstance particleSystemInstance = new()
        {
            Name = ParticleSystemInstance.GenerateRandomName(MainVM.Data),
            InstanceID = ++MainVM.Data!.LastParticleSystemInstanceID,
        };

        layer.AssetsData.ParticleSystems?.Add(particleSystemInstance);
    }

    public void AddTextItemInstance(Layer layer)
    {
        TextItemInstance textItemInstance = new()
        {
            Name = TextItemInstance.GenerateRandomName(MainVM.Data),
        };

        layer.AssetsData.TextItems?.Add(textItemInstance);
    }

    public void RemoveAsset(IList list, object asset)
    {
        list.Remove(asset);
    }

    public object? FindItemFromCategory(object? category)
    {
        if ("GameObjects".Equals(category))
            return RoomTreeItems.First(x => x.Tag == "GameObjects");
        if ("Tiles".Equals(category))
            return RoomTreeItems.First(x => x.Tag == "Tiles");
        return category;
    }

    public object? FindCategoryOfItem(object? item)
    {
        // NOTE: This sucks. Ideally we'd have this information from the DataContext of the item directly.
        if (item is null)
            return null;

        bool isGMS2 = MainVM.Data!.IsVersionAtLeast(2);

        object? category = item switch
        {
            RoomTreeItem { Tag: "GameObjects" } => "GameObjects",
            GameObject => !isGMS2 ? "GameObjects" : null,
            RoomTreeItem { Tag: "Tiles" } => "Tiles",
            Tile => !isGMS2 ? "Tiles" : null,
            RoomTreeItem => null,
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

    public async void AutoSizeTileLayer()
    {
        if (PropertiesContent is Layer layer)
        {
            if (layer.TilesData is null
                || layer.TilesData.Background is null
                || layer.TilesData.Background.GMS2TileWidth == 0
                || layer.TilesData.Background.GMS2TileHeight == 0)
                return;

            layer.TilesData.TilesX = (uint)Math.Ceiling((double)Room.Width / layer.TilesData.Background.GMS2TileWidth);
            layer.TilesData.TilesY = (uint)Math.Ceiling((double)Room.Height / layer.TilesData.Background.GMS2TileHeight);
        }
    }

    public void SelectedTileDataFlipX()
    {
        SelectedTileData ^= ((SelectedTileData & TILE_ROTATE) == 0) ? TILE_FLIP_H : TILE_FLIP_V;
    }

    public void SelectedTileDataFlipY()
    {
        SelectedTileData ^= ((SelectedTileData & TILE_ROTATE) == 0) ? TILE_FLIP_V : TILE_FLIP_H;
    }

    public void SelectedTileDataRotateClockwise()
    {
        if ((SelectedTileData & TILE_ROTATE) != 0)
        {
            SelectedTileData ^= TILE_ROTATE | TILE_FLIP_H | TILE_FLIP_V;
        }
        else
        {
            SelectedTileData ^= TILE_ROTATE;
        }
    }

    public async void SaveAsImage()
    {
        IStorageFile? file = await MainVM.View!.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Save image",
            FileTypeChoices = [
                new FilePickerFileType("PNG files (.png)")
                {
                    Patterns = ["*.png"],
                },
                new FilePickerFileType("All files")
                {
                    Patterns = ["*"],
                },
            ],
            DefaultExtension = ".png",
            SuggestedFileName = $"{Room.Name?.Content ?? "Room"}.png",
        });

        if (file is null)
            return;

        using Stream stream = await file.OpenWriteAsync();

        var bitmap = new SKBitmap((int)Room.Width, (int)Room.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var canvas = new SKCanvas(bitmap);

        var updater = new UndertaleRoomEditor.Updater();
        updater.Room = Room;
        updater.Update();

        var renderer = new UndertaleRoomEditor.Renderer();
        renderer.Room = Room;
        renderer.RoomItems = updater.RoomItems;
        renderer.Canvas = canvas;
        renderer.RenderRoom();

        var result = bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        if (!result)
            throw new InvalidOperationException();
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
            RoomTreeItem => null,
            object o => o,
            _ => null,
        };

        CategorySelected = FindCategoryOfItem(RoomItemsSelectedItem);
    }

    public class RoomTreeItem(string header, string tag, IEnumerable itemsSource)
    {
        public string Header { get; set; } = header;
        public string Tag { get; set; } = tag;
        public IEnumerable ItemsSource { get; set; } = itemsSource;
    }
}
