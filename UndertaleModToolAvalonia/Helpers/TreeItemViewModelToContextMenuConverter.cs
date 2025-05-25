using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using UndertaleModToolAvalonia.Views;

namespace UndertaleModToolAvalonia.Helpers;

public class TreeItemViewModelToContextMenuConverter : IValueConverter
{
    public ContextMenu? ListMenu { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TreeItemViewModel treeItem)
        {
            if (treeItem.Tag?.Equals("list") ?? false)
                return ListMenu;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}