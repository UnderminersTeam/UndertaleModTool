using System.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleRoomView : UserControl
{
    public UndertaleRoomView()
    {
        InitializeComponent();

        DataContextChanged += (_, __) =>
        {
            if (DataContext is UndertaleRoomViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(UndertaleRoomViewModel.RoomItemsSelectedItem))
                    {
                        var item = vm.RoomItemsSelectedItem;
                        if (item is not null)
                        {
                            TreeViewItem? treeViewItem = GetTreeViewItem(RoomItemsTreeView, item);

                            if (treeViewItem is null)
                                return;

                            // Recursively expand parents of this item
                            TreeViewItem currentViewItem = treeViewItem;
                            while (currentViewItem.Parent is TreeViewItem parentTreeViewItem)
                            {
                                parentTreeViewItem.IsExpanded = true;
                                currentViewItem = parentTreeViewItem;
                            }

                            treeViewItem.BringIntoView();
                        }
                    }
                };
            }
        };
    }

    private void ContextMenu_AddGameObjectInstance_GameObjectList_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm)
        {
            // TODO: Move to view model
            UndertaleRoom.GameObject instance = new()
            {
                InstanceID = vm.MainVM.Data!.GeneralInfo.LastObj++,
            };
            vm.Room.GameObjects.Add(instance);
        }
    }

    private void ContextMenu_RemoveGameObjectInstance_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm
            && sender is Control control
            && control.FindLogicalAncestorOfType<TreeViewItem>() is TreeViewItem treeViewItem
            && treeViewItem.DataContext is UndertaleRoom.GameObject instance)
        {
            // Find if called from "Game objects" list or from an instances layer
            TreeViewItem? parentTreeViewItem = treeViewItem.FindLogicalAncestorOfType<TreeViewItem>();
            if (parentTreeViewItem?.DataContext is UndertaleRoomViewModel.RoomItem { Tag: "GameObjects" })
            {
                // TODO: Move to view model
                vm.Room.GameObjects.Remove(instance);
            }
            else if (parentTreeViewItem?.DataContext is UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Instances } layer)
            {
                // TODO: Move to view model
                // TODO: Remove from InstanceCreationOrderIDs
                layer.InstancesData.Instances.Remove(instance);
                vm.Room.GameObjects.Remove(instance);
            }
        }
    }

    private void ContextMenu_AddTile_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm)
        {
            // TODO: Move to view model
            UndertaleRoom.Tile tile = new()
            {
                InstanceID = vm.MainVM.Data!.GeneralInfo.LastTile++,
            };
            vm.Room.Tiles.Add(tile);
        }
    }

    private void ContextMenu_RemoveTile_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm
            && sender is Control control
            && control.FindLogicalAncestorOfType<TreeViewItem>() is TreeViewItem treeViewItem
            && treeViewItem.DataContext is UndertaleRoom.Tile tile)
        {
            // Find if called from "Tiles" list or from an asset layer
            TreeViewItem? parentTreeViewItem = treeViewItem.FindLogicalAncestorOfType<TreeViewItem>();
            if (parentTreeViewItem?.DataContext is UndertaleRoomViewModel.RoomItem { Tag: "Tiles" })
            {
                // TODO: Move to view model
                vm.Room.Tiles.Remove(tile);
            }
            else if (parentTreeViewItem?.DataContext is UndertalePointerList<UndertaleRoom.Tile> legacyTiles)
            {
                // TODO: Move to view model
                legacyTiles.Remove(tile);
            }
        }
    }

    private void ContextMenu_AddBackgroundLayer_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm)
            vm.AddLayer(UndertaleRoom.LayerType.Background);
    }

    private void ContextMenu_AddInstancesLayer_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm)
            vm.AddLayer(UndertaleRoom.LayerType.Instances);
    }

    private void ContextMenu_AddTilesLayer_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm)
            vm.AddLayer(UndertaleRoom.LayerType.Tiles);
    }

    private void ContextMenu_AddPathLayer_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm)
            vm.AddLayer(UndertaleRoom.LayerType.Path2);
    }

    private void ContextMenu_AddAssetsLayer_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm)
            vm.AddLayer(UndertaleRoom.LayerType.Assets);
    }

    private void ContextMenu_AddEffectLayer_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm)
            vm.AddLayer(UndertaleRoom.LayerType.Effect);
    }

    private void ContextMenu_RemoveLayer_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm
            && sender is Control control
            && control.FindLogicalAncestorOfType<TreeViewItem>() is TreeViewItem treeViewItem
            && treeViewItem.DataContext is UndertaleRoom.Layer layer)
        {
            // TODO: Move to view model
            if (layer.LayerType == UndertaleRoom.LayerType.Instances)
            {
                // TODO: Remove from InstanceCreationOrderIDs
                foreach (UndertaleRoom.GameObject? instance in layer.InstancesData.Instances)
                    vm.Room.GameObjects.Remove(instance);
            }

            vm.Room.Layers.Remove(layer);
        }
    }

    private void ContextMenu_AddGameObjectInstance_InstancesLayer_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm
            && sender is Control control
            && control.FindLogicalAncestorOfType<TreeViewItem>() is TreeViewItem treeViewItem
            && treeViewItem.DataContext is UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Instances } layer)
        {
            // TODO: Move to view model
            UndertaleRoom.GameObject instance = new()
            {
                InstanceID = vm.MainVM.Data!.GeneralInfo.LastObj++,
            };
            vm.Room.GameObjects.Add(instance);

            layer.InstancesData.Instances.Add(instance);
        }
    }

    private void ContextMenu_AddLegacyTileInstance_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm
            && sender is Control control
            && control.FindLogicalAncestorOfType<TreeViewItem>() is TreeViewItem treeViewItem
            && treeViewItem.DataContext is UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Assets } layer)
        {
            // TODO: Move to view model
            UndertaleRoom.Tile tile = new()
            {
                InstanceID = vm.MainVM.Data!.GeneralInfo.LastTile++,
                spriteMode = true,
            };

            layer.AssetsData.LegacyTiles.Add(tile);
        }
    }

    private void ContextMenu_AddSpriteInstance_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm
            && sender is Control control
            && control.FindLogicalAncestorOfType<TreeViewItem>() is TreeViewItem treeViewItem
            && treeViewItem.DataContext is UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Assets } layer)
        {
            // TODO: Move to view model
            UndertaleRoom.SpriteInstance spriteInstance = new()
            {
                Name = UndertaleRoom.SpriteInstance.GenerateRandomName(vm.MainVM.Data),
            };

            layer.AssetsData.Sprites.Add(spriteInstance);
        }
    }

    private void ContextMenu_AddSequenceInstance_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm
            && sender is Control control
            && control.FindLogicalAncestorOfType<TreeViewItem>() is TreeViewItem treeViewItem
            && treeViewItem.DataContext is UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Assets } layer)
        {
            // TODO: Move to view model
            UndertaleRoom.SequenceInstance sequenceInstance = new()
            {
                // Uses the same naming scheme as a sprite
                Name = UndertaleRoom.SpriteInstance.GenerateRandomName(vm.MainVM.Data),
            };

            if (layer.AssetsData.Sequences is not null)
                layer.AssetsData.Sequences.Add(sequenceInstance);
        }
    }

    private void ContextMenu_AddParticleSystemInstance_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm
            && sender is Control control
            && control.FindLogicalAncestorOfType<TreeViewItem>() is TreeViewItem treeViewItem
            && treeViewItem.DataContext is UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Assets } layer)
        {
            // TODO: Move to view model
            UndertaleRoom.ParticleSystemInstance particleSystemInstance = new()
            {
                Name = UndertaleRoom.ParticleSystemInstance.GenerateRandomName(vm.MainVM.Data),
            };

            if (layer.AssetsData.ParticleSystems is not null)
                layer.AssetsData.ParticleSystems.Add(particleSystemInstance);
        }
    }

    private void ContextMenu_AddTextItemInstance_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm
            && sender is Control control
            && control.FindLogicalAncestorOfType<TreeViewItem>() is TreeViewItem treeViewItem
            && treeViewItem.DataContext is UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Assets } layer)
        {
            // TODO: Move to view model
            UndertaleRoom.TextItemInstance textItemInstance = new()
            {
                Name = UndertaleRoom.TextItemInstance.GenerateRandomName(vm.MainVM.Data),
            };

            if (layer.AssetsData.TextItems is not null)
                layer.AssetsData.TextItems.Add(textItemInstance);
        }
    }

    private void ContextMenu_RemoveAsset_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm
            && sender is Control control
            && control.FindLogicalAncestorOfType<TreeViewItem>() is TreeViewItem treeViewItem)
        {
            TreeViewItem? parentTreeViewItem = treeViewItem.FindLogicalAncestorOfType<TreeViewItem>();

            if (parentTreeViewItem?.DataContext is IList list)
            {
                // TODO: Move to view model
                list.Remove(treeViewItem.DataContext);
            }
        }
    }

    // Gets a TreeViewItem even if it's not "realized" yet.
    // NOTE: Mostly taken from:
    // https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-find-a-treeviewitem-in-a-treeview
    // So I'm not sure exactly why and how this works.
    private static TreeViewItem? GetTreeViewItem(ItemsControl container, object item)
    {
        if (container != null)
        {
            if (container.DataContext == item)
            {
                return container as TreeViewItem;
            }

            container.ApplyTemplate();

            ItemsPresenter? itemsPresenter = container.FindDescendantOfType<ItemsPresenter>();

            // This actually makes the child items.
            itemsPresenter?.ApplyTemplate();

            for (int i = 0, count = container.Items.Count; i < count; i++)
            {
                TreeViewItem? subContainer;
                subContainer = (TreeViewItem?)container.ContainerFromIndex(i);

                if (subContainer != null)
                {
                    TreeViewItem? resultContainer = GetTreeViewItem(subContainer, item);
                    if (resultContainer != null)
                    {
                        return resultContainer;
                    }
                }
            }
        }

        return null;
    }
}