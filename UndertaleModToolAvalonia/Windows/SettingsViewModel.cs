using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace UndertaleModToolAvalonia;

public partial class SettingsViewModel : ObservableObject
{
    public MainViewModel MainVM { get; }

    public enum IndentStyleValue
    {
        FourSpaces = 0,
        TwoSpaces = 1,
        Tabs = 2,
        Custom = 3,
    }

    public IndentStyleValue IndentStyle
    {
        get;
        set
        {
            field = value;

            MainVM.Settings.DecompileSettings.IndentString = value switch
            {
                IndentStyleValue.FourSpaces => "    ",
                IndentStyleValue.TwoSpaces => "  ",
                IndentStyleValue.Tabs => "\t",
                IndentStyleValue.Custom => MainVM.Settings.DecompileSettings.IndentString,
                _ => throw new NotImplementedException(),
            };

            IsCustomIndentStyle = value is IndentStyleValue.Custom;
        }
    }

    [ObservableProperty]
    public partial bool IsCustomIndentStyle { get; set; }

    public SettingsViewModel(IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        IndentStyle = MainVM.Settings.DecompileSettings.IndentString switch
        {
            "    " => IndentStyleValue.FourSpaces,
            "  " => IndentStyleValue.TwoSpaces,
            "\t" => IndentStyleValue.Tabs,
            _ => IndentStyleValue.Custom,
        };

        IsCustomIndentStyle = (IndentStyle is IndentStyleValue.Custom);
    }
}