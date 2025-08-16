﻿using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace UndertaleModToolAvalonia;

public partial class App : Application
{
    public static IServiceProvider Services = null!;
    public static IStyle? CurrentCustomStyles = null;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        // Dependency injection.
        ServiceCollection collection = new();
        collection.AddSingleton<MainViewModel>();

        Services = collection.BuildServiceProvider();

        MainViewModel vm = Services.GetRequiredService<MainViewModel>();
        vm.Initialize();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm,
                WindowState = WindowState.Maximized,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
