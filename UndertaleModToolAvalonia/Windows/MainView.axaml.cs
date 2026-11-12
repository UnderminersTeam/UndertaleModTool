using System;
using System.Threading.Tasks;
using Avalonia.Controls;
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
        Window window = this.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException("Window not found");
        await new SettingsWindow()
        {
            DataContext = new SettingsViewModel(serviceProvider),
        }.ShowDialog(window);
    }

    public void OpenSearchInCode(IServiceProvider serviceProvider)
    {
        Window window = this.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException("Window not found");
        new SearchInCodeWindow()
        {
            DataContext = new SearchInCodeViewModel(serviceProvider),
        }.Show(window);
    }

    public void OpenFindReferences(IServiceProvider serviceProvider, UndertaleResource? resource = null)
    {
        Window window = this.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException("Window not found");
        new FindReferencesWindow()
        {
            DataContext = new FindReferencesViewModel(serviceProvider, resource),
        }.Show(window);
    }

    public void OpenProjectAssets(IServiceProvider serviceProvider)
    {
        Window window = this.FindLogicalAncestorOfType<Window>() ?? throw new InvalidOperationException("Window not found");

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