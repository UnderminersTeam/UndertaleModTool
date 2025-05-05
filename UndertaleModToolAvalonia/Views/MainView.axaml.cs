using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;

namespace UndertaleModToolAvalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        DataContextChanged += (_, __) =>
        {
            if (DataContext is MainViewModel vm)
                vm.OpenFileDialog = OpenFileDialog;
        };
    }

    public async Task<IReadOnlyList<IStorageFile>> OpenFileDialog(FilePickerOpenOptions options)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        return await topLevel.StorageProvider.OpenFilePickerAsync(options);
    }

    public void TreeView_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            if (e.Source is Control control)
            {
                TreeViewItem? treeViewItem = control.FindLogicalAncestorOfType<TreeViewItem>();
                if (treeViewItem is not null)
                {
                    // For General info, etc.
                    if (treeViewItem.DataContext != vm)
                        vm.TabOpen(treeViewItem.DataContext);
                    else
                        vm.TabOpen(treeViewItem);
                }
            }
        }
    }

    public void TabControl_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Middle)
        {
            if (DataContext is MainViewModel vm)
            {
                if (e.Source is Control control)
                {
                    TabControl? tabControl = control.FindLogicalAncestorOfType<TabControl>();
                    if (tabControl is not null && tabControl == sender)
                    {
                        TabItem? tabItem = control.FindLogicalAncestorOfType<TabItem>();
                        if (tabItem is not null && tabItem.DataContext is TabItemViewModel vmTabItem)
                        {
                            vm.TabClose(vmTabItem);
                        }
                    }
                }
            }
        }
    }
}