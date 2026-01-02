using System;
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

    private void MoveUp_Click(object? sender, RoutedEventArgs e)
    {
        MoveSelectedRoomItem(-1);
    }

    private void MoveDown_Click(object? sender, RoutedEventArgs e)
    {
        MoveSelectedRoomItem(1);
    }

    private void MoveSelectedRoomItem(int distance)
    {
        if (DataContext is UndertaleRoomViewModel vm)
        {
            var treeViewItem = GetTreeViewItem(RoomItemsTreeView, RoomItemsTreeView.SelectedItem);
            if (treeViewItem is null)
                return;

            IList? list = null;
            Action<int, int>? moveAction = null;

            TreeViewItem? parentTreeViewItem = treeViewItem.FindLogicalAncestorOfType<TreeViewItem>();

            switch (parentTreeViewItem?.DataContext)
            {
                case UndertaleRoomViewModel.RoomTreeItem { Tag: "GameObjects" }:
                    list = vm.Room.GameObjects;
                    moveAction = vm.Room.GameObjects.Move;
                    break;
                case UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Instances } layer:
                    list = layer.InstancesData.Instances;
                    moveAction = layer.InstancesData.Instances.Move;
                    break;
                case UndertaleRoomViewModel.RoomTreeItem { Tag: "Tiles" }:
                    list = vm.Room.Tiles;
                    moveAction = vm.Room.Tiles.Move;
                    break;
                case UndertalePointerList<UndertaleRoom.Tile> legacyTiles:
                    list = legacyTiles;
                    moveAction = legacyTiles.Move;
                    break;
                case UndertalePointerList<UndertaleRoom.SpriteInstance> sprites:
                    list = sprites;
                    moveAction = sprites.Move;
                    break;
                case UndertalePointerList<UndertaleRoom.SequenceInstance> sequences:
                    list = sequences;
                    moveAction = sequences.Move;
                    break;
                case UndertalePointerList<UndertaleRoom.ParticleSystemInstance> particleSystems:
                    list = particleSystems;
                    moveAction = particleSystems.Move;
                    break;
                case UndertalePointerList<UndertaleRoom.TextItemInstance> textItems:
                    list = textItems;
                    moveAction = textItems.Move;
                    break;
            }

            if (list is not null && moveAction is not null)
            {
                var item = treeViewItem.DataContext;
                var oldIndex = list.IndexOf(item);
                var newIndex = oldIndex + distance;

                if (oldIndex != -1 && newIndex >= 0 && newIndex < list.Count)
                {
                    moveAction(oldIndex, newIndex);
                }
            }
        }
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
            if (parentTreeViewItem?.DataContext is UndertaleRoomViewModel.RoomTreeItem { Tag: "GameObjects" })
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
            if (parentTreeViewItem?.DataContext is UndertaleRoomViewModel.RoomTreeItem { Tag: "Tiles" })
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
                InstanceID = ++vm.MainVM.Data!.LastParticleSystemInstanceID,
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

    private void ContextMenu_Copy_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm
            && sender is Control control
            && control.FindLogicalAncestorOfType<TreeViewItem>() is TreeViewItem treeViewItem)
        {
            vm.MainVM.InternalClipboard = treeViewItem.DataContext;
        }
    }

    private void ContextMenu_Paste_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleRoomViewModel vm
            && sender is Control control
            && control.FindLogicalAncestorOfType<TreeViewItem>() is TreeViewItem treeViewItem)
        {
            if (vm.MainVM.InternalClipboard is UndertaleRoom.GameObject instance)
            {
                // Find if called from "Game objects" list or from an instances layer or instance itself

                object? where = treeViewItem?.DataContext;
                UndertaleRoom.GameObject? selectedInstance = null;

                if (where is UndertaleRoom.GameObject _selectedInstance)
                {
                    TreeViewItem? parentTreeViewItem = treeViewItem.FindLogicalAncestorOfType<TreeViewItem>();
                    if (parentTreeViewItem is not null)
                    {
                        where = parentTreeViewItem.DataContext;
                        selectedInstance = _selectedInstance;
                    }
                }

                if (where is UndertaleRoomViewModel.RoomTreeItem { Tag: "GameObjects" })
                {
                    int index = selectedInstance is not null ? vm.Room.GameObjects.IndexOf(selectedInstance) : vm.Room.GameObjects.Count;

                    UndertaleRoom.GameObject newInstance = instance.Clone();
                    newInstance.InstanceID = vm.MainVM.Data!.GeneralInfo.LastObj++;

                    vm.Room.GameObjects.Insert(index, newInstance);
                }
                else if (where is UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Instances } layer)
                {
                    int index = selectedInstance is not null ? layer.InstancesData.Instances.IndexOf(selectedInstance) : layer.InstancesData.Instances.Count;

                    UndertaleRoom.GameObject newInstance = instance.Clone();
                    newInstance.InstanceID = vm.MainVM.Data!.GeneralInfo.LastObj++;

                    vm.Room.GameObjects.Add(newInstance);
                    layer.InstancesData.Instances.Insert(index, newInstance);
                }
            }
        }
    }

    // Gets a TreeViewItem even if it's not "realized" yet.
    // NOTE: Mostly taken from:
    // https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-find-a-treeviewitem-in-a-treeview
    // So I'm not sure exactly why and how this works.
    private static TreeViewItem? GetTreeViewItem(ItemsControl? container, object? item)
    {
        if (container != null && item != null)
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