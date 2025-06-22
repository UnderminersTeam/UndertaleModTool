using System;
using Avalonia.Styling;
using PropertyChanged.SourceGenerator;

namespace UndertaleModToolAvalonia.Core;

public partial class SettingsFile
{
    public enum ThemeValue
    {
        SystemDefault = 0,
        Light = 1,
        Dark = 2,
    }

    [Notify]
    private ThemeValue _Theme;

    void OnThemeChanged()
    {
        if (App.Current is not null)
        {
            App.Current.RequestedThemeVariant = Theme switch
            {
                ThemeValue.SystemDefault => ThemeVariant.Default,
                ThemeValue.Light => ThemeVariant.Light,
                ThemeValue.Dark => ThemeVariant.Dark,
                _ => throw new NotImplementedException(),
            };
        }
    }
}
