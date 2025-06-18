using System;
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
        if (DataContext is UndertaleCodeViewModel vm)
        {
            vm.CompileAndDecompileGML();
        }
    }

    private void ASMTextEditor_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleCodeViewModel vm)
        {
            vm.CompileAndDecompileASM();
        }
    }

    private void GMLTextEditor_TextChanged(object? sender, EventArgs e)
    {
        if (DataContext is UndertaleCodeViewModel vm)
        {
            vm.GMLOutdated = true;
        }
    }

    private void ASMTextEditor_TextChanged(object? sender, EventArgs e)
    {
        if (DataContext is UndertaleCodeViewModel vm)
        {
            vm.ASMOutdated = true;
        }
    }
}