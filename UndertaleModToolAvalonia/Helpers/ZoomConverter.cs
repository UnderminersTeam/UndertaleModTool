using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace UndertaleModToolAvalonia;

public class ZoomConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double zoom)
            return (zoom * 100) + "%";
        return BindingOperations.DoNothing;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string zoom)
        {
            if (double.TryParse(zoom.Replace("%", ""), out double result))
            {
                return result / 100;
            }
        }
        return BindingOperations.DoNothing;
    }
}
