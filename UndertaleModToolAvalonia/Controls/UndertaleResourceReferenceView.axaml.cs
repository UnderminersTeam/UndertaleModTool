using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using UndertaleModLib;

namespace UndertaleModToolAvalonia;

using AddFuncType = Func<object?, Task<UndertaleResource?>>;

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

    public static readonly StyledProperty<AddFuncType?> AddFuncProperty = AvaloniaProperty.Register<UndertaleResourceReferenceView, AddFuncType?>(
        nameof(AddFunc));
    public AddFuncType? AddFunc
    {
        get { return GetValue(AddFuncProperty); }
        set { SetValue(AddFuncProperty, value); }
    }

    public static readonly StyledProperty<object?> AddFuncArgumentProperty = AvaloniaProperty.Register<UndertaleResourceReferenceView, object?>(
        nameof(AddFuncArgument));
    public object? AddFuncArgument
    {
        get { return GetValue(AddFuncArgumentProperty); }
        set { SetValue(AddFuncArgumentProperty, value); }
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

    private void TextBox_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Middle
            && ((e.Source as Visual)?.GetTransformedBounds()?.Contains(e.GetPosition(null)) ?? false))
        {
            OpenInNewTab();
        }
    }

    public async void Add()
    {
        if (AddFunc is not null)
        {
            UndertaleResource? reference = await AddFunc(AddFuncArgument);
            if (reference is not null)
                Reference = reference;
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