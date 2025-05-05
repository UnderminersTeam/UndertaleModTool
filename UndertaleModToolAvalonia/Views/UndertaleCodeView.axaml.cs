using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleCodeView : UserControl
{
    public UndertaleCodeView()
    {
        InitializeComponent();
    }

    private void GMLTextEditor_LostFocus(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"TextEditor_LostFocus {sender} {e}");
        if (DataContext is UndertaleCodeViewModel vm)
        {
            vm.CompileFromGML();
            vm.DecompileToGML();
            vm.DecompileToASM();
        }
    }

    private void ASMTextEditor_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleCodeViewModel vm)
        {
            vm.CompileFromASM();
            vm.DecompileToASM();
            vm.DecompileToGML();
        }
    }
}