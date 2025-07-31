using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace UndertaleModToolAvalonia.Helpers;

public class ByteArrayToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte[] byteArray)
            return BitConverter.ToString(byteArray).Replace("-", " ");
        return "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int byteArrayLength = 16;
        if (value is string stringValue)
        {
            string[] hexValues = stringValue.Split(" ");
            if (hexValues.Length != byteArrayLength)
            {
                return new BindingNotification(new InvalidOperationException(), BindingErrorType.DataValidationError);
            }
            byte[] bytes = new byte[hexValues.Length];

            try
            {
                for (int i = 0; i < hexValues.Length; i++)
                {
                    bytes[i] = System.Convert.ToByte(hexValues[i], 16);
                }
            }
            catch (Exception e) when (e is FormatException || e is ArgumentOutOfRangeException || e is OverflowException)
            {
                return new BindingNotification(new InvalidOperationException(), BindingErrorType.DataValidationError);
            }
            return bytes;
        }
        return new BindingNotification(new InvalidOperationException(), BindingErrorType.DataValidationError);
    }
}