using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using UndertaleModLib;

namespace UndertaleModToolAvalonia;

public partial class MainView : UserControl, IView
{
    ProjectAssetsWindow? projectAssetsWindow = null;

    public MainView()
    {
        InitializeComponent();

        DataContextChanged += (_, __) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.View = this;
            }
        };

        Loaded += (_, __) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.OnLoaded();
            }
        };

        CommandTextBox.AddHandler(TextBox.KeyDownEvent, CommandTextBox_KeyDown_Tunnel, RoutingStrategies.Tunnel);
    }

    public async Task OpenSettingsDialog(IServiceProvider serviceProvider)
    {
        Window window = this.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException();
        await new SettingsWindow()
        {
            DataContext = new SettingsViewModel(serviceProvider),
        }.ShowDialog(window);
    }

    public void OpenSearchInCode(IServiceProvider serviceProvider)
    {
        Window window = this.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException();
        new SearchInCodeWindow()
        {
            DataContext = new SearchInCodeViewModel(serviceProvider),
        }.Show(window);
    }

    public void OpenFindReferences(IServiceProvider serviceProvider, UndertaleResource? resource = null)
    {
        Window window = this.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException();
        new FindReferencesWindow()
        {
            DataContext = new FindReferencesViewModel(serviceProvider, resource),
        }.Show(window);
    }

    public void OpenProjectAssets(IServiceProvider serviceProvider)
    {
        Window window = this.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException();

        if (projectAssetsWindow is not null)
        {
            projectAssetsWindow.Focus();
        }
        else
        {
            projectAssetsWindow = new ProjectAssetsWindow(serviceProvider);
            projectAssetsWindow.Closed += (_, _) =>
            {
                projectAssetsWindow = null;
            };
            projectAssetsWindow.Show(window);
        }
    }

    public void CloseProjectAssets()
    {
        projectAssetsWindow?.Close();
        projectAssetsWindow = null;
    }

    public void ExpandItemOnTree(DataExplorerViewModel.Item item)
    {
        DataExplorer.ExpandItemOnTree(item);
    }

    public void SelectValueInTree(object value)
    {
        DataExplorer.SelectValueInTree(value);
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

    private async void CommandTextBox_KeyDown_Tunnel(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                e.Handled = true;
                object? result = await vm.Scripting.RunScript(vm.CommandTextBoxText);
                vm.CommandTextBoxText = result?.ToString() ?? "";
            }
    }
}