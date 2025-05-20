using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public partial class MainViewModel
{
    // Set this when testing.
    public Func<FilePickerOpenOptions, Task<IReadOnlyList<IStorageFile>>>? OpenFileDialog;

    // Data
    [Notify]
    private UndertaleData? _Data;

    [Notify]
    private (uint Major, uint Minor, uint Release, uint Build) _Version;

    // Tabs
    public ObservableCollection<TabItemViewModel> Tabs { get; set; }

    [Notify]
    private TabItemViewModel? _TabSelected;
    [Notify]
    private int _TabSelectedIndex;

    public MainViewModel()
    {
        //Data = UndertaleData.CreateNew();

        Tabs = [
            new TabItemViewModel(new DescriptionViewModel(
                "Welcome to UndertaleModTool!",
                "Open a data.win file to get started, then double click on the items on the left to view them.")),
        ];
    }

    public void SetData(UndertaleData? data)
    {
        if (Data is not null)
        {
            Data.GeneralInfo.PropertyChanged -= DataGeneralInfoChangedHandler;
        }

        Data = data;

        if (Data is not null)
        {
            Data.GeneralInfo.PropertyChanged += DataGeneralInfoChangedHandler;
        }
    }

    private void DataGeneralInfoChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
        if (Data is not null && e.PropertyName is
            nameof(UndertaleGeneralInfo.Major) or nameof(UndertaleGeneralInfo.Minor) or
            nameof(UndertaleGeneralInfo.Release) or nameof(UndertaleGeneralInfo.Build))
        {
            Version = (Data.GeneralInfo.Major, Data.GeneralInfo.Minor, Data.GeneralInfo.Release, Data.GeneralInfo.Build);
        }
    }

    // Menus
    public void FileNew()
    {
        Tabs.Clear();

        SetData(UndertaleData.CreateNew());
    }

    public async void FileOpen()
    {
        if (OpenFileDialog is null)
            return;

        var files = await OpenFileDialog(new FilePickerOpenOptions
        {
            Title = "Testing.",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("GameMaker data files (.win, .unx, .ios, .droid, audiogroup*.dat)")
                {
                    Patterns = ["*.win", "*.unx", "*.ios", "*.droid", "audiogroup*.dat"],
                },
                new FilePickerFileType("All files")
                {
                    Patterns = ["*"],
                },
            ],
        });

        if (files.Count >= 1)
        {
            Debug.WriteLine(files[0].TryGetLocalPath());
            using Stream stream = await files[0].OpenReadAsync();

            SetData(UndertaleIO.Read(stream,
                (string warning, bool isImportant) =>
                {
                    Debug.WriteLine($"Data.Read warning: {(isImportant ? "(important) " : "")}{warning}");
                },
                (string message) =>
                {
                    Debug.WriteLine($"Data.Read message: {message}");
                }));

            Tabs.Clear();
        }
    }

    public void TabOpen(object? item)
    {
        if (Data is null)
            return;

        object? content = item switch
        {
            DescriptionViewModel vm => vm,
            TreeViewItem { Name: "GeneralInfo" } => new GeneralInfoViewModel(Data),
            TreeViewItem { Name: "GlobalInitScripts" } => new GlobalInitScriptsViewModel((Data.GlobalInitScripts as ObservableCollection<UndertaleGlobalInit>)!),
            TreeViewItem { Name: "GameEndScripts" } => new GameEndScriptsViewModel((Data.GameEndScripts as ObservableCollection<UndertaleGlobalInit>)!),
            UndertaleGameObject r => new UndertaleGameObjectViewModel(r),
            UndertaleRoom r => new UndertaleRoomViewModel(r),
            UndertaleCode r => new UndertaleCodeViewModel(r),
            UndertaleString r => new UndertaleStringViewModel(r),
            // temp
            UndertaleResource i => i,
            _ => null,
        };

        if (content is not null)
        {
            TabItemViewModel tab = new(content);
            Tabs.Add(tab);
            TabSelected = tab;
        }
    }

    public void TabClose(TabItemViewModel tab)
    {
        int index = TabSelectedIndex;
        Tabs.Remove(tab);
        if (index >= Tabs.Count)
            index = Tabs.Count - 1;

        TabSelectedIndex = index;
    }
}