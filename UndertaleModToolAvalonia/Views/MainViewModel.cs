using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Controls;

namespace UndertaleModToolAvalonia.Views;

public partial class MainViewModel
{
    // Set this when testing.
    public Func<FilePickerOpenOptions, Task<IReadOnlyList<IStorageFile>>>? OpenFileDialog;
    public Func<FilePickerSaveOptions, Task<IStorageFile?>>? SaveFileDialog;
    public Func<Uri, Task<bool>>? LaunchUriAsync;


    public delegate Task<MessageWindow.Result> MessageDialogDelegate(string message, string? title = null, bool ok = false, bool yes = false, bool no = false, bool cancel = false);
    public MessageDialogDelegate? MessageDialog;

    // Window
    public string Title => $"UndertaleModToolAvalonia - v0.0.0.0" +
        $"{(Data?.GeneralInfo is not null ? " - " + Data?.GeneralInfo.ToString() : "")}" +
        $"{(DataPath is not null ? " [" + DataPath + "]" : "")}";

    // Data
    [Notify]
    private UndertaleData? _Data;
    [Notify]
    private string? _DataPath;

    [Notify]
    private (uint Major, uint Minor, uint Release, uint Build) _Version;

    IReadOnlyList<FilePickerFileType> dataFileTypes =
    [
        new FilePickerFileType("GameMaker data files (.win, .unx, .ios, .droid, audiogroup*.dat)")
        {
            Patterns = ["*.win", "*.unx", "*.ios", "*.droid", "audiogroup*.dat"],
        },
        new FilePickerFileType("All files")
        {
            Patterns = ["*"],
        },
    ];

    // Tabs
    public ObservableCollection<TabItemViewModel> Tabs { get; set; }

    [Notify]
    private TabItemViewModel? _TabSelected;
    [Notify]
    private int _TabSelectedIndex;
    [Notify]
    private string _TabSelectedResourceIdString = "None";

    // Image cache
    public ImageCache ImageCache = new();

    public MainViewModel()
    {
        //Data = UndertaleData.CreateNew();

        Tabs = [
            new TabItemViewModel(new DescriptionViewModel(
                "Welcome to UndertaleModTool!",
                "Open a data.win file to get started, then double click on the items on the left to view them."),
                isSelected: true),
        ];
    }

    public async Task<MessageWindow.Result> ShowMessageDialog(string message, string? title = null, bool ok = false, bool yes = false, bool no = false, bool cancel = false)
    {
        if (MessageDialog is not null)
            return await MessageDialog.Invoke(message, title, ok, yes, no, cancel);
        return MessageWindow.Result.None;
    }

    public void SetData(UndertaleData? data)
    {
        if (Data is not null && Data.GeneralInfo is not null)
        {
            Data.GeneralInfo.PropertyChanged -= DataGeneralInfoChangedHandler;
        }

        Data = data;

        if (Data is not null && Data.GeneralInfo is not null)
        {
            Data.GeneralInfo.PropertyChanged += DataGeneralInfoChangedHandler;
        }

        UpdateVersion();
    }

    public void UpdateVersion()
    {
        Version = Data is not null && Data.GeneralInfo is not null ? (Data.GeneralInfo.Major, Data.GeneralInfo.Minor, Data.GeneralInfo.Release, Data.GeneralInfo.Build) : default;
    }

    private void DataGeneralInfoChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
        if (Data is not null && e.PropertyName is
            nameof(UndertaleGeneralInfo.Major) or nameof(UndertaleGeneralInfo.Minor) or
            nameof(UndertaleGeneralInfo.Release) or nameof(UndertaleGeneralInfo.Build))
        {
            UpdateVersion();
        }
    }

    // Menus
    public void FileNew()
    {
        Tabs.Clear();

        SetData(UndertaleData.CreateNew());
        DataPath = null;
    }

    public async void FileOpen()
    {
        var files = await OpenFileDialog!(new FilePickerOpenOptions()
        {
            Title = "Open data file",
            AllowMultiple = false,
            FileTypeFilter = dataFileTypes,
        });

        if (files.Count != 1)
            return;

        Tabs.Clear();

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

        DataPath = files[0].TryGetLocalPath();
    }

    public async void FileSave()
    {
        if (Data is null)
            return;

        IStorageFile? file = await SaveFileDialog!(new FilePickerSaveOptions()
        {
            Title = "Save data file",
            FileTypeChoices = dataFileTypes,
            DefaultExtension = ".win",
        });

        if (file is null)
            return;

        using Stream stream = await file.OpenWriteAsync();

        UndertaleIO.Write(stream, Data, message =>
        {
            Debug.WriteLine($"Data.Write message: {message}");
        });
    }

    public void FileClose()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    public void HelpGitHub()
    {
        LaunchUriAsync?.Invoke(new Uri("https://github.com/UnderminersTeam/UndertaleModTool"));
    }

    public async void HelpAbout()
    {
        await ShowMessageDialog("UndertaleModTool by the Underminers team\nLicensed under the GNU General Public License Version 3.",
            title: "About", ok: true);
    }

    public void DataItemAdd(IList list)
    {
        // TODO: Ask user for name etc.

        if (Data is null || list is null)
            return;

        UndertaleResource res = UndertaleData.CreateResource(list);
        Data.InitializeResource(res, list, UndertaleData.GetDefaultResourceName(list));

        if (res is UndertaleRoom room)
        {
            Data.GeneralInfo?.RoomOrder.Add(new(room));
        }

        list.Add(res);
    }
    
    public void TabOpen(object? item)
    {
        if (Data is null)
            return;

        if (item is TreeItemViewModel treeItem)
        {
            item = treeItem.Value;
        }

        object? content = item switch
        {
            DescriptionViewModel vm => vm,
            "GeneralInfo" => new GeneralInfoViewModel(Data),
            "GlobalInitScripts" => new GlobalInitScriptsViewModel((Data.GlobalInitScripts as ObservableCollection<UndertaleGlobalInit>)!),
            "GameEndScripts" => new GameEndScriptsViewModel((Data.GameEndScripts as ObservableCollection<UndertaleGlobalInit>)!),
            UndertaleAudioGroup r => new UndertaleAudioGroupViewModel(r),
            UndertaleSound r => new UndertaleSoundViewModel(r),
            UndertaleSprite r => new UndertaleSpriteViewModel(r),
            UndertaleBackground r => new UndertaleBackgroundViewModel(r),
            UndertalePath r => new UndertalePathViewModel(r),
            UndertaleScript r => new UndertaleScriptViewModel(r),
            UndertaleShader r => new UndertaleShaderViewModel(r),
            UndertaleFont r => new UndertaleFontViewModel(r),
            UndertaleTimeline r => new UndertaleTimelineViewModel(r),
            UndertaleGameObject r => new UndertaleGameObjectViewModel(r),
            UndertaleRoom r => new UndertaleRoomViewModel(r),
            UndertaleTexturePageItem r => new UndertaleTexturePageItemViewModel(r),
            UndertaleCode r => new UndertaleCodeViewModel(r),
            UndertaleVariable r => new UndertaleVariableViewModel(r),
            UndertaleFunction r => new UndertaleFunctionViewModel(r),
            UndertaleCodeLocals r => new UndertaleCodeLocalsViewModel(r),
            UndertaleString r => new UndertaleStringViewModel(r),
            UndertaleEmbeddedTexture r => new UndertaleEmbeddedTextureViewModel(r),
            UndertaleEmbeddedAudio r => new UndertaleEmbeddedAudioViewModel(r),
            UndertaleTextureGroupInfo r => new UndertaleTextureGroupInfoViewModel(r),
            UndertaleEmbeddedImage r => new UndertaleEmbeddedImageViewModel(r),
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
        var selected = TabSelected;
        var index = TabSelectedIndex;

        Tabs.Remove(tab);

        if (TabSelected != selected)
        {
            if (index >= Tabs.Count)
                index = Tabs.Count - 1;

            TabSelectedIndex = index;
        }
    }

    private void OnTabSelectedChanged()
    {
        if (Data is not null && TabSelected?.Content is IUndertaleResourceViewModel vm)
        {
            TabSelectedResourceIdString = Data.IndexOf(vm.Resource).ToString();
        }
        else
        {
            TabSelectedResourceIdString = "None";
        }
    }
}