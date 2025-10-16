using System;
using System.ComponentModel;
using System.Reflection;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.VisualTree;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace UndertaleModToolAvalonia;

public partial class UndertaleCodeView : UserControl, IUndertaleCodeView
{
    private static IHighlightingDefinition? GMLHighlightingDefinition = null;
    private static IHighlightingDefinition? ASMHighlightingDefinition = null;

    public (int, int) LastCaretOffsets;

    public UndertaleCodeView()
    {
        InitializeComponent();

        DataContextChanged += (_, __) =>
        {
            if (DataContext is UndertaleCodeViewModel vm)
            {
                vm.View = this;
                if (vm.MainVM.Settings!.EnableSyntaxHighlighting)
                {
                    UndertaleCodeView.GMLHighlightingDefinition ??= LoadHighlightingDefinition("GML");
                    GMLTextEditor.SyntaxHighlighting = UndertaleCodeView.GMLHighlightingDefinition;

                    UndertaleCodeView.ASMHighlightingDefinition ??= LoadHighlightingDefinition("ASM");
                    ASMTextEditor.SyntaxHighlighting = UndertaleCodeView.ASMHighlightingDefinition;
                }
                else
                {
                    GMLTextEditor.SyntaxHighlighting = null;
                    ASMTextEditor.SyntaxHighlighting = null;
                }

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

        InitializeTextEditor(GMLTextEditor);
        InitializeTextEditor(ASMTextEditor);
    }

    static IHighlightingDefinition LoadHighlightingDefinition(string name)
    {
        using (XmlReader reader = XmlReader.Create(AssetLoader.Open(new Uri($"avares://{Assembly.GetExecutingAssembly().FullName}/Assets/Syntax{name}.xshd"))))
        {
            return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
    }

    void InitializeTextEditor(AvaloniaEdit.TextEditor textEditor)
    {
        textEditor.Options.ConvertTabsToSpaces = true;
        textEditor.Options.HighlightCurrentLine = true;
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
        if (DataContext is UndertaleCodeViewModel vm && vm.MainVM.Settings!.AutomaticallyCompileAndDecompileCodeOnLostFocus)
        {
            vm.CompileAndDecompileGML(onlyIfOutdated: true);
        }
    }

    private void ASMTextEditor_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UndertaleCodeViewModel vm && vm.MainVM.Settings!.AutomaticallyCompileAndDecompileCodeOnLostFocus)
        {
            vm.CompileAndDecompileASM(onlyIfOutdated: true);
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

public interface IUndertaleCodeView
{
    private UndertaleCodeView View => (UndertaleCodeView)this;

    public void SaveCaretOffsets()
    {
        View.LastCaretOffsets = (View.GMLTextEditor.CaretOffset, View.ASMTextEditor.CaretOffset);
    }

    public void RestoreCaretOffsets()
    {
        View.GMLTextEditor.CaretOffset = Math.Clamp(View.LastCaretOffsets.Item1, 0, View.GMLTextEditor.Text.Length);
        View.ASMTextEditor.CaretOffset = Math.Clamp(View.LastCaretOffsets.Item2, 0, View.ASMTextEditor.Text.Length);
    }

    public int GMLCaretOffset
    {
        get { return View.GMLTextEditor.CaretOffset; }
        set { View.GMLTextEditor.CaretOffset = value; }
    }
}