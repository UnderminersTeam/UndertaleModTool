using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

    public void ListMenu_Add_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.Data is not null)
        {
            if (sender is MenuItem menuItem)
            {
                TreeViewItem? treeViewItem = menuItem.FindLogicalAncestorOfType<TreeViewItem>();
                if (treeViewItem is not null)
                {
                    // Ideally I'd just get the ItemsSource, but it isn't set. Also ideally, it should be type of the resource.
                    IList list = (treeViewItem.Name switch
                    {
                        "AudioGroups" => vm.Data.AudioGroups as IList,
                        "Sounds" => vm.Data.Sounds as IList,
                        "Sprites" => vm.Data.Sprites as IList,
                        "Backgrounds" => vm.Data.Backgrounds as IList,
                        "Paths" => vm.Data.Paths as IList,
                        "Scripts" => vm.Data.Scripts as IList,
                        "Shaders" => vm.Data.Shaders as IList,
                        "Fonts" => vm.Data.Fonts as IList,
                        "Timelines" => vm.Data.Timelines as IList,
                        "GameObjects" => vm.Data.GameObjects as IList,
                        "Rooms" => vm.Data.Rooms as IList,
                        "Extensions" => vm.Data.Extensions as IList,
                        "TexturePageItems" => vm.Data.TexturePageItems as IList,
                        "Code" => vm.Data.Code as IList,
                        "Variables" => vm.Data.Variables as IList,
                        "Functions" => vm.Data.Functions as IList,
                        "CodeLocals" => vm.Data.CodeLocals as IList,
                        "Strings" => vm.Data.Strings as IList,
                        "EmbeddedTextures" => vm.Data.EmbeddedTextures as IList,
                        "EmbeddedAudio" => vm.Data.EmbeddedAudio as IList,
                        "TextureGroupInformation" => vm.Data.TextureGroupInfo as IList,
                        "EmbeddedImages" => vm.Data.EmbeddedImages as IList,
                        "ParticleSystems" => vm.Data.ParticleSystems as IList,
                        "ParticleSystemEmitters" => vm.Data.ParticleSystemEmitters as IList,
                        _ => null,
                    })!;

                    vm.DataItemAdd(list);
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