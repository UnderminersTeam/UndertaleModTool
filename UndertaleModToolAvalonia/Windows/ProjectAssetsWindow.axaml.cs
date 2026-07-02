using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Project;

namespace UndertaleModToolAvalonia;

public partial class ProjectAssetsWindow : Window
{
    public MainViewModel MainVM { get; }

    private bool _preventUpdateList = false;

    public readonly record struct UnexportedAsset(string Name, string AssetType, IProjectAsset ProjectAsset);

    public ProjectAssetsWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        AssetsDataGrid.AddHandler(DataGrid.KeyDownEvent, DataGrid_KeyDown_Tunnel, RoutingStrategies.Tunnel);

        if (MainVM.Project is ProjectContext project)
        {
            UpdateList();
            project.UnexportedAssetsChanged += UpdateList;
        }
    }

    private void UpdateList(object? sender, EventArgs e)
    {
        UpdateList();
    }

    private void UpdateList()
    {
        if (MainVM.Project is null)
            return;

        // If list is temporarily prevented from being updated, don't do anything
        if (_preventUpdateList)
        {
            return;
        }

        // Populate with current project assets
        List<UnexportedAsset> assets = MainVM.Project
            .EnumerateUnexportedAssets()
            .Select((IProjectAsset asset) => new UnexportedAsset(asset.ProjectName, asset.ProjectAssetType.ToInterfaceName(), asset))
            .ToList();

        // Sort assets by type and name
        assets.Sort((a, b) =>
        {
            if (a.AssetType.CompareTo(b.AssetType) is int i && i != 0)
            {
                return i;
            }
            if (a.Name.CompareTo(b.Name) is int j && j != 0)
            {
                return j;
            }
            return 0;
        });

        // Update data grid
        AssetsDataGrid.ItemsSource = assets;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (MainVM.Project is ProjectContext project)
        {
            project.UnexportedAssetsChanged -= UpdateList;
        }
    }

    private void OpenSelectedListViewItem(bool inNewTab = false)
    {
        if (AssetsDataGrid.SelectedItems is [UnexportedAsset asset, ..])
        {
            OpenUnexportedAsset(asset, inNewTab);
        }
    }

    private void OpenUnexportedAsset(UnexportedAsset asset, bool inNewTab = false)
    {
        if (asset.ProjectAsset is not UndertaleObject obj)
        {
            return;
        }

        MainVM.TabOpen(obj, inNewTab);
    }

    private void UnmarkSelectedListViewItemsForExport()
    {
        if (MainVM.Project is ProjectContext projectContext)
        {
            _preventUpdateList = true;
            foreach (UnexportedAsset asset in AssetsDataGrid.SelectedItems)
            {
                projectContext.UnmarkAssetForExport(asset.ProjectAsset);
            }
            _preventUpdateList = false;
            UpdateList();
        }
    }

    private void DataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Control control)
            if (control.DataContext is UnexportedAsset asset)
                OpenUnexportedAsset(asset);
    }

    private void DataGrid_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Middle
            && ((e.Source as Visual)?.GetTransformedBounds()?.Contains(e.GetPosition(null)) ?? false))
        {
            if (e.Source is Control control)
                if (control.DataContext is UnexportedAsset asset)
                    OpenUnexportedAsset(asset, inNewTab: true);
        }
    }

    private void DataGrid_KeyDown_Tunnel(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OpenSelectedListViewItem();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            UnmarkSelectedListViewItemsForExport();
            e.Handled = true;
        }
    }

    private void DataGridRow_Open_Click(object sender, RoutedEventArgs e)
    {
        OpenSelectedListViewItem();
        e.Handled = true;
    }

    private void DataGridRow_OpenInNewTab_Click(object sender, RoutedEventArgs e)
    {
        OpenSelectedListViewItem(inNewTab: true);
        e.Handled = true;
    }

    private void DataGridRow_UnmarkForExport_Click(object sender, RoutedEventArgs e)
    {
        UnmarkSelectedListViewItemsForExport();
        e.Handled = true;
    }
}

public class ProjectAssetsWindowDataGridDropHandler : DropHandlerBase
{
    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (targetContext is ProjectAssetsWindow window && window.MainVM.Project is ProjectContext project
            && sourceContext is MainViewModel.TreeDataGridItem item && item.Value is IProjectAsset projectAsset)
        {
            return true;
        }

        return false;
    }
    public override bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (targetContext is ProjectAssetsWindow window && window.MainVM.Project is ProjectContext project
            && sourceContext is MainViewModel.TreeDataGridItem item && item.Value is IProjectAsset projectAsset)
        {
            project.MarkAssetForExport(projectAsset);
            return true;
        }

        return false;
    }
}