using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UndertaleModToolAvalonia.Controls;

public partial class FlagsBoxView : UserControl
{
    public static readonly StyledProperty<dynamic> ValueProperty = AvaloniaProperty.Register<FlagsBoxView, dynamic>(
        nameof(Value), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public dynamic Value
    {
        get { return GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    public partial class Flag : ObservableObject
    {
        public dynamic FlagEnum { get; set; }
        public string Name { get; set; }
        [ObservableProperty]
        public bool _Checked;
    }

    public ObservableCollection<Flag> Flags { get; set; } = new ObservableCollection<Flag>();

    public FlagsBoxView()
    {
        InitializeComponent();

        this.GetObservable(ValueProperty).Subscribe(new AnonymousObserver<dynamic>(value =>
        {
            // Update checkboxes to fit with value.
            if (value is Enum enumValue)
            {
                foreach (dynamic flagEnum in Enum.GetValues(enumValue.GetType()))
                {
                    Flag? f = Flags.FirstOrDefault(x => (x!.FlagEnum) == flagEnum, null);
                    if (f is not null)
                    {
                        f.Checked = enumValue.HasFlag(flagEnum);
                    }
                    else
                    {
                        Flags.Add(new Flag()
                        {
                            FlagEnum = flagEnum,
                            Name = flagEnum.ToString(),
                            Checked = enumValue.HasFlag(flagEnum),
                        });
                    }
                }
            }
        }));
    }

    public void CheckBox_Checked(object? sender, RoutedEventArgs e)
    {
        CheckBox checkBox = (sender as CheckBox)!;
        if (checkBox.DataContext is Flag flag)
        {
            if (checkBox.IsChecked == true)
            {
                Value |= flag.FlagEnum;
            }
            else
            {
                Value &= ~flag.FlagEnum;
            }
        }
    }
}