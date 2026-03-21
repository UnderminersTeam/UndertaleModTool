using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using PropertyChanged.SourceGenerator;

namespace UndertaleModToolAvalonia;

public partial class FlagsBoxView : UserControl
{
    public static readonly StyledProperty<dynamic> ValueProperty = AvaloniaProperty.Register<FlagsBoxView, dynamic>(
        nameof(Value), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public dynamic Value
    {
        get { return GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    public partial class Flag
    {
        public dynamic FlagEnum;
        public string Name { get; set; }

        [Notify] private bool _Checked;

        public Flag(dynamic flagEnum, string name, bool _checked)
        {
            FlagEnum = flagEnum;
            Name = name;
            Checked = _checked;
        }
    }

    public ObservableCollection<Flag> Flags { get; set; } = new ObservableCollection<Flag>();

    public FlagsBoxView()
    {
        InitializeComponent();

        if (this.Resources["FlagEnumToStringConverter"] is FlagEnumToStringConverter converter)
        {
            converter.View = this;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            // Update checkboxes to fit with value.
            if (change.NewValue is Enum enumValue)
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
                        Flags.Add(new Flag(
                            flagEnum: flagEnum,
                            name: flagEnum.ToString(),
                            _checked: enumValue.HasFlag(flagEnum)));
                    }
                }
            }
        }
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
                Enum test = (Enum)Enum.ToObject(Value.GetType(), 42);
            }
        }
    }
}

public class FlagEnumToStringConverter : IValueConverter
{
    public FlagsBoxView? View { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Enum valueEnum)
            return valueEnum.ToString();
        return BindingOperations.DoNothing;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        DataValidationErrors.SetError(View!.ValueTextBox, null);
        if (value is string valueString)
        {
            if (Enum.TryParse(View!.Value.GetType(), valueString, out object? result))
            {
                return result;
            }
            else
            {
                // Can't do this because the type is dynamic, so the notification will be stored in Value. This may actually be a bug with Avalonia, I'm not sure.
                // return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
                DataValidationErrors.SetError(View!.ValueTextBox, new InvalidCastException());
                return BindingOperations.DoNothing;

            }
        }
        return BindingOperations.DoNothing;
    }
}