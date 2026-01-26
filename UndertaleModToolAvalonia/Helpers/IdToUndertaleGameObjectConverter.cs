using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

// TODO: Don't use a static field for services. Also somehow make this update when the ids change.
public class IdToUndertaleGameObjectConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is uint id)
        {
            MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();

            int intId = (int)id;
            if (intId > 0 && intId < mainVM.Data!.GameObjects.Count)
            {
                return mainVM.Data!.GameObjects[intId];
            }
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UndertaleGameObject gameObject)
        {
            MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();
            return mainVM.Data!.GameObjects.IndexOf(gameObject);
        }
        return BindingOperations.DoNothing;
    }
}