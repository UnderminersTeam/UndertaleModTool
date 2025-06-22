using System;
using Microsoft.Extensions.DependencyInjection;

namespace UndertaleModToolAvalonia.Views;

public class SettingsViewModel
{
    public MainViewModel MainVM { get; }

    public SettingsViewModel(IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();
    }
}