using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleStringReferenceView : UserControl
{
    public static readonly StyledProperty<UndertaleString> ReferenceProperty = AvaloniaProperty.Register<UndertaleStringReferenceView, UndertaleString>(
        nameof(Reference), defaultBindingMode: BindingMode.TwoWay);
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
        ReferenceTextBox.AddHandler(TextBox.KeyDownEvent, TextBox_KeyDown_Tunnel, RoutingStrategies.Tunnel);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ReferenceProperty)
        {
            UpdateTextBoxWatermark();
        }
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);

        UpdateReferenceToText();
    }

    private void TextBox_KeyDown_Tunnel(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            e.Handled = true;
            UpdateReferenceToText();
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
        UpdateReferenceToText();
    }

    public void Add()
    {
        if (mainVM.Data is not null)
            Reference = mainVM.Data.Strings.MakeString("");
    }

    public void Open()
    {
        _ = mainVM.TabOpen(Reference);
    }

    public void OpenInNewTab()
    {
        _ = mainVM.TabOpen(Reference, inNewTab: true);
    }

    void UpdateReferenceToText()
    {
        if (mainVM.Data is not null && ReferenceTextBox.Text is not null)
        {
            Reference = mainVM.Data.Strings.MakeString(ReferenceTextBox.Text);
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
            if (sourceContext is DataExplorerViewModel.Item item && item.Value is UndertaleString resource)
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
            if (sourceContext is DataExplorerViewModel.Item item && item.Value is UndertaleString resource)
            {
                vm.Reference = resource;
                return true;
            }
        }
        return false;
    }
}