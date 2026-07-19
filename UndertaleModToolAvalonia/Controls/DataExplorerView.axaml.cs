using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class DataExplorerView : UserControl
{
    public DataExplorerView()
    {
        InitializeComponent();

        MainTreeDataGrid.AddHandler(TreeDataGrid.PointerReleasedEvent, MainTreeDataGrid_PointerReleased_HandledEventsToo, handledEventsToo: true);
    }

    protected override void OnInitialized()
    {
        if (DataContext is not DataExplorerViewModel vm)
            return;

        MainTreeDataGrid.Source = new HierarchicalTreeDataGridSource<DataExplorerViewModel.Item>(vm.TreeDataGridData)
        {
            Columns = {
                new HierarchicalExpanderColumn<DataExplorerViewModel.Item>(
                    new TemplateColumn<DataExplorerViewModel.Item>(null,
                        new FuncDataTemplate<DataExplorerViewModel.Item>((value, namescope) =>
                        {
                            if (value is null)
                                return null;

                            TextBlock textBlock = new() { Text = value.Text };

                            if (value.Value is UndertaleNamedResource namedResource)
                            {
                                textBlock[!TextBlock.TextProperty] = new Binding("Value.Name.Content");

                                if (namedResource is UndertaleCode code)
                                {
                                    textBlock.BindClass("DisableForeground", new Binding("Value.ParentEntry")
                                    {
                                        Converter = ObjectConverters.IsNotNull
                                    }, null!);
                                }
                            }
                            else if (value.Value is UndertaleString _string)
                            {
                                textBlock[!TextBlock.TextProperty] = new Binding("Value.Content");
                            }
                            else if (value.Value is null)
                            {
                                textBlock.Text = "(null)";
                                textBlock.Classes.Add("DisableForeground");
                            }
                            //else if (value.Value is UndertaleData data)
                            //{
                            //    textBlock[!TextBlock.TextProperty] = new Binding("Value.GeneralInfo");
                            //}

                            return textBlock;
                        }), width: GridLength.Star
                    ),
                    childSelector: x => x.Children)
            }
        };

        base.OnInitialized();
    }

    #region Events
    private void MainTreeDataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        OpenItemFromTreeDataGridControl(e.Source);
    }

    private void MainTreeDataGrid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
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

    private void OpenItemFromTreeDataGridControl(object? source, bool inNewTab = false)
    {
        if (DataContext is not DataExplorerViewModel vm)
            return;

        if (source is Control control)
        {
            TreeDataGridRow? row = control.FindLogicalAncestorOfType<TreeDataGridRow>(includeSelf: true);
            if (row?.DataContext is DataExplorerViewModel.Item item)
            {
                if (row.Rows?[row.RowIndex] is HierarchicalRow<DataExplorerViewModel.Item> hierarchicalRow)
                {
                    hierarchicalRow.IsExpanded = !hierarchicalRow.IsExpanded;
                }
                vm.MainVM.TabOpen(item.Value, inNewTab);
            }
        }
    }
    #endregion

    #region Context menus

    public void ContextMenu_Add_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DataExplorerViewModel vm)
            return;

        DataExplorerViewModel.Item? item = GetItemFromTreeDataGridControl(e.Source);
        if (item is not null && vm.MainVM.Data is not null)
        {
            // This could probably be better
            IList list = (item.Value switch
            {
                "AudioGroups" => vm.MainVM.Data.AudioGroups as IList,
                "Sounds" => vm.MainVM.Data.Sounds as IList,
                "Sprites" => vm.MainVM.Data.Sprites as IList,
                "Backgrounds" => vm.MainVM.Data.Backgrounds as IList,
                "Paths" => vm.MainVM.Data.Paths as IList,
                "Scripts" => vm.MainVM.Data.Scripts as IList,
                "Shaders" => vm.MainVM.Data.Shaders as IList,
                "Fonts" => vm.MainVM.Data.Fonts as IList,
                "Timelines" => vm.MainVM.Data.Timelines as IList,
                "GameObjects" => vm.MainVM.Data.GameObjects as IList,
                "Rooms" => vm.MainVM.Data.Rooms as IList,
                "Extensions" => vm.MainVM.Data.Extensions as IList,
                "TexturePageItems" => vm.MainVM.Data.TexturePageItems as IList,
                "Code" => vm.MainVM.Data.Code as IList,
                "Variables" => vm.MainVM.Data.Variables as IList,
                "Functions" => vm.MainVM.Data.Functions as IList,
                "CodeLocals" => vm.MainVM.Data.CodeLocals as IList,
                "Strings" => vm.MainVM.Data.Strings as IList,
                "EmbeddedTextures" => vm.MainVM.Data.EmbeddedTextures as IList,
                "EmbeddedAudio" => vm.MainVM.Data.EmbeddedAudio as IList,
                "TextureGroupInformation" => vm.MainVM.Data.TextureGroupInfo as IList,
                "EmbeddedImages" => vm.MainVM.Data.EmbeddedImages as IList,
                "AnimationCurves" => vm.MainVM.Data.AnimationCurves as IList,
                "ParticleSystems" => vm.MainVM.Data.ParticleSystems as IList,
                "ParticleSystemEmitters" => vm.MainVM.Data.ParticleSystemEmitters as IList,
                _ => null,
            })!;

            vm.MainVM.DataItemAdd(list);
        }
    }

    public void ContextMenu_Open_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DataExplorerViewModel vm)
            return;

        DataExplorerViewModel.Item? item = GetItemFromTreeDataGridControl(e.Source);
        if (item is not null && vm.MainVM.Data is not null)
        {
            vm.MainVM.TabOpen(item.Value);
        }
    }

    public void ContextMenu_OpenInNewTab_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DataExplorerViewModel vm)
            return;

        DataExplorerViewModel.Item? item = GetItemFromTreeDataGridControl(e.Source);
        if (item is not null && vm.MainVM.Data is not null)
        {
            vm.MainVM.TabOpen(item.Value, inNewTab: true);
        }
    }

    public async void ContextMenu_CopyName_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DataExplorerViewModel vm)
            return;

        DataExplorerViewModel.Item? item = GetItemFromTreeDataGridControl(e.Source);
        if (item is not null && vm.MainVM.Data is not null)
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

    public async void ContextMenu_FindReferences_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DataExplorerViewModel vm)
            return;

        DataExplorerViewModel.Item? item = GetItemFromTreeDataGridControl(e.Source);
        if (item is not null && item.Value is UndertaleResource resource && vm.MainVM.Data is not null)
        {
            vm.MainVM.OpenFindReferences(resource);
        }
    }

    public async void ContextMenu_Move_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DataExplorerViewModel vm)
            return;

        DataExplorerViewModel.Item? item = GetItemFromTreeDataGridControl(e.Source);
        if (item is not null && vm.MainVM.Data is not null && vm.MainVM.View is not null)
        {
            UndertaleResource resource = (item.Value as UndertaleResource)!;
            IList list = vm.MainVM.Data[resource.GetType()];
            int oldIndex = list.IndexOf(resource);

            string? input = await vm.MainVM.View.TextBoxDialog("Swap to position:", oldIndex.ToString());
            if (input is null)
                return;

            if (!int.TryParse(input, out int newIndex))
            {
                await vm.MainVM.View.MessageDialog($"\"{input}\" is not a integer");
                return;
            }
            if (newIndex < 0 || newIndex >= list.Count)
            {
                await vm.MainVM.View.MessageDialog($"{newIndex} is out of range of the list");
                return;
            }

            object? temp = list[newIndex];
            list[newIndex] = list[oldIndex];
            list[oldIndex] = temp;
        }
    }

    public async void ContextMenu_Remove_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DataExplorerViewModel vm)
            return;

        DataExplorerViewModel.Item? item = GetItemFromTreeDataGridControl(e.Source);
        if (item is not null && vm.MainVM.Data is not null)
        {
            UndertaleResource resource = (item.Value as UndertaleResource)!;
            vm.MainVM.DataItemRemove(resource);
        }
    }

    static DataExplorerViewModel.Item? GetItemFromTreeDataGridControl(object? source)
    {
        if (source is Control control)
        {
            TreeDataGridRow? row = control.FindLogicalAncestorOfType<TreeDataGridRow>(includeSelf: true);
            if (row?.DataContext is DataExplorerViewModel.Item item)
            {
                return item;
            }
        }

        return null;
    }
    #endregion

    public void ExpandItemOnTree(DataExplorerViewModel.Item item)
    {
        if (DataContext is not DataExplorerViewModel vm)
            return;

        IndexPath? foundIndex = FindTreeIndexPathFromValue(item, vm.TreeDataGridData);

        if (foundIndex is IndexPath index)
        {
            var source = (MainTreeDataGrid.Source as HierarchicalTreeDataGridSource<DataExplorerViewModel.Item>)!;
            source.Expand(index);
        }
    }

    public void SelectValueInTree(object value)
    {
        if (DataContext is not DataExplorerViewModel vm)
            return;

        IndexPath? foundIndex = FindTreeIndexPathFromValue(value, vm.TreeDataGridData);

        if (foundIndex is IndexPath index)
        {
            var source = (MainTreeDataGrid.Source as HierarchicalTreeDataGridSource<DataExplorerViewModel.Item>)!;
            source.Expand(index);
            source.RowSelection!.Select(index);

            int rowIndex = MainTreeDataGrid.Rows!.ModelIndexToRowIndex(index);
            MainTreeDataGrid.RowsPresenter!.BringIntoView(rowIndex);
        }
    }

    static IndexPath? FindTreeIndexPathFromValue(object value, IList<DataExplorerViewModel.Item>? list, IndexPath indexPath = new())
    {
        if (list is null)
            return null;

        for (int i = 0; i < list.Count; i++)
        {
            DataExplorerViewModel.Item? item = list[i];
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
}