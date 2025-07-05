using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModToolAvalonia.Views;

namespace UndertaleModToolAvalonia.Core;

public partial class SettingsFile
{
    public MainViewModel MainVM = null!;

    public SettingsFile() {}
    public SettingsFile(IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();
    }

    public static async Task<SettingsFile> Load(IServiceProvider serviceProvider)
    {
        MainViewModel mainVM = serviceProvider.GetRequiredService<MainViewModel>();

        string roamingAppData = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UndertaleModToolAvalonia");

        try
        {
            string path = Path.Join(roamingAppData, "Settings.json");

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                SettingsFile? settings = JsonSerializer.Deserialize<SettingsFile>(json, new JsonSerializerOptions()
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });

                if (settings is not null)
                {
                    settings.MainVM = mainVM;
                    return settings;
                }
            }
        }
        catch (Exception e)
        {
            await mainVM.ShowMessageDialog($"Error when loading settings file: {e.Message}");
            throw;
        }

        return new SettingsFile(serviceProvider);
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
            await MainVM.ShowMessageDialog($"Error when saving settings file: {e.Message}");
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
        Save();
    }
}
