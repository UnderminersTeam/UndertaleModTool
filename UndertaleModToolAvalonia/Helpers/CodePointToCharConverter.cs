using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UndertaleModToolAvalonia;

public class CodePointToCharConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ushort valueUshort)
        {
            return (char)valueUshort;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string valueString)
        {
            if (!string.IsNullOrEmpty(valueString))
            {
                return (ushort)valueString[0];
            }
        }
        return null;
    }
}
