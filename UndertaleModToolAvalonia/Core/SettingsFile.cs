using System;
using System.IO;
using System.Text.Json;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace UndertaleModToolAvalonia;

public partial class SettingsFile
{
    static readonly string roamingAppData = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UndertaleModToolAvalonia");

    public SettingsFile() { }

    public static (SettingsFile settingsFile, Exception? exception) Load()
    {
        SettingsFile? settings = null;

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
                    // NOTE: Check for upgrades here.
                    settings.Version = App.VersionString;
                }
            }
            catch (Exception ex)
            {
                return (new SettingsFile(), ex);
            }
        }

        settings ??= new SettingsFile();
        return (settings, null);
    }

    public static Exception? LoadStyles()
    {
        // Load Styles.xaml
        string stylesPath = Path.Join(roamingAppData, "Styles.xaml");

        if (File.Exists(stylesPath))
        {
            Styles styles;
            try
            {
                string xaml = File.ReadAllText(stylesPath);
                styles = AvaloniaRuntimeXamlLoader.Parse<Styles>(xaml);
            }
            catch (Exception ex)
            {
                return ex;
            }

            if (App.CurrentCustomStyles is not null)
                App.Current!.Styles.Remove(App.CurrentCustomStyles);

            App.CurrentCustomStyles = styles;
            App.Current!.Styles.Add(styles);
        }

        return null;
    }

    public Exception? Save()
    {
        Directory.CreateDirectory(roamingAppData);

        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
        });

        try
        {
            File.WriteAllText(Path.Join(roamingAppData, "Settings.json"), json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return ex;
        }
        return null;
    }

    public string Version { get; set; } = App.VersionString;

    public enum ThemeValue
    {
        SystemDefault = 0,
        Light = 1,
        Dark = 2,
    }

    public ThemeValue Theme
    {
        get;
        set
        {
            field = value;
            App.Current?.RequestedThemeVariant = value switch
            {
                ThemeValue.SystemDefault => ThemeVariant.Default,
                ThemeValue.Light => ThemeVariant.Light,
                ThemeValue.Dark => ThemeVariant.Dark,
                _ => throw new NotImplementedException(),
            };
        }
    }

    public bool StartMaximized { get; set; } = true;

    public bool OpenNewResourceAfterCreatingIt { get; set; } = false;
    public bool EnableSyntaxHighlighting { get; set; } = true;
    public bool AutomaticallyCompileAndDecompileCodeOnLostFocus { get; set; } = true;

    public bool EnableRoomGridByDefault { get; set; } = false;
    public uint DefaultRoomGridWidth { get; set; } = 20;
    public uint DefaultRoomGridHeight { get; set; } = 20;

    public bool EnableSelectAnyLayerByDefault { get; set; } = true;

    public bool EnableProjectBackup { get; set; } = true;

    public bool AlwaysSaveDataInProjectDestination { get; set; } = true;

    public string InstanceIdPrefix { get; set; } = "inst_";

    public Underanalyzer.Decompiler.DecompileSettings DecompileSettings { get; set; } = new();
}
