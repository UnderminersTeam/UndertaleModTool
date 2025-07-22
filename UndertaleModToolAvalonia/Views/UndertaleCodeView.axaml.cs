using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleCodeView : UserControl
{
    public UndertaleCodeView()
    {
        InitializeComponent();

        DataContextChanged += (_, __) =>
        {
            if (DataContext is UndertaleCodeViewModel vm)
            {
                if (this.IsAttachedToVisualTree())
                {
                    ProcessLastGoToLocation();
                }
                else
                {
                    AttachedToLogicalTree += (_, __) =>
                    {
                        ProcessLastGoToLocation();
                    };
                }

                vm.PropertyChanged += (object? source, PropertyChangedEventArgs e) =>
                {
                    if (e.PropertyName == nameof(UndertaleCodeViewModel.LastGoToLocation) && vm.LastGoToLocation is not null)
                    {
                        ProcessLastGoToLocation();
                    }
                };
            }
        };

        GMLTextEditor.Options.ConvertTabsToSpaces = true;
        GMLTextEditor.Options.HighlightCurrentLine = true;
        ASMTextEditor.Options.ConvertTabsToSpaces = true;
        ASMTextEditor.Options.HighlightCurrentLine = true;
    }

    public void ProcessLastGoToLocation()
    {
        if (DataContext is UndertaleCodeViewModel vm)
        {
            if (vm.LastGoToLocation is not null)
            {
                GoToLocation(vm.LastGoToLocation.Value);
                vm.LastGoToLocation = null;
            }
        }
    }

    public void GoToLocation((UndertaleCodeViewModel.Tab tab, int line) location)
    {
        if (DataContext is UndertaleCodeViewModel vm)
        {
            vm.SelectedTab = location.tab;
            AvaloniaEdit.TextEditor textEditor = (location.tab == UndertaleCodeViewModel.Tab.GML) ? GMLTextEditor : ASMTextEditor;

            textEditor.TextArea.Caret.Column = 0;
            textEditor.TextArea.Caret.Line = location.line;
            textEditor.Focus();

            EventHandler? func = null;
            func = (_, __) =>
            {
                textEditor.ScrollToLine(location.line);
                textEditor.LayoutUpdated -= func;
            };
            textEditor.LayoutUpdated += func;

            // HACK: I don't know how to check if the layout has updated already here or not, so I just invalidate it to call the above function.
            textEditor.InvalidateMeasure();
        }
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