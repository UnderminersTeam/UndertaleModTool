using System.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class MainView : UserControl, IView
{
    public MainView()
    {
        InitializeComponent();

        DataContextChanged += (_, __) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.View = this;

                MainTreeDataGrid.Source = new HierarchicalTreeDataGridSource<MainViewModel.TreeDataGridItem>(vm.TreeDataGridData)
                {
                    Columns = {
                        new HierarchicalExpanderColumn<MainViewModel.TreeDataGridItem>(
                            new TemplateColumn<MainViewModel.TreeDataGridItem>(null,
                                new FuncDataTemplate<MainViewModel.TreeDataGridItem>((value, namescope) =>
                                {
                                    if (value is null)
                                        return null;

                                    TextBlock textBlock = new() { Text = value.Text };

                                    if (value.Value is UndertaleNamedResource namedResource)
                                    {
                                        textBlock[!TextBlock.TextProperty] = new Binding("Value.Name.Content");
                                    }
                                    else if (value.Value is UndertaleString _string)
                                    {
                                        textBlock[!TextBlock.TextProperty] = new Binding("Value.Content");
                                    }
                                    //else if (value.Value is UndertaleData data)
                                    //{
                                    //    textBlock[!TextBlock.TextProperty] = new Binding("Value.GeneralInfo");
                                    //}

                                    return textBlock;
                                }), width: GridLength.Star
                            ),
                            x => x.Children)
                    }
                };
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

    private void FilterTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SetFilterText(FilterTextBox.Text ?? "");
        }
    }

    private MainViewModel.TreeDataGridItem? GetItemFromTreeDataGridControl(object? source)
    {
        if (DataContext is MainViewModel vm)
        {
            if (source is Control control)
            {
                TreeDataGridRow? row = control.FindLogicalAncestorOfType<TreeDataGridRow>(includeSelf: true);
                if (row?.DataContext is MainViewModel.TreeDataGridItem item)
                {
                    return item;
                }
            }
        }
        return null;
    }

    private void OpenItemFromTreeDataGridControl(object? source)
    {
        if (DataContext is MainViewModel vm)
        {
            if (source is Control control)
            {
                TreeDataGridRow? row = control.FindLogicalAncestorOfType<TreeDataGridRow>(includeSelf: true);
                if (row?.DataContext is MainViewModel.TreeDataGridItem item)
                {
                    if (row.Rows?[row.RowIndex] is HierarchicalRow<MainViewModel.TreeDataGridItem> hierarchicalRow)
                    {
                        hierarchicalRow.IsExpanded = !hierarchicalRow.IsExpanded;
                    }
                    vm.TabOpen(item.Value);
                }
            }
        }
    }

    private void TreeDataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        OpenItemFromTreeDataGridControl(e.Source);
    }

    private void MainTreeDataGrid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.PhysicalKey == PhysicalKey.Enter)
        {
            OpenItemFromTreeDataGridControl(e.Source);
        }
    }

    public void ContextMenu_Add_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            MainViewModel.TreeDataGridItem? item = GetItemFromTreeDataGridControl(e.Source);
            if (item is not null && vm.Data is not null)
            {
                // This could probably be better
                IList list = (item.Value switch
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

    public void ContextMenu_Open_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            MainViewModel.TreeDataGridItem? item = GetItemFromTreeDataGridControl(e.Source);
            if (item is not null && vm.Data is not null)
            {
                vm.TabOpen(item.Value);
            }
        }
    }

    public void ContextMenu_OpenInNewTab_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            MainViewModel.TreeDataGridItem? item = GetItemFromTreeDataGridControl(e.Source);
            if (item is not null && vm.Data is not null)
            {
                vm.TabOpen(item.Value, inNewTab: true);
            }
        }
    }

    public async void ContextMenu_CopyName_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            MainViewModel.TreeDataGridItem? item = GetItemFromTreeDataGridControl(e.Source);
            if (item is not null && vm.Data is not null)
            {
                string? name = item.Value switch
                {
                    UndertaleNamedResource namedResource => namedResource.Name.Content,
                    UndertaleString _string => _string.Content,
                    _ => null,
                };

                if (name is not null)
                {
                    TopLevel topLevel = TopLevel.GetTopLevel(this)!;
                    await topLevel.Clipboard!.SetTextAsync(name);
                }
            }
        }
    }

    public async void ContextMenu_Move_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            MainViewModel.TreeDataGridItem? item = GetItemFromTreeDataGridControl(e.Source);
            if (item is not null && vm.Data is not null && vm.View is not null)
            {
                UndertaleResource resource = (item.Value as UndertaleResource)!;
                IList list = vm.Data[resource.GetType()];
                int oldIndex = list.IndexOf(resource);

                string? input = await vm.View.TextBoxDialog("Swap to position:", oldIndex.ToString());
                if (input is null)
                    return;

                if (!int.TryParse(input, out int newIndex))
                {
                    await vm.View.MessageDialog($"\"{input}\" is not a integer");
                    return;
                }
                if (newIndex < 0 || newIndex >= list.Count)
                {
                    await vm.View.MessageDialog($"{newIndex} is out of range of the list");
                    return;
                }

                object? temp = list[newIndex];
                list[newIndex] = list[oldIndex];
                list[oldIndex] = temp;
            }
        }
    }

    public async void ContextMenu_Remove_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            MainViewModel.TreeDataGridItem? item = GetItemFromTreeDataGridControl(e.Source);
            if (item is not null && vm.Data is not null)
            {
                // TODO: Maybe do something about all references to this.
                UndertaleResource resource = (item.Value as UndertaleResource)!;

                if (await vm.ShowMessageDialog($"Delete {resource}?\nNote that the code often references objects by ID, " +
                    $"so this operation is likely to break stuff because other items will shift up!",
                    ok: false, yes: true, no: true) == MessageWindow.Result.Yes)
                {
                    vm.Data[resource.GetType()].Remove(resource);

                    // TODO: Close tabs, remove histories
                }
            }
        }
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