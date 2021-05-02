using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace UndertaleModTool
{
    public class Settings
    {
        public static string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UndertaleModTool");
        public static string ProfilesFolder = Path.Combine(AppDataFolder, "Profiles");

        public string GraphVizPath { get; set; } = ".\\graphviz\\bin";
        public string GameMakerStudioPath { get; set; } = "%appdata%\\GameMaker-Studio";
        public string GameMakerStudio2RuntimesPath { get; set; } = "C:\\ProgramData\\GameMakerStudio2\\Cache\\runtimes";
        public bool AssetOrderSwappingEnabled { get; set; } = false;
        public bool ProfileModeEnabled { get; set; } = true;
        public bool ProfileMessageShown { get; set; } = false;
        public bool DeleteOldProfileOnSave { get; set; } = true;

        public static Settings Instance;

        public static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public Settings()
        {
            Instance = this;
        }

        public static void Load()
        {
            try
            {
                string path = Path.Combine(AppDataFolder, "settings.json");
                if (!File.Exists(path))
                {
                    new Settings();
                    Save();
                    return;
                }
                byte[] bytes = File.ReadAllBytes(path);
                JsonSerializer.Deserialize<Settings>(bytes, JsonOptions);
            } catch (Exception e)
            {
                MessageBox.Show($"Failed to load settings.json! Using default values.\n{e.Message}");
                new Settings();
            }
        }

        public static void Save()
        {
            try
            {
                string path = Path.Combine(AppDataFolder, "settings.json");
                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(Instance, JsonOptions);
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to save settings.json!\n{e.Message}");
            }
        }
    }
}
