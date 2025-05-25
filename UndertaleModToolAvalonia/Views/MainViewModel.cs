using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

    // Data
    [Notify]
    private UndertaleData? _Data;

    [Notify]
    private (uint Major, uint Minor, uint Release, uint Build) _Version;

    // List
    public ObservableCollection<TreeItemViewModel> TreeSource { get; set; } = [];

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

        TreeSource.Add(new TreeItemViewModel(TreeSource, value: "Data", header: "Data", source: new ObservableCollection<TreeItemViewModel>()
        {
            new(TreeSource, value: "GeneralInfo", header: "General info"),
            new(TreeSource, value: "GlobalInitScripts", header: "Global init scripts"),
            new(TreeSource, value: "GameEndScripts", header: "Game End scripts"),
            new(TreeSource, tag: "list", value: "AudioGroups", header: "Audio groups"),
            new(TreeSource, tag: "list", value: "Sounds", header: "Sounds"),
            new(TreeSource, tag: "list", value: "Sprites", header: "Sprites"),
            new(TreeSource, tag: "list", value: "Backgrounds", header: "Backgrounds & Tile sets"),
            new(TreeSource, tag: "list", value: "Paths", header: "Paths"),
            new(TreeSource, tag: "list", value: "Scripts", header: "Scripts"),
            new(TreeSource, tag: "list", value: "Shaders", header: "Shaders"),
            new(TreeSource, tag: "list", value: "Fonts", header: "Fonts"),
            new(TreeSource, tag: "list", value: "Timelines", header: "Timelines"),
            new(TreeSource, tag: "list", value: "GameObjects", header: "Game objects"),
            new(TreeSource, tag: "list", value: "Rooms", header: "Rooms"),
            new(TreeSource, tag: "list", value: "Extensions", header: "Extensions"),
            new(TreeSource, tag: "list", value: "TexturePageItems", header: "Texture page items"),
            new(TreeSource, tag: "list", value: "Code", header: "Code"),
            new(TreeSource, tag: "list", value: "Variables", header: "Variables"),
            new(TreeSource, tag: "list", value: "Functions", header: "Functions"),
            new(TreeSource, tag: "list", value: "CodeLocals", header: "Code locals"),
            new(TreeSource, tag: "list", value: "Strings", header: "Strings"),
            new(TreeSource, tag: "list", value: "EmbeddedTextures", header: "Embedded textures"),
            new(TreeSource, tag: "list", value: "EmbeddedAudio", header: "Embedded audio"),
            new(TreeSource, tag: "list", value: "TextureGroupInformation", header: "Texture group information"),
            new(TreeSource, tag: "list", value: "EmbeddedImages", header: "Embedded images"),
            new(TreeSource, tag: "list", value: "ParticleSystems", header: "Particle systems"),
            new(TreeSource, tag: "list", value: "ParticleSystemEmitters", header: "Particle system emitters"),
        }));

        TreeSource[0].UpdateSource();
        TreeSource[0].ExpandCollapse();
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

            TreeSource.First(x => Equals(x.Value, "AudioGroups")).Source = Data.AudioGroups;
            TreeSource.First(x => Equals(x.Value, "Sounds")).Source = Data.Sounds;
            TreeSource.First(x => Equals(x.Value, "Sprites")).Source = Data.Sprites;
            TreeSource.First(x => Equals(x.Value, "Backgrounds")).Source = Data.Backgrounds;
            TreeSource.First(x => Equals(x.Value, "Paths")).Source = Data.Paths;
            TreeSource.First(x => Equals(x.Value, "Scripts")).Source = Data.Scripts;
            TreeSource.First(x => Equals(x.Value, "Shaders")).Source = Data.Shaders;
            TreeSource.First(x => Equals(x.Value, "Fonts")).Source = Data.Fonts;
            TreeSource.First(x => Equals(x.Value, "Timelines")).Source = Data.Timelines;
            TreeSource.First(x => Equals(x.Value, "GameObjects")).Source = Data.GameObjects;
            TreeSource.First(x => Equals(x.Value, "Rooms")).Source = Data.Rooms;
            TreeSource.First(x => Equals(x.Value, "Extensions")).Source = Data.Extensions;
            TreeSource.First(x => Equals(x.Value, "TexturePageItems")).Source = Data.TexturePageItems;
            TreeSource.First(x => Equals(x.Value, "Code")).Source = Data.Code;
            TreeSource.First(x => Equals(x.Value, "Variables")).Source = Data.Variables;
            TreeSource.First(x => Equals(x.Value, "Functions")).Source = Data.Functions;
            TreeSource.First(x => Equals(x.Value, "CodeLocals")).Source = Data.CodeLocals;
            TreeSource.First(x => Equals(x.Value, "Strings")).Source = Data.Strings;
            TreeSource.First(x => Equals(x.Value, "EmbeddedTextures")).Source = Data.EmbeddedTextures;
            TreeSource.First(x => Equals(x.Value, "EmbeddedAudio")).Source = Data.EmbeddedAudio;
            TreeSource.First(x => Equals(x.Value, "TextureGroupInformation")).Source = Data.TextureGroupInfo;
            TreeSource.First(x => Equals(x.Value, "EmbeddedImages")).Source = Data.EmbeddedImages;
            TreeSource.First(x => Equals(x.Value, "ParticleSystems")).Source = Data.ParticleSystems;
            TreeSource.First(x => Equals(x.Value, "ParticleSystemEmitters")).Source = Data.ParticleSystemEmitters;

            TreeSource[0].UpdateSource();
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

            //TreeViewItem { Name: "GeneralInfo" } => new GeneralInfoViewModel(Data),
            //TreeViewItem { Name: "GlobalInitScripts" } => new GlobalInitScriptsViewModel((Data.GlobalInitScripts as ObservableCollection<UndertaleGlobalInit>)!),
            //TreeViewItem { Name: "GameEndScripts" } => new GameEndScriptsViewModel((Data.GameEndScripts as ObservableCollection<UndertaleGlobalInit>)!),

            UndertaleSprite r => new UndertaleSpriteViewModel(r),
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