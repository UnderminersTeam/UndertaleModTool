using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class DataExplorerViewModel : ObservableObject
{
    public MainViewModel MainVM;

    [ObservableProperty]
    public partial ObservableCollection<Item> TreeDataGridData { get; set; } = [];

    public delegate void OnExpandItemOnTreeDelegate(DataExplorerViewModel.Item item);
    public OnExpandItemOnTreeDelegate? OnExpandItemOnTree;

    public delegate void OnSelectValueInTreeDelegate(object value);
    public OnSelectValueInTreeDelegate? OnSelectValueInTree;

    readonly List<ObservableCollectionView> observableCollectionViewList = [];

    public DataExplorerViewModel(MainViewModel mainVM)
    {
        MainVM = mainVM;
    }

    public void UpdateFromData()
    {
        TreeDataGridData.Clear();

        observableCollectionViewList.Clear();

        if (MainVM.Data is null)
            return;

        Item dataItem = new()
        {
            Value = MainVM.Data,
            Text = "Data",
            Children = [],
        };

        void AddItem(object? item, string value, string text)
        {
            if (item is not null)
                dataItem.Children.Add(new() { Value = value, Text = text });
        }

        void AddList<T>(IList<T?>? list, string value, string text) where T : class?
        {
            if (list is not null)
                dataItem.Children.Add(new() { Tag = "list", Value = value, Text = text, Children = CreateListObservableCollectionView(list) });
        }

        AddItem(MainVM.Data.GeneralInfo, "GeneralInfo", "General info");
        AddItem(MainVM.Data.GlobalInitScripts, "GlobalInitScripts", "Global init scripts");
        AddItem(MainVM.Data.GameEndScripts, "GameEndScripts", "Game End scripts");

        AddList(MainVM.Data.AudioGroups, "AudioGroups", "Audio groups");
        AddList(MainVM.Data.Sounds, "Sounds", "Sounds");
        AddList(MainVM.Data.Sprites, "Sprites", "Sprites");
        AddList(MainVM.Data.Backgrounds, "Backgrounds", "Backgrounds & Tile sets");
        AddList(MainVM.Data.Paths, "Paths", "Paths");
        AddList(MainVM.Data.Scripts, "Scripts", "Scripts");
        AddList(MainVM.Data.Shaders, "Shaders", "Shaders");
        AddList(MainVM.Data.Fonts, "Fonts", "Fonts");
        AddList(MainVM.Data.Timelines, "Timelines", "Timelines");
        AddList(MainVM.Data.GameObjects, "GameObjects", "Game objects");
        AddList(MainVM.Data.Rooms, "Rooms", "Rooms");
        AddList(MainVM.Data.Extensions, "Extensions", "Extensions");
        AddList(MainVM.Data.TexturePageItems, "TexturePageItems", "Texture page items");
        AddList(MainVM.Data.Code, "Code", "Code");
        AddList(MainVM.Data.Variables, "Variables", "Variables");
        AddList(MainVM.Data.Functions, "Functions", "Functions");
        AddList(MainVM.Data.CodeLocals, "CodeLocals", "Code locals");
        AddList(MainVM.Data.Strings, "Strings", "Strings");
        AddList(MainVM.Data.EmbeddedTextures, "EmbeddedTextures", "Embedded textures");
        AddList(MainVM.Data.EmbeddedAudio, "EmbeddedAudio", "Embedded audio");
        AddList(MainVM.Data.TextureGroupInfo, "TextureGroupInformation", "Texture group information");
        AddList(MainVM.Data.EmbeddedImages, "EmbeddedImages", "Embedded images");
        AddList(MainVM.Data.AnimationCurves, "AnimationCurves", "Animation curves");
        AddList(MainVM.Data.ParticleSystems, "ParticleSystems", "Particle systems");
        AddList(MainVM.Data.ParticleSystemEmitters, "ParticleSystemEmitters", "Particle system emitters");

        TreeDataGridData.Add(dataItem);
    }

    ObservableCollectionView<T?, Item>.CustomObservableCollection<Item>? CreateListObservableCollectionView<T>(IList<T?>? list) where T : class?
    {
        if (list is not null)
        {
            ObservableCollectionView<T?, Item> view = new(list,
                transform: x => new Item() { Text = "", Value = x },
                filter: item => AssetNameContainsText(item.Value, MainVM.FilterText));

            observableCollectionViewList.Add(view);

            return view.Output;
        }
        return null;
    }

    public void SetFilter()
    {
        foreach (ObservableCollectionView view in observableCollectionViewList)
        {
            view.SetFilter(item => AssetNameContainsText(((Item)item!).Value, MainVM.FilterText));
        }
    }

    public void SetSort()
    {
        Comparison<object?>? comparison = null;
        if (MainVM.IsSorted)
        {
            comparison = static (a, b) =>
            {
                string? aName = AssetGetName(((Item)a!).Value);
                string? bName = AssetGetName(((Item)b!).Value);

                if (aName is null && bName is null) return 0;
                if (aName is null) return 1;
                if (bName is null) return -1;

                return aName.CompareTo(bName, StringComparison.Ordinal);
            };
        }

        foreach (ObservableCollectionView view in observableCollectionViewList)
        {
            view.SetSort(comparison);
        }
    }

    static bool AssetNameContainsText(object? asset, string text)
    {
        if (text == "")
            return true;

        string? name = AssetGetName(asset);

        if (name is null)
            return false;

        return name.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    static string? AssetGetName(object? asset)
    {
        return asset switch
        {
            UndertaleNamedResource namedResource => namedResource.Name?.Content,
            UndertaleString _string => _string.Content,
            _ => null,
        };
    }

    public partial class Item : ObservableObject
    {
        [ObservableProperty]
        public partial string Text { get; set; } = "<unset text!>";
        public object? Value { get; set; }
        public object? Tag { get; set; }

        [ObservableProperty]
        public partial IList<Item>? Children { get; set; }
    }
}
