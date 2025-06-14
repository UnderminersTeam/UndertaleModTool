using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UndertaleModToolAvalonia.Helpers;

public class LevelToWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int level)
        {
            return level * 20;
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
