using System;
using System.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleUIRootNodeView : UserControl
{
    public UndertaleUIRootNodeView()
    {
        InitializeComponent();
    }

    private void MoveUp_Click(object? sender, RoutedEventArgs e)
    {
        MoveSelectedItem(-1);
    }

    private void MoveDown_Click(object? sender, RoutedEventArgs e)
    {
        MoveSelectedItem(1);
    }

    private void MoveSelectedItem(int distance)
    {
        if (DataContext is UndertaleUIRootNodeViewModel vm)
        {
            var treeViewItem = UndertaleRoomView.GetTreeViewItem(NodesTreeView, NodesTreeView.SelectedItem);
            if (treeViewItem is null)
                return;

            IList? list = null;
            Action<int, int>? moveAction = null;

            TreeViewItem? parentTreeViewItem = treeViewItem.FindLogicalAncestorOfType<TreeViewItem>();

            if (parentTreeViewItem?.DataContext is UndertaleUINode node && node.Node is UndertaleUIContainer container)
            {
                list = container.Children;
                moveAction = container.Children.Move;
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

    void AddToSelected(object? sender, IUndertaleUINodeData innerNode)
    {
        if (DataContext is UndertaleUIRootNodeViewModel vm
            && sender is Control control
            && control.FindLogicalAncestorOfType<TreeViewItem>() is TreeViewItem treeViewItem
            && treeViewItem.DataContext is UndertaleUINode currentNode)
        {
            if (currentNode.Node is UndertaleUIContainer container)
            {
                container.Children.Add(new UndertaleUINode() { Node = innerNode });
            }
        }
    }

    private void ContextMenu_AddFlexPanel_Click(object? sender, RoutedEventArgs e)
    {
        AddToSelected(sender, new UndertaleUIFlexPanel());
    }

    private void ContextMenu_AddGameObject_Click(object? sender, RoutedEventArgs e)
    {
        AddToSelected(sender, new UndertaleUIGameObject());
    }

    private void ContextMenu_AddSequenceInstance_Click(object? sender, RoutedEventArgs e)
    {
        AddToSelected(sender, new UndertaleUISequenceInstance());
    }

    private void ContextMenu_AddSpriteInstance_Click(object? sender, RoutedEventArgs e)
    {
        AddToSelected(sender, new UndertaleUISpriteInstance());
    }

    private void ContextMenu_AddTextItemInstance_Click(object? sender, RoutedEventArgs e)
    {
        AddToSelected(sender, new UndertaleUITextItemInstance());
    }

    private void ContextMenu_AddEffectLayer_Click(object? sender, RoutedEventArgs e)
    {
        AddToSelected(sender, new UndertaleUIEffectLayer());
    }

    private void ContextMenu_Remove_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleUIRootNodeViewModel vm
            && sender is Control control
            && control.FindLogicalAncestorOfType<TreeViewItem>() is TreeViewItem treeViewItem
            && treeViewItem.DataContext is UndertaleUINode currentNode)
        {
            TreeViewItem? parentTreeViewItem = treeViewItem.FindLogicalAncestorOfType<TreeViewItem>();
            if (parentTreeViewItem?.DataContext is UndertaleUINode node && node.Node is UndertaleUIContainer container)
            {
                container.Children.Remove(currentNode);
            }
        }
    }
}