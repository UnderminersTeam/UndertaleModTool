using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;
using UndertaleModToolAvalonia.Controls;

namespace UndertaleModToolAvalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        DataContextChanged += (_, __) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.OpenFileDialog = OpenFileDialog;
                vm.SaveFileDialog = SaveFileDialog;
                vm.MessageDialog = MessageDialog;
                vm.LaunchUriAsync = LaunchUriAsync;
                vm.SettingsDialog = SettingsDialog;
            }
        };
    }

    public async Task<IReadOnlyList<IStorageFile>> OpenFileDialog(FilePickerOpenOptions options)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        return await topLevel.StorageProvider.OpenFilePickerAsync(options);
    }

    public async Task<IStorageFile?> SaveFileDialog(FilePickerSaveOptions options)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        return await topLevel.StorageProvider.SaveFilePickerAsync(options);
    }

    public async Task<bool> LaunchUriAsync(Uri uri)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        return await topLevel.Launcher.LaunchUriAsync(uri);
    }

    public async Task<MessageWindow.Result> MessageDialog(string message, string? title = null, bool ok = false, bool yes = false, bool no = false, bool cancel = false)
    {
        Window window = this.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException();
        return await new MessageWindow(message, title, ok, yes, no, cancel).ShowDialog<MessageWindow.Result>(window);
    }

    public async Task SettingsDialog()
    {
        Window window = this.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException();
        await new SettingsWindow()
        {
            DataContext = new SettingsViewModel(),
        }.ShowDialog(window);
    }

    private void FilterTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        DataTreeView.SetFilter(FilterTextBox.Text ?? "");
    }

    private void TabControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            object? tabSelected = e.AddedItems.Count > 0 ? e.AddedItems[0] : null;
            foreach (TabItemViewModel tab in vm.Tabs)
            {
                tab.IsSelected = (tab == tabSelected);
            }
        }
    }

    private void TabControl_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Middle)
        {
            if (DataContext is MainViewModel vm)
            {
                if (e.Source is Control control)
                {
                    TabStrip? tabControl = control.FindLogicalAncestorOfType<TabStrip>();
                    if (tabControl is not null && tabControl == sender)
                    {
                        TabStripItem? tabItem = control.FindLogicalAncestorOfType<TabStripItem>();
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