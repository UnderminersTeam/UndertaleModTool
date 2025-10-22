using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using UndertaleModLib;

namespace UndertaleModToolAvalonia;

public partial class UndertaleResourceReferenceView : UserControl
{
    public static readonly StyledProperty<UndertaleResource?> ReferenceProperty = AvaloniaProperty.Register<UndertaleResourceReferenceView, UndertaleResource?>(
        nameof(Reference), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public UndertaleResource? Reference
    {
        get { return GetValue(ReferenceProperty); }
        set { SetValue(ReferenceProperty, value); }
    }

    public static readonly StyledProperty<Type> ReferenceTypeProperty = AvaloniaProperty.Register<UndertaleResourceReferenceView, Type>(
        nameof(ReferenceType));
    public Type ReferenceType
    {
        get { return GetValue(ReferenceTypeProperty); }
        set { SetValue(ReferenceTypeProperty, value); }
    }

    public UndertaleResourceReferenceView()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ReferenceTypeProperty)
        {
            this.Find<TextBox>("TextBox")!.Watermark = "(" + ReferenceType.Name + " reference)";
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

    public void Remove()
    {
        Reference = null;
    }
}

public class UndertaleReferenceDropHandler : DropHandlerBase
{
    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (targetContext is UndertaleResourceReferenceView vm)
        {
            if (sourceContext is MainViewModel.TreeDataGridItem item && item.Value is UndertaleResource resource && vm.ReferenceType.IsInstanceOfType(resource))
            {
                return true;
            }
        }
        return false;
    }
    public override bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (targetContext is UndertaleResourceReferenceView vm)
        {
            if (sourceContext is MainViewModel.TreeDataGridItem item && item.Value is UndertaleResource resource && vm.ReferenceType.IsInstanceOfType(resource))
            {
                vm.Reference = resource;
                return true;
            }
        }
        return false;
    }
}