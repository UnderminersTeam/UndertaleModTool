using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public partial class MainViewModel
{
    // Set this when testing.
    public Func<FilePickerOpenOptions, Task<IReadOnlyList<IStorageFile>>>? OpenFileDialog;
    public Func<FilePickerSaveOptions, Task<IStorageFile?>>? SaveFileDialog;

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

    // Tabs
    public ObservableCollection<TabItemViewModel> Tabs { get; set; }

    [Notify]
    private TabItemViewModel? _TabSelected;
    [Notify]
    private int _TabSelectedIndex;

    // Image cache
    public ImageCache ImageCache = new();

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

        UpdateVersion();
    }

    public void UpdateVersion()
    {
        Version = Data is not null ? (Data.GeneralInfo.Major, Data.GeneralInfo.Minor, Data.GeneralInfo.Release, Data.GeneralInfo.Build) : default;
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
        if (OpenFileDialog is null)
            return;

        var files = await OpenFileDialog(new FilePickerOpenOptions
        {
            Title = "Open",
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
    }

    public void DataItemAdd(IList list)
    {
        // TODO: This is terrible and should at least partially be in the lib.
        // TODO: Ask user for name etc.

        if (Data is null)
            return;
        if (list is null)
            return;

        Type type = list.GetType().GetGenericArguments()[0];
        UndertaleResource obj = (Activator.CreateInstance(type) as UndertaleResource)!;

        if (obj is UndertaleNamedResource namedResource)
        {
            string typeName = type.Name.Replace("Undertale", "").Replace("GameObject", "Object").ToLower();
            string resourceName = typeName + list.Count;

            namedResource.Name = obj switch
            {
                // UTMT only names.
                UndertaleTexturePageItem => new UndertaleString("PageItem " + list.Count),
                UndertaleEmbeddedAudio => new UndertaleString("EmbeddedSound " + list.Count),
                UndertaleEmbeddedTexture => new UndertaleString("Texture " + list.Count),
                // Data names.
                UndertaleNamedResource => Data.Strings.MakeString(resourceName, createNew: true),
                _ => null,
            };

            // TODO: Initialize each type. This should be done in the lib.
            InitializeResource(namedResource);
        }
        else if (obj is UndertaleString _string)
        {
            _string.Content = "string" + list.Count;
        }

        list.Add(obj);
    }

    void InitializeResource(UndertaleNamedResource obj)
    {
        if (Data is null)
            return;

        if (obj is UndertaleRoom room)
        {
            if (Data.IsVersionAtLeast(2))
            {
                room.Caption = null;
                room.Backgrounds.Clear();
                if (Data.IsVersionAtLeast(2024, 13))
                {
                    room.Flags |= Data.IsVersionAtLeast(2024, 13) ? UndertaleRoom.RoomEntryFlags.IsGM2024_13 : UndertaleRoom.RoomEntryFlags.IsGMS2;
                }
                else
                {
                    room.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2;
                    if (Data.IsVersionAtLeast(2, 3))
                    {
                        room.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2_3;
                    }
                }
            }
            else
            {
                room.Caption = Data.Strings.MakeString("", createNew: true);
            }

            Data.GeneralInfo.RoomOrder.Add(new(room));
        }
        else if (obj is UndertaleScript script)
        {
            if (Data.IsVersionAtLeast(2, 3))
            {
                script.Code = UndertaleCode.CreateEmptyEntry(Data, Data.Strings.MakeString($"gml_GlobalScript_{script.Name.Content}", createNew: true));
                if (Data.GlobalInitScripts is IList<UndertaleGlobalInit> globalInitScripts)
                {
                    globalInitScripts.Add(new UndertaleGlobalInit()
                    {
                        Code = script.Code,
                    });
                }
            }
            else
            {
                script.Code = UndertaleCode.CreateEmptyEntry(Data, Data.Strings.MakeString($"gml_Script_{script.Name.Content}", createNew: true));
            }
        }
        else if (obj is UndertaleCode code)
        {
            if (Data.CodeLocals is not null)
            {
                code.LocalsCount = 1;
                UndertaleCodeLocals.CreateEmptyEntry(Data, code.Name);
            }
            else
            {
                code.WeirdLocalFlag = true;
            }
        }
        else if (obj is UndertaleExtension)
        {
            // TODO: This should absolutely not be here!
            if (Data.GeneralInfo?.Major >= 2 ||
                (Data.GeneralInfo?.Major == 1 && Data.GeneralInfo?.Build >= 1773) ||
                (Data.GeneralInfo?.Major == 1 && Data.GeneralInfo?.Build == 1539))
            {
                var newProductID = new byte[] { 0xBA, 0x5E, 0xBA, 0x11, 0xBA, 0xDD, 0x06, 0x60, 0xBE, 0xEF, 0xED, 0xBA, 0x0B, 0xAB, 0xBA, 0xBE };
                Data.FORM.EXTN.productIdData.Add(newProductID);
            }
        }
        else if (obj is UndertaleShader shader)
        {
            shader.GLSL_ES_Vertex = Data.Strings.MakeString("", createNew: true);
            shader.GLSL_ES_Fragment = Data.Strings.MakeString("", createNew: true);
            shader.GLSL_Vertex = Data.Strings.MakeString("", createNew: true);
            shader.GLSL_Fragment = Data.Strings.MakeString("", createNew: true);
            shader.HLSL9_Vertex = Data.Strings.MakeString("", createNew: true);
            shader.HLSL9_Fragment = Data.Strings.MakeString("", createNew: true);
        }
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
            UndertaleSound r => new UndertaleSoundViewModel(r),
            UndertaleSprite r => new UndertaleSpriteViewModel(r),
            UndertaleBackground r => new UndertaleBackgroundViewModel(r),
            UndertaleGameObject r => new UndertaleGameObjectViewModel(r),
            UndertaleRoom r => new UndertaleRoomViewModel(r),
            UndertaleTexturePageItem r => new UndertaleTexturePageItemViewModel(r),
            UndertaleCode r => new UndertaleCodeViewModel(r),
            UndertaleString r => new UndertaleStringViewModel(r),
            UndertaleEmbeddedTexture r => new UndertaleEmbeddedTextureViewModel(r),
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
}