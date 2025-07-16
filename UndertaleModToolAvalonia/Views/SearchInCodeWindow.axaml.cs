using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace UndertaleModToolAvalonia.Views;

public partial class SearchInCodeWindow : Window
{
    // TODO: Open multiple results
    // TODO: Context menu

    public SearchInCodeWindow()
    {
        InitializeComponent();

        AttachedToVisualTree += (_, __) =>
        {
            SearchTextTextBox.Focus();
        };

        SearchTextTextBox.AddHandler(TextBox.KeyDownEvent, TextBox_KeyDown_Tunnel, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        ResultsDataGrid.AddHandler(DataGrid.KeyDownEvent, DataGrid_KeyDown_Tunnel, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    private void TextBox_KeyDown_Tunnel(object? sender, KeyEventArgs e)
    {
        if (DataContext is SearchInCodeViewModel vm)
            if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                e.Handled = true;
                vm.Search();
            }
    }

    private void DataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is SearchInCodeViewModel vm)
            if (e.Source is Control control)
                if (control.DataContext is SearchInCodeViewModel.SearchResult searchResult)
                    vm.OpenSearchResult(searchResult);
    }

    private void DataGrid_KeyDown_Tunnel(object? sender, KeyEventArgs e)
    {
        if (DataContext is SearchInCodeViewModel vm)
            if (ResultsDataGrid.SelectedItem is SearchInCodeViewModel.SearchResult searchResult)
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    vm.OpenSearchResult(searchResult);
                }
    }

    private void DataGridRow_Open_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SearchInCodeViewModel vm)
            if (e.Source is Control control)
                if (control.DataContext is SearchInCodeViewModel.SearchResult searchResult)
                    vm.OpenSearchResult(searchResult);
    }

    private void DataGridRow_OpenInNewTab_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SearchInCodeViewModel vm)
            if (e.Source is Control control)
                if (control.DataContext is SearchInCodeViewModel.SearchResult searchResult)
                    vm.OpenSearchResult(searchResult, inNewTab: true);
    }
}