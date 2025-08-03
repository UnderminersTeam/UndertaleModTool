﻿using System;
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
                vm.OpenFolderDialog = OpenFolderDialog;
                vm.LaunchUriAsync = LaunchUriAsync;
                vm.MessageDialog = MessageDialog;
                vm.TextBoxDialog = TextBoxDialog;
                vm.LoaderOpen = LoaderOpen;
                vm.SettingsDialog = SettingsDialog;
                vm.SearchInCodeOpen = SearchInCodeOpen;
            }
        };

        Loaded += (_, __) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.OnLoaded();
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

    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderDialog(FolderPickerOpenOptions options)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        return await topLevel.StorageProvider.OpenFolderPickerAsync(options);
    }

    public async Task<bool> LaunchUriAsync(Uri uri)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        return await topLevel.Launcher.LaunchUriAsync(uri);
    }

    public async Task<MessageWindow.Result> MessageDialog(string message, string? title = null, bool ok = true, bool yes = false, bool no = false, bool cancel = false)
    {
        Window window = this.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException();
        return await new MessageWindow(message, title, ok, yes, no, cancel).ShowDialog<MessageWindow.Result>(window);
    }

    public async Task<string?> TextBoxDialog(string message, string text = "", string? title = null, bool isMultiline = false, bool isReadOnly = false)
    {
        Window window = this.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException();
        return await new TextBoxWindow(message, text, title, isMultiline, isReadOnly).ShowDialog<string?>(window);
    }

    public LoaderWindow LoaderOpen()
    {
        // TODO: Replace LoaderWindow with an interface
        Window window = this.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException();
        LoaderWindow loaderWindow = new();
        loaderWindow.ShowDelayed(window);
        return loaderWindow;
    }

    public async Task SettingsDialog()
    {
        Window window = this.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException();
        await new SettingsWindow()
        {
            DataContext = new SettingsViewModel(),
        }.ShowDialog(window);
    }

    public static void SearchInCodeOpen()
    {
        new SearchInCodeWindow()
        {
            DataContext = new SearchInCodeViewModel(),
        }.Show();
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

    private void TabMenu_Close_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            if (e.Source is Control control)
            {
                TabStripItem? tabItem = control.FindLogicalAncestorOfType<TabStripItem>();
                if (tabItem is not null && tabItem.DataContext is TabItemViewModel vmTabItem)
                {
                    vm.TabClose(vmTabItem);
                }
            }
        }
    }

    private async void CommandTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            if (e.Key == Key.Enter)
            {
                object? result = await vm.Scripting.RunScript(vm.CommandTextBoxText);
                vm.CommandTextBoxText = result?.ToString() ?? "";
            }
    }
}