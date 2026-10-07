using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace UndertaleModToolAvalonia;

public partial class App : Application
{
    public static string VersionString = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?.?.?.?";
    public static string InformationalVersionString = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "<unknown>";

    public static IServiceProvider Services = null!;
    public static IStyle? CurrentCustomStyles = null;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
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
            };

            Window.WindowOpenedEvent.AddClassHandler(typeof(Window), (source, e) =>
            {
                if (source is Window window)
                {
                    bool isDark = (ActualThemeVariant == ThemeVariant.Dark);
                    window.SetDarkTitleBar(isDark);
                }
            });

            ActualThemeVariantChanged += (s, e) =>
            {
                bool isDark = (ActualThemeVariant == ThemeVariant.Dark);
                foreach (var window in desktop.Windows)
                {
                    window.SetDarkTitleBar(isDark);
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
