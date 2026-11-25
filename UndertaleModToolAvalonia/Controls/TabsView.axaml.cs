using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace UndertaleModToolAvalonia;

public partial class TabsView : UserControl
{
    public TabsView()
    {
        InitializeComponent();
    }

    void TabControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is TabsViewModel vm)
        {
            object? tabSelected = e.AddedItems.Count > 0 ? e.AddedItems[0] : null;
            foreach (TabItemViewModel tab in vm.Tabs)
            {
                tab.IsSelected = (tab == tabSelected);
            }
        }
    }

    void TabControl_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Middle)
        {
            if (DataContext is TabsViewModel vm)
            {
                if (e.Source is Control control)
                {
                    TabStrip? tabControl = control.FindLogicalAncestorOfType<TabStrip>();
                    if (tabControl is not null && tabControl == sender)
                    {
                        TabStripItem? tabItem = control.FindLogicalAncestorOfType<TabStripItem>();
                        if (tabItem is not null && tabItem.DataContext is TabItemViewModel vmTabItem)
                        {
                            _ = vm.TabClose(vmTabItem);
                        }
                    }
                }
            }
        }
    }

    void TabMenu_Select_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not TabsViewModel vm)
            return;

        if (e.Source is Control control)
        {
            TabStripItem? tabItem = control.FindLogicalAncestorOfType<TabStripItem>();
            if (tabItem is not null && tabItem.DataContext is TabItemViewModel vmTabItem)
            {
                if (vmTabItem?.Content is IUndertaleResourceViewModel vmResourceView)
                {
                    vm.MainVM.DataExplorer.OnSelectValueInTree?.Invoke(vmResourceView.Resource);
                }
            }
        }
    }

    void TabMenu_Close_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TabsViewModel vm)
        {
            if (e.Source is Control control)
            {
                TabStripItem? tabItem = control.FindLogicalAncestorOfType<TabStripItem>();
                if (tabItem is not null && tabItem.DataContext is TabItemViewModel vmTabItem)
                {
                    _ = vm.TabClose(vmTabItem);
                }
            }
        }
    }

    void TabMenu_CloseAll_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TabsViewModel vm)
        {
            _ = vm.TabCloseAll();
        }
    }
}