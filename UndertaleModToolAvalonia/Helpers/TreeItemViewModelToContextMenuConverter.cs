using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using UndertaleModLib;

namespace UndertaleModToolAvalonia;

public class TreeItemViewModelToContextMenuConverter : IValueConverter
{
    public ContextMenu? ListMenu { get; set; }
    public ContextMenu? SingleMenu { get; set; }
    public ContextMenu? ResourceMenu { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TreeItemViewModel treeItem)
        {
            if (treeItem.Tag?.Equals("list") ?? false)
                return ListMenu;
            if (treeItem.Value is UndertaleResource)
                return ResourceMenu;
            if (treeItem.Value is "GeneralInfo" or "GlobalInitScripts" or "GameEndScripts")
                return SingleMenu;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}