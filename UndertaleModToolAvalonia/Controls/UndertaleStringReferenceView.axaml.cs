using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleStringReferenceView : UserControl
{
    public static readonly StyledProperty<UndertaleString> ReferenceProperty = AvaloniaProperty.Register<UndertaleStringReferenceView, UndertaleString>(
        nameof(Reference));
    public UndertaleString Reference
    {
        get { return GetValue(ReferenceProperty); }
        set { SetValue(ReferenceProperty, value); }
    }

    readonly MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();

    public UndertaleStringReferenceView()
    {
        InitializeComponent();
        UpdateTextBoxWatermark();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ReferenceProperty)
        {
            UpdateTextBoxWatermark();
        }
    }

    private void TextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox textBox && e.Key == Key.Enter)
        {
            UpdateString(textBox);
        }
    }

    private void TextBox_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Middle
            && ((e.Source as Visual)?.GetTransformedBounds()?.Contains(e.GetPosition(null)) ?? false))
        {
            OpenInNewTab();
        }
    }

    private void TextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            UpdateString(textBox);
        }
    }

    public void Add()
    {
        if (mainVM.Data is null)
            return;
        Reference = mainVM.Data.Strings.MakeString("", createNew: true);
    }

    public void Open()
    {
        mainVM.TabOpen(Reference);
    }

    public void OpenInNewTab()
    {
        mainVM.TabOpen(Reference, inNewTab: true);
    }

    void UpdateString(TextBox textBox)
    {
        if (Reference is not null)
        {
            // TODO: Ask if user wants to change all references or just this one
            BindingOperations.GetBindingExpressionBase(textBox, TextBox.TextProperty)!.UpdateSource();
        }
        else
        {
            // TODO: Create new string
        }
    }

    void UpdateTextBoxWatermark()
    {
        ReferenceTextBox.PlaceholderText = (Reference is null) ? "(string reference)" : "";
    }
}

public class UndertaleStringDropHandler : DropHandlerBase
{
    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (targetContext is UndertaleStringReferenceView vm)
        {
            if (sourceContext is MainViewModel.TreeDataGridItem item && item.Value is UndertaleString resource)
            {
                return true;
            }
        }
        return false;
    }
    public override bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (targetContext is UndertaleStringReferenceView vm)
        {
            if (sourceContext is MainViewModel.TreeDataGridItem item && item.Value is UndertaleString resource)
            {
                vm.Reference = resource;
                return true;
            }
        }
        return false;
    }
}