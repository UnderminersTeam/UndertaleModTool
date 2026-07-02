using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using PropertyChanged.SourceGenerator;

namespace UndertaleModToolAvalonia;

public partial class FindReferencesWindow : Window
{
    public FindReferencesWindow()
    {
        InitializeComponent();

        ResultsDataGrid.AddHandler(DataGrid.KeyDownEvent, DataGrid_KeyDown_Tunnel, RoutingStrategies.Tunnel);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is FindReferencesViewModel vm)
        {
            if (vm.Resource is not null)
            {
                vm.FindReferences();
            }
        }
    }

    void OpenResultFromSource(object? source, bool inNewTab = false)
    {
        if (DataContext is FindReferencesViewModel vm)
            if (source is Control control)
                if (control.DataContext is FindReferencesViewModel.FindReferencesResult result)
                {
                    vm.OpenResult(result, inNewTab);
                }
    }

    private void DataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        OpenResultFromSource(e.Source);
    }

    private void DataGrid_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Middle
            && ((e.Source as Visual)?.GetTransformedBounds()?.Contains(e.GetPosition(null)) ?? false))
        {
            OpenResultFromSource(e.Source, inNewTab: true);
        }
    }

    private void DataGrid_KeyDown_Tunnel(object? sender, KeyEventArgs e)
    {
        if (DataContext is FindReferencesViewModel vm)
            if (e.Key == Key.Enter)
                if (ResultsDataGrid.SelectedItem is FindReferencesViewModel.FindReferencesResult result)
                {
                    e.Handled = true;
                    vm.OpenResult(result);
                }
    }

    private void DataGridRow_Open_Click(object? sender, RoutedEventArgs e)
    {
        OpenResultFromSource(e.Source);
    }

    private void DataGridRow_OpenInNewTab_Click(object? sender, RoutedEventArgs e)
    {
        OpenResultFromSource(e.Source, inNewTab: true);
    }
}