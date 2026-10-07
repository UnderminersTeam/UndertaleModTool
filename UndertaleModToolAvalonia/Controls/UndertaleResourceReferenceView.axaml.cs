using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;

namespace UndertaleModToolAvalonia;

using AddFuncType = Func<object?, Task<UndertaleResource?>>;

public partial class UndertaleResourceReferenceView : UserControl
{
    public static readonly StyledProperty<UndertaleResource?> ReferenceProperty = AvaloniaProperty.Register<UndertaleResourceReferenceView, UndertaleResource?>(
        nameof(Reference), defaultBindingMode: BindingMode.TwoWay);
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

    readonly MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();

    public UndertaleResourceReferenceView()
    {
        InitializeComponent();

        ReferenceTextBox.AddHandler(TextBox.KeyDownEvent, TextBox_KeyDown_Tunnel, RoutingStrategies.Tunnel);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ReferenceTypeProperty)
        {
            string name = ReferenceType.Name;
            if (name[.."Undertale".Length] == "Undertale")
            {
                name = name["Undertale".Length..];
            }
            ReferenceTextBox.PlaceholderText = "(" + name + " reference)";
        }
    }

    private void TextBox_KeyDown_Tunnel(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
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

    private void TextBox_DoubleTapped(object? sender, TappedEventArgs e)
    {
        Open();
    }

    private void TextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        UpdateReferenceToText();
    }

    void UpdateReferenceToText()
    {
        if (mainVM.Data is not null)
        {
            string? text = ReferenceTextBox.Text;

            UndertaleResource? ParseResourceText()
            {
                IList list;

                try
                {
                    list = mainVM.Data[ReferenceType];
                }
                catch (Exception e) when (e is NotSupportedException or MissingMemberException)
                {
                    return null;
                }

                if (int.TryParse(text, out int id) && id < list.Count)
                {
                    return list[id] as UndertaleResource;
                }

                return list
                    .OfType<UndertaleNamedResource>()
                    .FirstOrDefault(x => x.Name?.Content?.Equals(text, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            if (string.IsNullOrEmpty(text))
            {
                Reference = null;
            }
            else if (ParseResourceText() is UndertaleResource reference)
            {
                Reference = reference;
            }

            // Update text box to reflect current reference value
            BindingOperations.GetBindingExpressionBase(ReferenceTextBox, TextBox.TextProperty)?.UpdateTarget();
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
        _ = mainVM.TabOpen(Reference);
    }

    public void OpenInNewTab()
    {
        _ = mainVM.TabOpen(Reference, inNewTab: true);
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
            if (sourceContext is DataExplorerViewModel.Item item && item.Value is UndertaleResource resource && vm.ReferenceType.IsInstanceOfType(resource))
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
            if (sourceContext is DataExplorerViewModel.Item item && item.Value is UndertaleResource resource && vm.ReferenceType.IsInstanceOfType(resource))
            {
                vm.Reference = resource;
                return true;
            }
        }
        return false;
    }
}