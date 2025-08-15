using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using UndertaleModToolAvalonia.Views;

namespace UndertaleModToolAvalonia.Helpers;

public class TabStripItemDropHandler : DropHandlerBase
{
    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (sourceContext is TabItemViewModel)
        {
            return true;
        }

        return false;
    }

    public override bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (targetContext is MainViewModel mainVM)
        {
            if (e.Source is Control control && sourceContext is TabItemViewModel draggedTabItem)
            {
                int draggedIndex = mainVM.Tabs.IndexOf(draggedTabItem);
                int droppedIndex = mainVM.Tabs.Count - 1;

                var sourceTabStripItem = control.FindLogicalAncestorOfType<TabStripItem>();
                if (sourceTabStripItem is not null && sourceTabStripItem.DataContext is TabItemViewModel droppedTabItem)
                {
                    droppedIndex = mainVM.Tabs.IndexOf(droppedTabItem);
                }

                MoveItem(mainVM.Tabs, draggedIndex, droppedIndex);

                mainVM.TabSelected = draggedTabItem;
                return true;
            }
        }

        return false;
    }
}
