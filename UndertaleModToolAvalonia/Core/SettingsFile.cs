using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;

namespace UndertaleModToolAvalonia;

public partial class SettingsFile
{
    public MainViewModel MainVM = null!;

    public SettingsFile() { }
    public SettingsFile(IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();
    }

    public static SettingsFile Load(IServiceProvider serviceProvider)
    {
        MainViewModel mainVM = serviceProvider.GetRequiredService<MainViewModel>();

        SettingsFile? settings = null;

        string roamingAppData = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UndertaleModToolAvalonia");

        // Load Settings.json
        string settingsPath = Path.Join(roamingAppData, "Settings.json");

        if (File.Exists(settingsPath))
        {
            try
            {
                string json = File.ReadAllText(settingsPath);
                settings = JsonSerializer.Deserialize<SettingsFile>(json, new JsonSerializerOptions()
                {
                    AllowTrailingCommas = true,
                });

                if (settings is not null)
                {
                    // Check for upgrades here.
                    settings.MainVM = mainVM;
                    settings.Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?.?.?.?";
                }
            }
            catch (Exception e)
            {
                mainVM.LazyErrorMessages.Add($"Error when loading settings file:\n{e.Message}\nDefault settings loaded.");
            }
        }

        settings ??= new SettingsFile(serviceProvider);

        // Load Styles.xaml
        string stylesPath = Path.Join(roamingAppData, "Styles.xaml");

        if (File.Exists(stylesPath))
        {
            try
            {
                string xaml = File.ReadAllText(stylesPath);
                Styles styles = AvaloniaRuntimeXamlLoader.Parse<Styles>(xaml);

                if (App.CurrentCustomStyles is not null)
                    App.Current!.Styles.Remove(App.CurrentCustomStyles);

                App.CurrentCustomStyles = styles;
                App.Current!.Styles.Add(styles);
            }
            catch (Exception e)
            {
                mainVM.LazyErrorMessages.Add($"Error when loading styles file:\n{e.Message}");
            }
        }

        return settings;
    }

    public async void Save()
    {
        string roamingAppData = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UndertaleModToolAvalonia");
        Directory.CreateDirectory(roamingAppData);

        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
        });

        try
        {
            File.WriteAllText(Path.Join(roamingAppData, "Settings.json"), json);
        }
        catch (Exception e)
        {
            await MainVM.View!.MessageDialog($"Error when saving settings file: {e.Message}");
        }
    }

    public string Version { get; set; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?.?.?.?";

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

    public bool OpenNewResourceAfterCreatingIt { get; set; } = false;
    public bool EnableSyntaxHighlighting { get; set; } = true;
    public bool AutomaticallyCompileAndDecompileCodeOnLostFocus { get; set; } = true;

    public bool EnableRoomGridByDefault { get; set; } = false;
    public uint DefaultRoomGridWidth { get; set; } = 20;
    public uint DefaultRoomGridHeight { get; set; } = 20;

    public bool EnableSelectAnyLayerByDefault { get; set; } = true;

    public string InstanceIdPrefix { get; set; } = "inst_";

    public Underanalyzer.Decompiler.DecompileSettings DecompileSettings { get; set; } = new();
}
