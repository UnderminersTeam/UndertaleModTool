using System;
using Microsoft.Extensions.DependencyInjection;

namespace UndertaleModToolAvalonia;

public class SettingsViewModel
{
    public MainViewModel MainVM { get; }

    public SettingsViewModel(IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();
    }
}