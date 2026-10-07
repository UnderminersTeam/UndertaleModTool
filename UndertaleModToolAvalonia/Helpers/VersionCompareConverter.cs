using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace UndertaleModToolAvalonia;

/// <summary>
/// Checks if a version is greater then or equal, or less than, some value. Bind it to MainViewModel.DataVersion.
/// Parameter follows this pattern: [operation][major[.minor[.release[.build]]]]
/// Operation can be GE (greater or equal) or L (less than).
/// Usage:
/// <code>
/// {Binding $parent[l:MainView].((l:MainViewModel)DataContext).DataVersion,
/// Converter={StaticResource VersionCompareConverter},
/// ConverterParameter=2}
/// </code>
/// </summary>
public class VersionCompareConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ValueTuple<uint, uint, uint, uint> _version && parameter is string compareString)
        {
            (uint Major, uint Minor, uint Release, uint Build) version = _version;
            uint[] versionList = [version.Major, version.Minor, version.Release, version.Build];

            string operation = "GE";

            if (compareString.StartsWith("GE"))
            {
                operation = "GE";
                compareString = compareString[("GE".Length)..];
            }
            else if (compareString.StartsWith("L"))
            {
                operation = "L";
                compareString = compareString[("L".Length)..];
            }

            uint[] versionCompareList = [.. compareString.Split('.').Select(x => uint.Parse(x))];

            for (int i = 0; i < versionCompareList.Length; i++)
            {
                if (versionList[i] != versionCompareList[i])
                    if (operation == "GE")
                        return versionList[i] > versionCompareList[i];
                    else if (operation == "L")
                        return versionList[i] < versionCompareList[i];
            }

            if (operation == "GE")
                return true;
            else if (operation == "L")
                return false;
        }

        return new BindingNotification(new InvalidOperationException(), BindingErrorType.Error);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
