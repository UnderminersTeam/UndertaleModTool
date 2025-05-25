using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModToolAvalonia.Views;

namespace UndertaleModToolAvalonia.Controls;

public partial class DataTreeView : UserControl
{
    public static readonly StyledProperty<UndertaleData?> DataProperty =
        AvaloniaProperty.Register<DataTreeView, UndertaleData?>(nameof(Data));

    public UndertaleData? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    readonly MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();

    public ObservableCollection<TreeItemViewModel> TreeSource { get; set; } = [];

    public DataTreeView()
    {
        InitializeComponent();

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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DataProperty)
        {
            TreeSource.First(x => Equals(x.Value, "AudioGroups")).Source = Data?.AudioGroups;
            TreeSource.First(x => Equals(x.Value, "Sounds")).Source = Data?.Sounds;
            TreeSource.First(x => Equals(x.Value, "Sprites")).Source = Data?.Sprites;
            TreeSource.First(x => Equals(x.Value, "Backgrounds")).Source = Data?.Backgrounds;
            TreeSource.First(x => Equals(x.Value, "Paths")).Source = Data?.Paths;
            TreeSource.First(x => Equals(x.Value, "Scripts")).Source = Data?.Scripts;
            TreeSource.First(x => Equals(x.Value, "Shaders")).Source = Data?.Shaders;
            TreeSource.First(x => Equals(x.Value, "Fonts")).Source = Data?.Fonts;
            TreeSource.First(x => Equals(x.Value, "Timelines")).Source = Data?.Timelines;
            TreeSource.First(x => Equals(x.Value, "GameObjects")).Source = Data?.GameObjects;
            TreeSource.First(x => Equals(x.Value, "Rooms")).Source = Data?.Rooms;
            TreeSource.First(x => Equals(x.Value, "Extensions")).Source = Data?.Extensions;
            TreeSource.First(x => Equals(x.Value, "TexturePageItems")).Source = Data?.TexturePageItems;
            TreeSource.First(x => Equals(x.Value, "Code")).Source = Data?.Code;
            TreeSource.First(x => Equals(x.Value, "Variables")).Source = Data?.Variables;
            TreeSource.First(x => Equals(x.Value, "Functions")).Source = Data?.Functions;
            TreeSource.First(x => Equals(x.Value, "CodeLocals")).Source = Data?.CodeLocals;
            TreeSource.First(x => Equals(x.Value, "Strings")).Source = Data?.Strings;
            TreeSource.First(x => Equals(x.Value, "EmbeddedTextures")).Source = Data?.EmbeddedTextures;
            TreeSource.First(x => Equals(x.Value, "EmbeddedAudio")).Source = Data?.EmbeddedAudio;
            TreeSource.First(x => Equals(x.Value, "TextureGroupInformation")).Source = Data?.TextureGroupInfo;
            TreeSource.First(x => Equals(x.Value, "EmbeddedImages")).Source = Data?.EmbeddedImages;
            TreeSource.First(x => Equals(x.Value, "ParticleSystems")).Source = Data?.ParticleSystems;
            TreeSource.First(x => Equals(x.Value, "ParticleSystemEmitters")).Source = Data?.ParticleSystemEmitters;

            TreeSource[0].UpdateSource();
        }
    }

    public void ListBox_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Control control)
        {
            ListBoxItem? listBoxItem = control.FindLogicalAncestorOfType<ListBoxItem>();
            if (listBoxItem is not null)
            {
                mainVM.TabOpen(listBoxItem.DataContext);
            }
        }
    }

    public void ListMenu_Add_Click(object? sender, RoutedEventArgs e)
    {
        if (mainVM.Data is not null)
        {
            if (e.Source is Control control)
            {
                ListBoxItem? listBoxItem = control.FindLogicalAncestorOfType<ListBoxItem>();
                if (listBoxItem is not null && listBoxItem.DataContext is TreeItemViewModel treeItem)
                {
                    // This could probably be better
                    IList list = (treeItem.Value switch
                    {
                        "AudioGroups" => mainVM.Data.AudioGroups as IList,
                        "Sounds" => mainVM.Data.Sounds as IList,
                        "Sprites" => mainVM.Data.Sprites as IList,
                        "Backgrounds" => mainVM.Data.Backgrounds as IList,
                        "Paths" => mainVM.Data.Paths as IList,
                        "Scripts" => mainVM.Data.Scripts as IList,
                        "Shaders" => mainVM.Data.Shaders as IList,
                        "Fonts" => mainVM.Data.Fonts as IList,
                        "Timelines" => mainVM.Data.Timelines as IList,
                        "GameObjects" => mainVM.Data.GameObjects as IList,
                        "Rooms" => mainVM.Data.Rooms as IList,
                        "Extensions" => mainVM.Data.Extensions as IList,
                        "TexturePageItems" => mainVM.Data.TexturePageItems as IList,
                        "Code" => mainVM.Data.Code as IList,
                        "Variables" => mainVM.Data.Variables as IList,
                        "Functions" => mainVM.Data.Functions as IList,
                        "CodeLocals" => mainVM.Data.CodeLocals as IList,
                        "Strings" => mainVM.Data.Strings as IList,
                        "EmbeddedTextures" => mainVM.Data.EmbeddedTextures as IList,
                        "EmbeddedAudio" => mainVM.Data.EmbeddedAudio as IList,
                        "TextureGroupInformation" => mainVM.Data.TextureGroupInfo as IList,
                        "EmbeddedImages" => mainVM.Data.EmbeddedImages as IList,
                        "ParticleSystems" => mainVM.Data.ParticleSystems as IList,
                        "ParticleSystemEmitters" => mainVM.Data.ParticleSystemEmitters as IList,
                        _ => null,
                    })!;

                    mainVM.DataItemAdd(list);
                }
            }
        }
    }

    private void ExpandCollapseButton_Click(object? sender, RoutedEventArgs e)
    {
        // BUG: This selects the item if you expand or collapse. It's to 'fix' a bug where if you click the button
        // while something else below in the list is selected, not only will it select that item (no,
        // AutoScrollToSelectedItem="False" doesn't seem to work), it'll also slowly scroll down to that item, while
        // locking up the GUI. No idea why this happens.
        if (e.Source is Control control)
        {
            ListBoxItem? listBoxItem = control.FindLogicalAncestorOfType<ListBoxItem>();
            if (listBoxItem is not null)
            {
                listBoxItem.IsSelected = true;
            }
        }
    }
}