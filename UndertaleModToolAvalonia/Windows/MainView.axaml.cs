using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.VisualTree;
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

                                        if (namedResource is UndertaleCode code)
                                        {
                                            // NOTE: Doesn't update, but whatever.
                                            if (code.ParentEntry is not null)
                                            {
                                                textBlock[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("SystemControlForegroundBaseMediumBrush");
                                            }
                                        }
                                    }
                                    else if (value.Value is UndertaleString _string)
                                    {
                                        textBlock[!TextBlock.TextProperty] = new Binding("Value.Content");
                                    }
                                    else if (value.Value is null)
                                    {
                                        textBlock.Text = "(null)";
                                        textBlock[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("SystemControlForegroundBaseMediumBrush");
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

        MainTreeDataGrid.AddHandler(TreeDataGrid.PointerReleasedEvent, MainTreeDataGrid_PointerReleased_HandledEventsToo, handledEventsToo: true);
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

    private void OpenItemFromTreeDataGridControl(object? source, bool inNewTab = false)
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
                    vm.TabOpen(item.Value, inNewTab);
                }
            }
        }
    }

    public void ExpandItemOnTree(MainViewModel.TreeDataGridItem item)
    {
        if (DataContext is not MainViewModel vm)
            return;

        IndexPath? foundIndex = FindTreeIndexPathFromValue(item, vm.TreeDataGridData);

        if (foundIndex is IndexPath index)
        {
            var source = (MainTreeDataGrid.Source as HierarchicalTreeDataGridSource<MainViewModel.TreeDataGridItem>)!;
            source.Expand(index);
        }
    }

    public void SelectValueInTree(object value)
    {
        if (DataContext is not MainViewModel vm)
            return;

        IndexPath? foundIndex = FindTreeIndexPathFromValue(value, vm.TreeDataGridData);

        if (foundIndex is IndexPath index)
        {
            var source = (MainTreeDataGrid.Source as HierarchicalTreeDataGridSource<MainViewModel.TreeDataGridItem>)!;
            source.Expand(index);

            MainTreeDataGrid.RowSelection!.SelectedIndex = index;

            int rowIndex = MainTreeDataGrid.Rows!.ModelIndexToRowIndex(index);
            MainTreeDataGrid.RowsPresenter!.BringIntoView(rowIndex);
        }
    }

    public static IndexPath? FindTreeIndexPathFromValue(object value, IList<MainViewModel.TreeDataGridItem>? list, IndexPath indexPath = new())
    {
        if (list is null)
            return null;

        for (int i = 0; i < list.Count; i++)
        {
            MainViewModel.TreeDataGridItem? item = list[i];
            if (item.Value == value || item == value)
            {
                return indexPath.Append(i);
            }

            IndexPath? result = FindTreeIndexPathFromValue(value, item.Children, indexPath.Append(i));
            if (result is not null)
                return result;
        }

        return null;
    }

    private void MainTreeDataGrid_DoubleTapped(object? sender, TappedEventArgs e)
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

    private void MainTreeDataGrid_PointerReleased_HandledEventsToo(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Middle
            && ((e.Source as Visual)?.GetTransformedBounds()?.Contains(e.GetPosition(null)) ?? false))
        {
            OpenItemFromTreeDataGridControl(e.Source, inNewTab: true);
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
                    "AnimationCurves" => vm.Data.AnimationCurves as IList,
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

                if (await vm.View!.MessageDialog($"Delete {resource}?\nNote that the code often references objects by ID, " +
                    $"so this operation is likely to break stuff because other items will shift up!",
                    buttons: MessageWindow.Buttons.YesNo) == MessageWindow.Result.Yes)
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

    private void TabMenu_Select_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        if (e.Source is Control control)
        {
            TabStripItem? tabItem = control.FindLogicalAncestorOfType<TabStripItem>();
            if (tabItem is not null && tabItem.DataContext is TabItemViewModel vmTabItem)
            {
                if (vmTabItem?.Content is IUndertaleResourceViewModel vmResourceView)
                {
                    SelectValueInTree(vmResourceView.Resource);
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

    private void TabMenu_CloseAll_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.TabCloseAll();
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