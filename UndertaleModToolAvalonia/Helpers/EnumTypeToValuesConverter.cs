using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace UndertaleModToolAvalonia.Helpers;

public class EnumTypeToValuesConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Type enumType)
        {
            return Enum.GetValues(enumType);
        }
        return BindingNotification.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
