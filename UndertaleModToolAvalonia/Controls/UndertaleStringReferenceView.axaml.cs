using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
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

    private void TextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            UpdateString(textBox);
        }
    }

    public void Open()
    {
        MainViewModel mainView = (this.FindLogicalAncestorOfType<MainView>()!.DataContext as MainViewModel)!;
        mainView.TabOpen(Reference);
    }

    public void OpenInNewTab()
    {
        MainViewModel mainView = (this.FindLogicalAncestorOfType<MainView>()!.DataContext as MainViewModel)!;
        mainView.TabOpen(Reference, inNewTab: true);
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
        this.Find<TextBox>("TextBox")!.Watermark = (Reference is null) ? "(UndertaleString reference)" : "";
    }
}

public class UndertaleStringDropHandler : DropHandlerBase
{
    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (targetContext is UndertaleStringReferenceView vm)
        {
            if (sourceContext is TreeItemViewModel treeItem && treeItem.Value is UndertaleString resource)
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
            if (sourceContext is TreeItemViewModel treeItem && treeItem.Value is UndertaleString resource)
            {
                vm.Reference = resource;
                return true;
            }
        }
        return false;
    }
}