using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Controls;
using UndertaleModToolAvalonia.Core;

namespace UndertaleModToolAvalonia.Views;

public partial class MainViewModel
{
    // Set this when testing.
    public Func<FilePickerOpenOptions, Task<IReadOnlyList<IStorageFile>>>? OpenFileDialog;
    public Func<FilePickerSaveOptions, Task<IStorageFile?>>? SaveFileDialog;
    public Func<FolderPickerOpenOptions, Task<IReadOnlyList<IStorageFolder>>> OpenFolderDialog;
    public Func<Uri, Task<bool>>? LaunchUriAsync;

    public delegate Task<MessageWindow.Result> MessageDialogDelegate(string message, string? title = null, bool ok = true, bool yes = false, bool no = false, bool cancel = false);
    public MessageDialogDelegate? MessageDialog;

    public Func<Task>? SettingsDialog;
    public Action? SearchInCodeOpen;

    // Services
    public readonly IServiceProvider ServiceProvider;

    // Settings
    public SettingsFile? Settings { get; set; }

    // Scripting
    public Scripting Scripting = null!;

    // Window
    public string Title => $"UndertaleModToolAvalonia - v" +
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?.?.?.?" +
        $"{(Data?.GeneralInfo is not null ? " - " + Data.GeneralInfo.ToString() : "")}" +
        $"{(DataPath is not null ? " [" + DataPath + "]" : "")}";

    // Data
    [Notify]
    private UndertaleData? _Data;
    [Notify]
    private string? _DataPath;
    [Notify]
    private (uint Major, uint Minor, uint Release, uint Build) _DataVersion;

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

    // Command text box
    [Notify]
    private string _CommandTextBoxText = "";

    // Image cache
    public ImageCache ImageCache = new();

    public MainViewModel(IServiceProvider? serviceProvider = null)
    {
        ServiceProvider = serviceProvider ?? App.Services;

        Tabs = [
            new TabItemViewModel(new DescriptionViewModel(
                "Welcome to UndertaleModTool!",
                "Open a data.win file to get started, then double click on the items on the left to view them."),
                isSelected: true),
        ];
    }

    public async void OnLoaded()
    {
        Settings = await SettingsFile.Load(ServiceProvider);
        Scripting = new(ServiceProvider);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.Args?.Length >= 1)
            {
                try
                {
                    using FileStream stream = File.OpenRead(desktop.Args[0]);
                    if (await LoadData(stream))
                    {
                        DataPath = stream.Name;
                    }
                }
                catch (SystemException e)
                {
                    await ShowMessageDialog($"Error opening data file from argument: {e.Message}");
                }
            }
        }
    }

    public async Task<MessageWindow.Result> ShowMessageDialog(string message, string? title = null, bool ok = true, bool yes = false, bool no = false, bool cancel = false)
    {
        if (MessageDialog is not null)
            return await MessageDialog(message, title, ok, yes, no, cancel);
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

    public async Task<bool> LoadData(Stream stream)
    {
        try
        {
            UndertaleData data = UndertaleIO.Read(stream,
                (string warning, bool isImportant) =>
                {
                    Debug.WriteLine($"Data.Read warning: {(isImportant ? "(important) " : "")}{warning}");
                },
                (string message) =>
                {
                    Debug.WriteLine($"Data.Read message: {message}");
                });

            SetData(data);

            return true;
        }
        catch (Exception e)
        {
            await ShowMessageDialog($"Error opening data file: {e.Message}");

            return false;
        }
    }

    public void CloseData()
    {
        SetData(null);
        DataPath = null;

        Tabs.Clear();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows.ToList())
            {
                if (window is SearchInCodeWindow)
                {
                    window.Close();
                }
            }
        }
    }

    public void UpdateVersion()
    {
        DataVersion = Data is not null && Data.GeneralInfo is not null ? (Data.GeneralInfo.Major, Data.GeneralInfo.Minor, Data.GeneralInfo.Release, Data.GeneralInfo.Build) : default;
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
        CloseData();

        SetData(UndertaleData.CreateNew());
        DataPath = null;
    }

    public async void FileOpen()
    {
        var files = await OpenFileDialog!(new FilePickerOpenOptions()
        {
            Title = "Open data file",
            FileTypeFilter = dataFileTypes,
        });

        if (files.Count != 1)
            return;

        CloseData();

        using Stream stream = await files[0].OpenReadAsync();

        if (await LoadData(stream))
        {
            DataPath = files[0].TryGetLocalPath();
        }
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

    public async void FileSettings()
    {
        await SettingsDialog!();
    }

    public void FileClose()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    public void ToolsSearchInCode()
    {
        SearchInCodeOpen!();
    }

    public async void ScriptsRunOtherScript()
    {
        var files = await OpenFileDialog!(new FilePickerOpenOptions()
        {
            Title = "Run script",
            FileTypeFilter = [
                new FilePickerFileType("C# scripts (.csx)")
                {
                    Patterns = ["*.csx"],
                },
                new FilePickerFileType("All files")
                {
                    Patterns = ["*"],
                },
            ],
        });

        if (files.Count != 1)
            return;

        string text;
        using (Stream stream = await files[0].OpenReadAsync())
        {
            using StreamReader streamReader = new(stream);
            text = streamReader.ReadToEnd();
        }

        string? filePath = files[0].TryGetLocalPath();
        await Scripting.RunScript(text, filePath);

        CommandTextBoxText = $"{Path.GetFileName(filePath) ?? "Script"} finished!";
    }

    public void HelpGitHub()
    {
        LaunchUriAsync?.Invoke(new Uri("https://github.com/UnderminersTeam/UndertaleModTool"));
    }

    public async void HelpAbout()
    {
        await ShowMessageDialog($"UndertaleModTool v{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?.?.?.?"}" +
            $"by the Underminers team\nLicensed under the GNU General Public License Version 3.", title: "About");
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

    public TabItemViewModel? TabOpen(object? item)
    {
        if (Data is null)
            return null;

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
            UndertaleExtension r => new UndertaleExtensionViewModel(r),
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
            UndertaleParticleSystem r => new UndertaleParticleSystemViewModel(r),
            UndertaleParticleSystemEmitter r => new UndertaleParticleSystemEmitterViewModel(r),
            _ => null,
        };

        if (content is not null)
        {
            TabItemViewModel tab = new(content);
            Tabs.Add(tab);
            TabSelected = tab;
            return tab;
        }

        return null;
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