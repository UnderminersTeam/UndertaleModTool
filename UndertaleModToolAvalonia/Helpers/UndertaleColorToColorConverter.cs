using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace UndertaleModToolAvalonia;

public class UndertaleColorToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is uint color)
        {
            return UndertaleColor.ToColor(color);
        }
        return BindingOperations.DoNothing;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return UndertaleColor.FromColor(color);
        }
        return BindingOperations.DoNothing;
    }
}

public static class UndertaleColor
{
    public static uint FromColor(Color color)
    {
        return (uint)((color.A << 24) | (color.B << 16) | (color.G << 8) | color.R);
    }

    public static Color ToColor(uint color)
    {
        return Color.FromArgb((byte)((color >> 24) & 0xff), (byte)(color & 0xff), (byte)((color >> 8) & 0xff), (byte)((color >> 16) & 0xff));
    }
}