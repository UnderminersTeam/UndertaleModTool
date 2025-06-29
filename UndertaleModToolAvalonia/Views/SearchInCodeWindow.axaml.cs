using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;

namespace UndertaleModToolAvalonia.Views;

public partial class SearchInCodeWindow : Window
{
    // TODO: Shift+enter in textbox
    // TODO: Enter to open result
    // TODO: Open multiple results
    // TODO: Context menu

    public SearchInCodeWindow()
    {
        InitializeComponent();
    }

    private void DataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is SearchInCodeViewModel vm)
            if (e.Source is Control control)
                if (control.DataContext is SearchInCodeViewModel.SearchResult searchResult)
                    vm.OpenSearchResult(searchResult);
    }
}